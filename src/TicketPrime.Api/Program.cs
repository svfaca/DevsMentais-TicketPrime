using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TicketPrime API",
        Version = "v1",
        Description = "API para gerenciamento de eventos e ingressos"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketPrime API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("Frontend");

var connectionString =
    "Server=(localdb)\\MSSQLLocalDB;Database=TicketPrime;Trusted_Connection=True;TrustServerCertificate=True;";

var tokenSecret = builder.Configuration["Auth:TokenSecret"] ?? "ticketprime-dev-token-secret-change-this";
var bootstrapAdminKey = builder.Configuration["Auth:BootstrapAdminKey"] ?? "ticketprime-bootstrap-admin";

await EnsureAuthSchemaAsync(connectionString);

app.MapGet("/api/eventos/publico", async () =>
{
    await using var connection = new SqlConnection(connectionString);
    var eventos = await connection.QueryAsync(
        "SELECT Id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao FROM Eventos ORDER BY DataEvento DESC");
    return Results.Ok(eventos);
})
.WithName("ListarEventosPublicos")
.WithDescription("Lista eventos para exibicao publica")
.Produces(200);

app.MapGet("/api/eventos", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null)
    {
        return authError;
    }

    await using var connection = new SqlConnection(connectionString);
    var eventos = await connection.QueryAsync(
        "SELECT Id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao FROM Eventos WHERE CriadoPorCpf = @Cpf ORDER BY DataEvento DESC",
        new { Cpf = auth!.Cpf }
    );

    return Results.Ok(eventos);
})
.WithName("ListarMeusEventos")
.WithDescription("Lista apenas os eventos do administrador autenticado")
.Produces(200)
.Produces(401)
.Produces(403);

app.MapPost("/api/eventos", async (CriarEventoRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null)
    {
        return authError;
    }

    await using var connection = new SqlConnection(connectionString);

    const string sql = @"
        INSERT INTO Eventos (Nome, CapacidadeTotal, DataEvento, PrecoPadrao, CriadoPorCpf)
        VALUES (@Nome, @CapacidadeTotal, @DataEvento, @PrecoPadrao, @CriadoPorCpf)";

    await connection.ExecuteAsync(sql, new
    {
        request.Nome,
        request.CapacidadeTotal,
        request.DataEvento,
        request.PrecoPadrao,
        CriadoPorCpf = auth!.Cpf
    });

    return Results.Created("/api/eventos", null);
})
.WithName("CriarEvento")
.WithDescription("Cria evento vinculado ao administrador autenticado")
.Produces(201)
.Produces(401)
.Produces(403);

app.MapGet("/api/cupons", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null)
    {
        return authError;
    }

    await using var connection = new SqlConnection(connectionString);
    var cupons = await connection.QueryAsync(
        "SELECT Codigo, PorcentagemDesconto, ValorMinimoRegra FROM Cupons WHERE CriadoPorCpf = @Cpf ORDER BY Codigo",
        new { Cpf = auth!.Cpf }
    );

    return Results.Ok(cupons);
})
.WithName("ListarMeusCupons")
.WithDescription("Lista apenas os cupons do administrador autenticado")
.Produces(200)
.Produces(401)
.Produces(403);

app.MapPost("/api/cupons", async (CriarCupomRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null)
    {
        return authError;
    }

    await using var connection = new SqlConnection(connectionString);

    const string sql = @"
        INSERT INTO Cupons (Codigo, PorcentagemDesconto, ValorMinimoRegra, CriadoPorCpf)
        VALUES (@Codigo, @PorcentagemDesconto, @ValorMinimoRegra, @CriadoPorCpf)";

    await connection.ExecuteAsync(sql, new
    {
        request.Codigo,
        request.PorcentagemDesconto,
        request.ValorMinimoRegra,
        CriadoPorCpf = auth!.Cpf
    });

    return Results.Created("/api/cupons", null);
})
.WithName("CriarCupom")
.WithDescription("Cria cupom vinculado ao administrador autenticado")
.Produces(201)
.Produces(401)
.Produces(403);

