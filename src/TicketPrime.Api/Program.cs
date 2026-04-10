using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var corsOrigins = (builder.Configuration["Cors:Origins"]
    ?? Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
    ?? "http://localhost:3000,http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;

                if (corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                    return true;

                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);

                return false;
            })
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

app.UseCors("AllowFrontend");
app.UseStaticFiles();

var connectionString = ResolveConnectionString(builder.Configuration)
    ?? throw new InvalidOperationException("Connection string nao encontrada. Configure 'ConnectionStrings:DefaultConnection' ou 'DATABASE_URL'.");

var tokenSecret = builder.Configuration["Auth:TokenSecret"] ?? "ticketprime-dev-token-secret-change-this";
var bootstrapAdminKey = builder.Configuration["Auth:BootstrapAdminKey"] ?? "ticketprime-bootstrap-admin";

await EnsureAuthSchemaAsync(connectionString, app.Logger);

// ── EVENTOS ──────────────────────────────────────────────────────────────────

app.MapGet("/api/eventos/publico", async () =>
{
    await using var connection = new NpgsqlConnection(connectionString);
    var eventos = await connection.QueryAsync(@"
        SELECT e.Id, e.Nome, e.CapacidadeTotal, e.DataEvento, e.PrecoPadrao, e.ImagemUrl
        FROM Eventos e
        INNER JOIN Usuarios u ON e.CriadoPorCpf = u.Cpf
        WHERE u.Ativa = TRUE
        ORDER BY e.DataEvento DESC");
    return Results.Ok(eventos);
})
.WithName("ListarEventosPublicos")
.WithDescription("Lista eventos de admins ativos para exibicao publica")
.Produces(200);

app.MapGet("/api/eventos", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    
    var adminAtivo = await connection.QueryFirstOrDefaultAsync(
        "SELECT Ativa FROM Usuarios WHERE Cpf = @Cpf AND TipoConta = 'adm'",
        new { Cpf = auth!.Cpf });
    
    if (adminAtivo is null || !(bool)adminAtivo.Ativa)
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    var eventos = await connection.QueryAsync(
        "SELECT Id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao, ImagemUrl FROM Eventos WHERE CriadoPorCpf = @Cpf ORDER BY DataEvento DESC",
        new { Cpf = auth!.Cpf });

    return Results.Ok(eventos);
})
.WithName("ListarMeusEventos")
.WithDescription("Lista apenas os eventos do administrador autenticado (deve estar ativo)")
.Produces(200).Produces(401).Produces(403);

app.MapPost("/api/eventos", async (CriarEventoRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    
    var adminAtivo = await connection.QueryFirstOrDefaultAsync(
        "SELECT Ativa FROM Usuarios WHERE Cpf = @Cpf AND TipoConta = 'adm'",
        new { Cpf = auth!.Cpf });
    
    if (adminAtivo is null || !(bool)adminAtivo.Ativa)
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    await connection.ExecuteAsync(@"
        INSERT INTO Eventos (Nome, CapacidadeTotal, DataEvento, PrecoPadrao, CriadoPorCpf, ImagemUrl)
        VALUES (@Nome, @CapacidadeTotal, @DataEvento, @PrecoPadrao, @CriadoPorCpf, @ImagemUrl)",
        new { request.Nome, request.CapacidadeTotal, request.DataEvento, request.PrecoPadrao, CriadoPorCpf = auth!.Cpf, request.ImagemUrl });

    return Results.Created("/api/eventos", null);
})
.WithName("CriarEvento")
.WithDescription("Cria evento vinculado ao administrador autenticado (deve estar ativo)")
.Produces(201).Produces(401).Produces(403);

// ── CUPONS ───────────────────────────────────────────────────────────────────

app.MapGet("/api/cupons", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    var cupons = await connection.QueryAsync(
        "SELECT Codigo, PorcentagemDesconto, ValorMinimoRegra FROM Cupons WHERE CriadoPorCpf = @Cpf ORDER BY Codigo",
        new { Cpf = auth!.Cpf });

    return Results.Ok(cupons);
})
.WithName("ListarMeusCupons")
.WithDescription("Lista apenas os cupons do administrador autenticado")
.Produces(200).Produces(401).Produces(403);