app.MapGet("/api/me", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null)
    {
        return authError;
    }

    await using var connection = new SqlConnection(connectionString);
    var usuario = await connection.QueryFirstOrDefaultAsync(
        "SELECT Cpf, Nome, Email, TipoConta FROM Usuarios WHERE Cpf = @Cpf",
        new { Cpf = auth!.Cpf }
    );

    return usuario is null ? Results.NotFound() : Results.Ok(usuario);
})
.WithName("MeuPerfil")
.WithDescription("Retorna apenas os dados do usuario autenticado")
.Produces(200)
.Produces(401)
.Produces(404);

app.MapPost("/api/usuarios", async (CriarUsuarioRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out _, "adm");
    if (authError is not null)
    {
        return authError;
    }

    var tipoConta = NormalizeAccountType(request.TipoConta);
    if (tipoConta is null)
    {
        return Results.BadRequest("TipoConta invalido. Use 'usuario' ou 'adm'.");
    }

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf,
        request.Nome,
        request.Email,
        request.Senha,
        tipoConta
    ));
})
.WithName("CriarUsuario")
.WithDescription("Cria usuario ou administrador (somente administrador autenticado)")
.Produces(201)
.Produces(400)
.Produces(401)
.Produces(403);

app.MapPost("/api/auth/register", async (RegistrarRequest request) =>
{
    if (!string.IsNullOrWhiteSpace(request.TipoConta)
        && !string.Equals(request.TipoConta, "usuario", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Cadastro publico cria apenas conta do tipo usuario.");
    }

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf,
        request.Nome,
        request.Email,
        request.Senha,
        "usuario"
    ));
})
.WithName("Registrar")
.WithDescription("Registra um novo usuario comum")
.Produces(201)
.Produces(400);

app.MapPost("/api/auth/register-admin", async (RegistrarAdminRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out _, "adm");
    if (authError is not null)
    {
        return authError;
    }

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf,
        request.Nome,
        request.Email,
        request.Senha,
        "adm"
    ));
})
.WithName("RegistrarAdmin")
.WithDescription("Registra novo administrador (somente administrador autenticado)")
.Produces(201)
.Produces(400)
.Produces(401)
.Produces(403);

app.MapPost("/api/auth/bootstrap-admin", async (BootstrapAdminRequest request) =>
{
    if (request.ChaveInstalacao != bootstrapAdminKey)
    {
        return Results.Unauthorized();
    }

    await using var connection = new SqlConnection(connectionString);
    var adminsExistentes = await connection.QueryFirstAsync<int>(
        "SELECT COUNT(1) FROM Usuarios WHERE TipoConta = 'adm'");

    if (adminsExistentes > 0)
    {
        return Results.BadRequest("Ja existe administrador cadastrado. Use /api/auth/register-admin.");
    }

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf,
        request.Nome,
        request.Email,
        request.Senha,
        "adm"
    ));
})
.WithName("BootstrapAdmin")
.WithDescription("Cria o primeiro administrador da base")
.Produces(201)
.Produces(400)
.Produces(401);

app.MapPost("/api/auth/login", async (LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Senha))
    {
        return Results.BadRequest("Informe usuario e senha.");
    }

    var cpfNormalizado = NormalizeCpf(request.Usuario);

    await using var connection = new SqlConnection(connectionString);
    var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioLogin>(
        @"
        SELECT Cpf, Nome, Email, SenhaHash, TipoConta
        FROM Usuarios
        WHERE Cpf = @Cpf OR Email = @Email",
        new { Cpf = cpfNormalizado, Email = request.Usuario }
    );

    if (usuario is null || string.IsNullOrWhiteSpace(usuario.SenhaHash))
    {
        return Results.Unauthorized();
    }

    var senhaValida = string.Equals(usuario.SenhaHash, ComputeSha256(request.Senha), StringComparison.Ordinal);
    if (!senhaValida)
    {
        return Results.Unauthorized();
    }

    var tipoConta = NormalizeAccountType(usuario.TipoConta) ?? "usuario";
    var token = GenerateToken(tokenSecret, new TokenPayload(
        usuario.Cpf,
        tipoConta,
        DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds()
    ));

    return Results.Ok(new LoginResponse(
        usuario.Cpf,
        usuario.Nome,
        usuario.Email,
        tipoConta,
        token
    ));
})
.WithName("Login")
.WithDescription("Realiza login com CPF/e-mail e senha")
.Produces<LoginResponse>(200)
.Produces(401)
.Produces(400);

app.Run();

static async Task<IResult> CreateAccountAsync(string connectionString, AccountCreateData data)
{
    if (string.IsNullOrWhiteSpace(data.Nome)
        || string.IsNullOrWhiteSpace(data.Cpf)
        || string.IsNullOrWhiteSpace(data.Email)
        || string.IsNullOrWhiteSpace(data.Senha))
    {
        return Results.BadRequest("Todos os campos sao obrigatorios.");
    }

    var cpfNormalizado = NormalizeCpf(data.Cpf);
    if (cpfNormalizado.Length != 11)
    {
        return Results.BadRequest("CPF deve conter 11 digitos.");
    }

    await using var connection = new SqlConnection(connectionString);
    var existente = await connection.QueryFirstOrDefaultAsync(
        "SELECT 1 FROM Usuarios WHERE Cpf = @Cpf OR Email = @Email",
        new { Cpf = cpfNormalizado, data.Email }
    );

    if (existente is not null)
    {
        return Results.BadRequest("Ja existe conta com este CPF ou e-mail.");
    }

    await connection.ExecuteAsync(
        @"
        INSERT INTO Usuarios (Cpf, Nome, Email, SenhaHash, TipoConta)
        VALUES (@Cpf, @Nome, @Email, @SenhaHash, @TipoConta)",
        new
        {
            Cpf = cpfNormalizado,
            data.Nome,
            data.Email,
            SenhaHash = ComputeSha256(data.Senha),
            data.TipoConta
        }
    );

    return Results.Created("/api/usuarios", new
    {
        mensagem = "Conta criada com sucesso.",
        tipoConta = data.TipoConta
    });
}

static IResult? TryAuthenticate(HttpContext httpContext, string tokenSecret, out TokenPayload? auth, params string[] requiredRoles)
{
    auth = null;

    var authorization = httpContext.Request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Unauthorized();
    }

    var token = authorization["Bearer ".Length..].Trim();
    if (!TryValidateToken(tokenSecret, token, out var payload))
    {
        return Results.Unauthorized();
    }

    if (requiredRoles.Length > 0
        && !requiredRoles.Contains(payload!.TipoConta, StringComparer.OrdinalIgnoreCase))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    auth = payload;
    return null;
}

static string NormalizeCpf(string value)
{
    return new string(value.Where(char.IsDigit).ToArray());
}

static string? NormalizeAccountType(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    var normalized = value.Trim().ToLowerInvariant();
    return normalized is "usuario" or "adm" ? normalized : null;
}

static string ComputeSha256(string input)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes);
}

static string GenerateToken(string secret, TokenPayload payload)
{
    var payloadJson = JsonSerializer.Serialize(payload);
    var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
    var signatureBytes = Sign(payloadBytes, secret);

    return $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signatureBytes)}";
}

static bool TryValidateToken(string secret, string token, out TokenPayload? payload)
{
    payload = null;

    var parts = token.Split('.');
    if (parts.Length != 2)
    {
        return false;
    }

    if (!TryBase64UrlDecode(parts[0], out var payloadBytes)
        || !TryBase64UrlDecode(parts[1], out var providedSignature))
    {
        return false;
    }

    var expectedSignature = Sign(payloadBytes, secret);
    if (!CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature))
    {
        return false;
    }

    try
    {
        payload = JsonSerializer.Deserialize<TokenPayload>(payloadBytes);
    }
    catch
    {
        return false;
    }

    if (payload is null || string.IsNullOrWhiteSpace(payload.Cpf) || string.IsNullOrWhiteSpace(payload.TipoConta))
    {
        return false;
    }

    var tipo = NormalizeAccountType(payload.TipoConta);
    if (tipo is null)
    {
        return false;
    }

    payload = payload with { TipoConta = tipo };

    var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    return payload.ExpiraEmUnix > nowUnix;
}