app.MapPost("/api/cupons", async (CriarCupomRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.ExecuteAsync(@"
        INSERT INTO Cupons (Codigo, PorcentagemDesconto, ValorMinimoRegra, CriadoPorCpf)
        VALUES (@Codigo, @PorcentagemDesconto, @ValorMinimoRegra, @CriadoPorCpf)",
        new { request.Codigo, request.PorcentagemDesconto, request.ValorMinimoRegra, CriadoPorCpf = auth!.Cpf });

    return Results.Created("/api/cupons", null);
})
.WithName("CriarCupom")
.WithDescription("Cria cupom vinculado ao administrador autenticado")
.Produces(201).Produces(401).Produces(403);

// ── RESERVAS ─────────────────────────────────────────────────────────────────

app.MapGet("/api/eventos/{id:int}/assentos-ocupados", async (int id) =>
{
    await using var connection = new NpgsqlConnection(connectionString);
    var rows = await connection.QueryAsync<string>(
        "SELECT Assento FROM Reservas WHERE EventoId = @Id AND Status != 'cancelada' AND Assento IS NOT NULL",
        new { Id = id });

    var ocupados = rows
        .SelectMany(a => a.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct()
        .ToList();

    return Results.Ok(ocupados);
})
.WithName("AssentosOcupados")
.WithDescription("Retorna ids dos assentos ocupados de um evento")
.Produces<List<string>>(200);

app.MapPost("/api/reservas", async (CriarReservaRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);

    // Busca o evento
    var evento = await connection.QueryFirstOrDefaultAsync(
        "SELECT id, nome, capacidadetotal, precopadrao FROM Eventos WHERE Id = @Id",
        new { Id = request.EventoId });

    if (evento is null)
        return Results.NotFound("Evento nao encontrado.");

    // Verifica vagas disponíveis
    var reservados = await connection.QueryFirstAsync<int>(
        "SELECT COUNT(1) FROM Reservas WHERE EventoId = @EventoId AND Status != 'cancelada'",
        new { EventoId = request.EventoId });

    if (reservados >= (int)evento.capacidadetotal)
        return Results.BadRequest("Evento sem vagas disponiveis.");

    // Verifica se algum dos assentos solicitados já está ocupado
    if (!string.IsNullOrWhiteSpace(request.Assento))
    {
        var assentosRow = await connection.QueryAsync<string>(
            "SELECT Assento FROM Reservas WHERE EventoId = @EventoId AND Status != 'cancelada' AND Assento IS NOT NULL",
            new { EventoId = request.EventoId });

        var ocupados = assentosRow
            .SelectMany(a => a.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet();

        var solicitados = request.Assento.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var conflito = solicitados.FirstOrDefault(s => ocupados.Contains(s));
        if (conflito is not null)
            return Results.BadRequest($"O assento '{conflito}' ja esta reservado.");
    }

    // Calcula preço com cupom opcional
    decimal precoFinal = (decimal)evento.precopadrao;
    string? codigoCupomAplicado = null;

    if (!string.IsNullOrWhiteSpace(request.CodigoCupom))
    {
        var cupom = await connection.QueryFirstOrDefaultAsync(
            "SELECT Codigo, PorcentagemDesconto, ValorMinimoRegra FROM Cupons WHERE Codigo = @Codigo",
            new { Codigo = request.CodigoCupom.Trim().ToUpperInvariant() });

        if (cupom is null)
            return Results.BadRequest("Cupom invalido.");

        if (precoFinal < (decimal)cupom.ValorMinimoRegra)
            return Results.BadRequest($"Preco minimo para este cupom e R$ {cupom.ValorMinimoRegra:F2}.");

        var desconto = precoFinal * ((decimal)cupom.PorcentagemDesconto / 100m);
        precoFinal -= desconto;
        codigoCupomAplicado = (string)cupom.Codigo;
    }

    // Cria a reserva
    var reservaId = await connection.QueryFirstAsync<int>(@"
        INSERT INTO Reservas (EventoId, UsuarioCpf, PrecoFinal, CupomCodigo, Status, CriadoEm, Assento)
        VALUES (@EventoId, @UsuarioCpf, @PrecoFinal, @CupomCodigo, 'confirmada', NOW(), @Assento)
        RETURNING Id",
        new
        {
            EventoId = request.EventoId,
            UsuarioCpf = auth!.Cpf,
            PrecoFinal = precoFinal,
            CupomCodigo = codigoCupomAplicado,
            Assento = string.IsNullOrWhiteSpace(request.Assento) ? null : request.Assento.Trim()
        });

    return Results.Created($"/api/reservas/{reservaId}", new
    {
        Id = reservaId,
        EventoId = request.EventoId,
        NomeEvento = (string)evento.nome,
        PrecoOriginal = (decimal)evento.precopadrao,
        PrecoFinal = precoFinal,
        CupomAplicado = codigoCupomAplicado,
        Status = "confirmada"
    });
})
.WithName("CriarReserva")
.WithDescription("Reserva ingresso para um evento, com cupom opcional")
.Produces(201).Produces(400).Produces(401).Produces(404);

app.MapGet("/api/reservas", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    var reservas = await connection.QueryAsync(@"
        SELECT r.Id, r.EventoId, e.Nome AS NomeEvento, e.DataEvento,
               r.PrecoFinal, r.CupomCodigo, r.Status, r.CriadoEm
        FROM Reservas r
        INNER JOIN Eventos e ON e.Id = r.EventoId
        WHERE r.UsuarioCpf = @Cpf
        ORDER BY r.CriadoEm DESC",
        new { Cpf = auth!.Cpf });

    return Results.Ok(reservas);
})
.WithName("MinhasReservas")
.WithDescription("Lista todas as reservas do usuario autenticado")
.Produces(200).Produces(401);

app.MapDelete("/api/reservas/{id:int}", async (int id, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    var reserva = await connection.QueryFirstOrDefaultAsync(
        "SELECT Id, UsuarioCpf, Status FROM Reservas WHERE Id = @Id",
        new { Id = id });

    if (reserva is null)
        return Results.NotFound("Reserva nao encontrada.");

    if ((string)reserva.UsuarioCpf != auth!.Cpf)
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    if ((string)reserva.Status == "cancelada")
        return Results.BadRequest("Reserva ja esta cancelada.");

    await connection.ExecuteAsync(
        "UPDATE Reservas SET Status = 'cancelada' WHERE Id = @Id",
        new { Id = id });

    return Results.Ok(new { mensagem = "Reserva cancelada com sucesso." });
})
.WithName("CancelarReserva")
.WithDescription("Cancela uma reserva do usuario autenticado")
.Produces(200).Produces(400).Produces(401).Produces(403).Produces(404);

// ── PERFIL / USUARIOS / AUTH ──────────────────────────────────────────────────

app.MapGet("/api/me", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    var usuario = await connection.QueryFirstOrDefaultAsync(
        "SELECT Cpf, Nome, Email, TipoConta, Telefone, FotoPerfil FROM Usuarios WHERE Cpf = @Cpf",
        new { Cpf = auth!.Cpf });

    return usuario is null ? Results.NotFound() : Results.Ok(usuario);
})
.WithName("MeuPerfil")
.WithDescription("Retorna apenas os dados do usuario autenticado")
.Produces(200).Produces(401).Produces(404);

app.MapPut("/api/me", async (AtualizarPerfilRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null) return authError;

    if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest("Nome e email sao obrigatorios.");

    await using var connection = new NpgsqlConnection(connectionString);
    
    await connection.ExecuteAsync(@"
        UPDATE Usuarios 
        SET Nome = @Nome,
            Email = @Email,
            Telefone = @Telefone
        WHERE Cpf = @Cpf",
        new { Cpf = auth!.Cpf, request.Nome, request.Email, request.Telefone });

    var usuarioAtualizado = await connection.QueryFirstOrDefaultAsync(
        "SELECT Cpf, Nome, Email, TipoConta, Telefone, FotoPerfil FROM Usuarios WHERE Cpf = @Cpf",
        new { Cpf = auth!.Cpf });

    return Results.Ok(usuarioAtualizado);
})
.WithName("AtualizarPerfil")
.WithDescription("Atualiza dados do perfil do usuario autenticado")
.Produces(200).Produces(400).Produces(401).Produces(404);