static byte[] Sign(byte[] data, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    return hmac.ComputeHash(data);
}

static string Base64UrlEncode(byte[] data)
{
    return Convert.ToBase64String(data)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}

static bool TryBase64UrlDecode(string input, out byte[] data)
{
    try
    {
        var base64 = input
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = 4 - (base64.Length % 4);
        if (padding is > 0 and < 4)
        {
            base64 += new string('=', padding);
        }

        data = Convert.FromBase64String(base64);
        return true;
    }
    catch
    {
        data = Array.Empty<byte>();
        return false;
    }
}

static async Task EnsureAuthSchemaAsync(string connectionString)
{
    const string sql = @"
        IF COL_LENGTH('Usuarios', 'SenhaHash') IS NULL
        BEGIN
            ALTER TABLE Usuarios ADD SenhaHash VARCHAR(128) NULL;
        END

        IF COL_LENGTH('Usuarios', 'TipoConta') IS NULL
        BEGIN
            ALTER TABLE Usuarios ADD TipoConta VARCHAR(20) NOT NULL CONSTRAINT DF_Usuarios_TipoConta DEFAULT 'usuario';
        END

        UPDATE Usuarios
        SET TipoConta = 'usuario'
        WHERE TipoConta IS NULL OR LTRIM(RTRIM(TipoConta)) = '';

        IF COL_LENGTH('Eventos', 'CriadoPorCpf') IS NULL
        BEGIN
            ALTER TABLE Eventos ADD CriadoPorCpf VARCHAR(11) NULL;
        END

        IF COL_LENGTH('Cupons', 'CriadoPorCpf') IS NULL
        BEGIN
            ALTER TABLE Cupons ADD CriadoPorCpf VARCHAR(11) NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Eventos_Usuarios_CriadoPorCpf')
        BEGIN
            ALTER TABLE Eventos WITH NOCHECK
            ADD CONSTRAINT FK_Eventos_Usuarios_CriadoPorCpf
            FOREIGN KEY (CriadoPorCpf) REFERENCES Usuarios(Cpf);
        END

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Cupons_Usuarios_CriadoPorCpf')
        BEGIN
            ALTER TABLE Cupons WITH NOCHECK
            ADD CONSTRAINT FK_Cupons_Usuarios_CriadoPorCpf
            FOREIGN KEY (CriadoPorCpf) REFERENCES Usuarios(Cpf);
        END";

    await using var connection = new SqlConnection(connectionString);
    await connection.ExecuteAsync(sql);
}

record CriarEventoRequest(string Nome, int CapacidadeTotal, DateTime DataEvento, decimal PrecoPadrao);
record CriarCupomRequest(string Codigo, decimal PorcentagemDesconto, decimal ValorMinimoRegra);
record CriarUsuarioRequest(string Cpf, string Nome, string Email, string Senha, string TipoConta);
record RegistrarRequest(string Nome, string Cpf, string Email, string Senha, string? TipoConta);
record RegistrarAdminRequest(string Nome, string Cpf, string Email, string Senha);
record BootstrapAdminRequest(string Nome, string Cpf, string Email, string Senha, string ChaveInstalacao);
record LoginRequest(string Usuario, string Senha);
record LoginResponse(string Cpf, string Nome, string Email, string TipoConta, string Token);
record TokenPayload(string Cpf, string TipoConta, long ExpiraEmUnix);
record AccountCreateData(string Cpf, string Nome, string Email, string Senha, string TipoConta);

class UsuarioLogin
{
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SenhaHash { get; set; }
    public string TipoConta { get; set; } = "usuario";
}