app.MapPost("/api/me/foto", async (HttpContext httpContext, ILogger<Program> logger) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null) return authError;

    var cpfNormalizado = NormalizeCpf(auth!.Cpf);
    if (cpfNormalizado.Length != 11)
        return Results.Unauthorized();

    if (!httpContext.Request.HasFormContentType)
        return Results.BadRequest("Requisicao deve ser multipart/form-data.");

    var file = httpContext.Request.Form.Files.GetFile("file");

    if (file is null || file.Length == 0)
        return Results.BadRequest("Arquivo invalido.");

    const long maxBytes = 5 * 1024 * 1024; // 5 MB
    if (file.Length > maxBytes)
        return Results.BadRequest("Arquivo muito grande. Tamanho maximo: 5MB.");

    var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

    if (!extensoesPermitidas.Contains(ext))
        return Results.BadRequest("Formato invalido. Use JPG, PNG ou WebP.");

    var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagens", "perfis");
    if (!Directory.Exists(pasta))
        Directory.CreateDirectory(pasta);

    var nomeArquivo = Guid.NewGuid().ToString("N") + ext;
    var caminhoCompleto = Path.Combine(pasta, nomeArquivo);

    await using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var url = $"/imagens/perfis/{nomeArquivo}";

    await using var connection = new NpgsqlConnection(connectionString);
    
    // 🔥 LOGS DE DEBUG FINAL
    logger.LogInformation("🔥 URL SALVA: {Url}", url);
    logger.LogInformation("🔥 CPF USADO: {Cpf}", cpfNormalizado);
    logger.LogInformation("🔥 CONNECTION: {Connection}", ExtractConnectionInfo(connectionString));
    logger.LogInformation("Upload foto perfil - CPF token normalizado: '{Cpf}'", cpfNormalizado);

    var rows = await connection.ExecuteAsync(@"
        UPDATE Usuarios
        SET FotoPerfil = @FotoPerfil
        WHERE regexp_replace(trim(Cpf), '[^0-9]', '', 'g') = @Cpf",
        new { FotoPerfil = url, Cpf = cpfNormalizado });

    logger.LogInformation("🔥 ROWS AFETADAS: {Rows}", rows);
    logger.LogInformation("Upload foto perfil - Rows afetadas: {Rows}", rows);

    if (rows == 0)
        return Results.NotFound("Usuario autenticado nao encontrado para persistir foto de perfil.");

    return Results.Ok(new { url });
})
.WithName("UploadFotoPerfil")
.WithDescription("Faz upload da foto de perfil e salva o caminho no banco")
.Produces(200).Produces(400).Produces(401).Produces(404);

app.MapDelete("/api/me", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    
    await connection.ExecuteAsync(
        "DELETE FROM Usuarios WHERE Cpf = @Cpf",
        new { Cpf = auth!.Cpf });

    return Results.Ok(new { mensagem = "Conta excluida com sucesso." });
})
.WithName("DeletarConta")
.WithDescription("Deleta a conta do usuario autenticado")
.Produces(200).Produces(401);

app.MapPost("/api/usuarios", async (CriarUsuarioRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out _, "adm");
    if (authError is not null) return authError;

    var tipoConta = NormalizeAccountType(request.TipoConta);
    if (tipoConta is null)
        return Results.BadRequest("TipoConta invalido. Use 'usuario' ou 'adm'.");

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf, request.Nome, request.Email, request.Senha, tipoConta));
})
.WithName("CriarUsuario")
.WithDescription("Cria usuario ou administrador (somente administrador autenticado)")
.Produces(201).Produces(400).Produces(401).Produces(403);

app.MapPost("/api/auth/register", async (RegistrarRequest request) =>
{
    if (!string.IsNullOrWhiteSpace(request.TipoConta)
        && !string.Equals(request.TipoConta, "usuario", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Cadastro publico cria apenas conta do tipo usuario.");

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf, request.Nome, request.Email, request.Senha, "usuario"));
})
.WithName("Registrar")
.WithDescription("Registra um novo usuario comum")
.Produces(201).Produces(400);

app.MapPost("/api/auth/register-admin", async (RegistrarAdminRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out _, "adm");
    if (authError is not null) return authError;

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf, request.Nome, request.Email, request.Senha, "adm"));
})
.WithName("RegistrarAdmin")
.WithDescription("Registra novo administrador (somente administrador autenticado)")
.Produces(201).Produces(400).Produces(401).Produces(403);

app.MapPost("/api/auth/bootstrap-admin", async (BootstrapAdminRequest request) =>
{
    if (request.ChaveInstalacao != bootstrapAdminKey)
        return Results.Unauthorized();

    await using var connection = new NpgsqlConnection(connectionString);
    var adminsExistentes = await connection.QueryFirstAsync<int>(
        "SELECT COUNT(1) FROM Usuarios WHERE TipoConta = 'adm'");

    if (adminsExistentes > 0)
        return Results.BadRequest("Ja existe administrador cadastrado. Use /api/auth/register-admin.");

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf, request.Nome, request.Email, request.Senha, "adm"));
})
.WithName("BootstrapAdmin")
.WithDescription("Cria o primeiro administrador da base")
.Produces(201).Produces(400).Produces(401);

app.MapPost("/api/auth/login", async (LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Senha))
        return Results.BadRequest("Informe usuario e senha.");

    var cpfNormalizado = NormalizeCpf(request.Usuario);

    await using var connection = new NpgsqlConnection(connectionString);
    var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioLogin>(@"
        SELECT Cpf, Nome, Email, Telefone, FotoPerfil, SenhaHash, TipoConta
        FROM Usuarios
        WHERE regexp_replace(trim(Cpf), '[^0-9]', '', 'g') = @Cpf
           OR lower(Email) = lower(@Email)",
        new { Cpf = cpfNormalizado, Email = request.Usuario });

    if (usuario is null || string.IsNullOrWhiteSpace(usuario.SenhaHash))
        return Results.Unauthorized();

    if (!string.Equals(usuario.SenhaHash, ComputeSha256(request.Senha), StringComparison.Ordinal))
        return Results.Unauthorized();

    var tipoConta = NormalizeAccountType(usuario.TipoConta) ?? "usuario";
    var token = GenerateToken(tokenSecret, new TokenPayload(
        usuario.Cpf, tipoConta,
        DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds()));

    return Results.Ok(new LoginResponse(
        usuario.Cpf,
        usuario.Nome,
        usuario.Email,
        tipoConta,
        token,
        usuario.Telefone,
        usuario.FotoPerfil));
})
.WithName("Login")
.WithDescription("Realiza login com CPF/e-mail e senha")
.Produces<LoginResponse>(200).Produces(401).Produces(400);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Run($"http://0.0.0.0:{port}");
}
else
{
    app.Run();
}

// ── HELPERS ───────────────────────────────────────────────────────────────────

static async Task<IResult> CreateAccountAsync(string connectionString, AccountCreateData data)
{
    if (string.IsNullOrWhiteSpace(data.Nome) || string.IsNullOrWhiteSpace(data.Cpf)
        || string.IsNullOrWhiteSpace(data.Email) || string.IsNullOrWhiteSpace(data.Senha))
        return Results.BadRequest("Todos os campos sao obrigatorios.");

    var cpfNormalizado = NormalizeCpf(data.Cpf);
    if (cpfNormalizado.Length != 11)
        return Results.BadRequest("CPF deve conter 11 digitos.");

    await using var connection = new NpgsqlConnection(connectionString);
    var existente = await connection.QueryFirstOrDefaultAsync(
        "SELECT 1 FROM Usuarios WHERE Cpf = @Cpf OR Email = @Email",
        new { Cpf = cpfNormalizado, data.Email });

    if (existente is not null)
        return Results.BadRequest("Ja existe conta com este CPF ou e-mail.");

    await connection.ExecuteAsync(@"
        INSERT INTO Usuarios (Cpf, Nome, Email, SenhaHash, TipoConta)
        VALUES (@Cpf, @Nome, @Email, @SenhaHash, @TipoConta)",
        new { Cpf = cpfNormalizado, data.Nome, data.Email, SenhaHash = ComputeSha256(data.Senha), data.TipoConta });

    return Results.Created("/api/usuarios", new { mensagem = "Conta criada com sucesso.", tipoConta = data.TipoConta });
}

static IResult? TryAuthenticate(HttpContext httpContext, string tokenSecret, out TokenPayload? auth, params string[] requiredRoles)
{
    auth = null;

    var authorization = httpContext.Request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return Results.Unauthorized();

    var token = authorization["Bearer ".Length..].Trim();
    if (!TryValidateToken(tokenSecret, token, out var payload))
        return Results.Unauthorized();

    if (requiredRoles.Length > 0 && !requiredRoles.Contains(payload!.TipoConta, StringComparer.OrdinalIgnoreCase))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    auth = payload;
    return null;
}

static string NormalizeCpf(string value) =>
    new string(value.Where(char.IsDigit).ToArray());

static string ExtractConnectionInfo(string connectionString)
{
    try
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return $"Host={builder.Host} Database={builder.Database} User={builder.Username}";
    }
    catch
    {
        return "Connection string inválida ou não pode ser parseada";
    }
}

static string? NormalizeAccountType(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return null;
    var normalized = value.Trim().ToLowerInvariant();
    return normalized is "usuario" or "adm" ? normalized : null;
}

static string? ResolveConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? configuration["DATABASE_URL"];

    if (!string.IsNullOrWhiteSpace(databaseUrl))
        return BuildNpgsqlConnectionStringFromDatabaseUrl(databaseUrl);

    return configuration.GetConnectionString("NeonDB")
        ?? configuration.GetConnectionString("DefaultConnection");
}

static string BuildNpgsqlConnectionStringFromDatabaseUrl(string databaseUrl)
{
    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        throw new InvalidOperationException("DATABASE_URL invalida.");

    if (!string.Equals(uri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(uri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("DATABASE_URL deve usar esquema postgres/postgresql.");

    var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

    var connectionBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.Trim('/'),
        Username = username,
        Password = password,
        SslMode = SslMode.Require
    };

    var query = uri.Query.TrimStart('?');
    if (!string.IsNullOrWhiteSpace(query))
    {
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = pair.Split('=', 2, StringSplitOptions.None);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLowerInvariant();
            var value = Uri.UnescapeDataString(kv[1]).Trim();

            if (key == "sslmode")
            {
                connectionBuilder.SslMode = value.ToLowerInvariant() switch
                {
                    "disable" => SslMode.Disable,
                    "allow" => SslMode.Allow,
                    "prefer" => SslMode.Prefer,
                    "require" => SslMode.Require,
                    "verifyca" or "verify-ca" => SslMode.VerifyCA,
                    "verifyfull" or "verify-full" => SslMode.VerifyFull,
                    _ => SslMode.Require
                };
            }
        }
    }

    return connectionBuilder.ConnectionString;
}

static string ComputeSha256(string input)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes);
}

static string GenerateToken(string secret, TokenPayload payload)
{
    var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
    return $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(Sign(payloadBytes, secret))}";
}

static bool TryValidateToken(string secret, string token, out TokenPayload? payload)
{
    payload = null;
    var parts = token.Split('.');
    if (parts.Length != 2) return false;

    if (!TryBase64UrlDecode(parts[0], out var payloadBytes) || !TryBase64UrlDecode(parts[1], out var providedSignature))
        return false;

    if (!CryptographicOperations.FixedTimeEquals(Sign(payloadBytes, secret), providedSignature))
        return false;

    try { payload = JsonSerializer.Deserialize<TokenPayload>(payloadBytes); }
    catch { return false; }

    if (payload is null || string.IsNullOrWhiteSpace(payload.Cpf) || string.IsNullOrWhiteSpace(payload.TipoConta))
        return false;

    var tipo = NormalizeAccountType(payload.TipoConta);
    if (tipo is null) return false;

    var cpfNormalizado = NormalizeCpf(payload.Cpf);
    if (cpfNormalizado.Length != 11) return false;

    payload = payload with { Cpf = cpfNormalizado, TipoConta = tipo };
    return payload.ExpiraEmUnix > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

static byte[] Sign(byte[] data, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    return hmac.ComputeHash(data);
}

static string Base64UrlEncode(byte[] data) =>
    Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

static bool TryBase64UrlDecode(string input, out byte[] data)
{
    try
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (base64.Length % 4);
        if (padding is > 0 and < 4) base64 += new string('=', padding);
        data = Convert.FromBase64String(base64);
        return true;
    }
    catch { data = Array.Empty<byte>(); return false; }
}

static async Task EnsureAuthSchemaAsync(string connectionString, ILogger logger)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS Usuarios (
            Cpf VARCHAR(11) PRIMARY KEY,
            Nome TEXT NOT NULL,
            Email TEXT NOT NULL UNIQUE,
            SenhaHash TEXT,
            TipoConta TEXT NOT NULL DEFAULT 'usuario',
            Ativa BOOLEAN DEFAULT TRUE
        );
        ALTER TABLE Usuarios ADD COLUMN IF NOT EXISTS Ativa BOOLEAN DEFAULT TRUE;
        ALTER TABLE Usuarios ADD COLUMN IF NOT EXISTS Telefone TEXT;
        ALTER TABLE Usuarios ADD COLUMN IF NOT EXISTS FotoPerfil TEXT;
        CREATE TABLE IF NOT EXISTS Eventos (
            Id SERIAL PRIMARY KEY,
            Nome TEXT NOT NULL,
            CapacidadeTotal INTEGER NOT NULL,
            DataEvento TIMESTAMPTZ NOT NULL,
            PrecoPadrao NUMERIC(10,2) NOT NULL,
            CriadoPorCpf VARCHAR(11) NOT NULL REFERENCES Usuarios(Cpf)
        );
        ALTER TABLE Eventos ADD COLUMN IF NOT EXISTS ImagemUrl TEXT;
        CREATE TABLE IF NOT EXISTS Cupons (
            Codigo TEXT PRIMARY KEY,
            PorcentagemDesconto NUMERIC(5,2) NOT NULL,
            ValorMinimoRegra NUMERIC(10,2) NOT NULL,
            CriadoPorCpf VARCHAR(11) REFERENCES Usuarios(Cpf)
        );
        CREATE TABLE IF NOT EXISTS Reservas (
            Id SERIAL PRIMARY KEY,
            EventoId INTEGER NOT NULL REFERENCES Eventos(Id),
            UsuarioCpf VARCHAR(11) NOT NULL REFERENCES Usuarios(Cpf),
            PrecoFinal NUMERIC(10,2) NOT NULL,
            CupomCodigo TEXT,
            Status TEXT NOT NULL DEFAULT 'confirmada',
            CriadoEm TIMESTAMPTZ NOT NULL,
            Assento TEXT
        );
        ALTER TABLE Reservas ADD COLUMN IF NOT EXISTS Assento TEXT;
    ");

    var eventosSemCriador = await connection.ExecuteScalarAsync<int>(@"
        SELECT COUNT(1)
        FROM Eventos
        WHERE CriadoPorCpf IS NULL");

    if (eventosSemCriador == 0)
    {
        await connection.ExecuteAsync("ALTER TABLE Eventos ALTER COLUMN CriadoPorCpf SET NOT NULL;");
    }
    else
    {
        logger.LogWarning(
            "Existem {Quantidade} eventos sem CriadoPorCpf. Corrija os dados antigos antes de aplicar NOT NULL em Eventos.CriadoPorCpf.",
            eventosSemCriador);
    }
}

record CriarEventoRequest(string Nome, int CapacidadeTotal, DateTime DataEvento, decimal PrecoPadrao, string? ImagemUrl);
record CriarCupomRequest(string Codigo, decimal PorcentagemDesconto, decimal ValorMinimoRegra);
record CriarReservaRequest(int EventoId, string? CodigoCupom, string? Assento);
record CriarUsuarioRequest(string Cpf, string Nome, string Email, string Senha, string TipoConta);
record RegistrarRequest(string Nome, string Cpf, string Email, string Senha, string? TipoConta);
record RegistrarAdminRequest(string Nome, string Cpf, string Email, string Senha);
record BootstrapAdminRequest(string Nome, string Cpf, string Email, string Senha, string ChaveInstalacao);
record AtualizarPerfilRequest(string Nome, string Email, string? Telefone, string? FotoPerfil);
record LoginRequest(string Usuario, string Senha);
record LoginResponse(string Cpf, string Nome, string Email, string TipoConta, string Token, string? Telefone, string? FotoPerfil);
record TokenPayload(string Cpf, string TipoConta, long ExpiraEmUnix);
record AccountCreateData(string Cpf, string Nome, string Email, string Senha, string TipoConta);

class UsuarioLogin
{
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? FotoPerfil { get; set; }
    public string? SenhaHash { get; set; }
    public string TipoConta { get; set; } = "usuario";
}
