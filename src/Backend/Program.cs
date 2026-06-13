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
        Title = "Backend API",
        Version = "v1",
        Description = "API para gerenciamento de eventos e ingressos"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backend API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowFrontend");
app.UseStaticFiles();

// ✅ HEALTH CHECK E ROTA RAIZ
app.MapGet("/", () => Results.Ok(new { status = "API online", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheckRoot")
    .WithDescription("Verifica se a API está respondendo");

app.MapGet("/health", () => Results.Ok(new { ok = true, timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithDescription("Health check endpoint");

var connectionString = ResolveConnectionString(builder.Configuration)
    ?? throw new InvalidOperationException("Connection string nao encontrada. Configure 'ConnectionStrings:DefaultConnection' ou 'DATABASE_URL'.");

app.Logger.LogInformation("Conexao PostgreSQL configurada: {Conn}", ExtractConnectionInfo(connectionString));

var tokenSecret = builder.Configuration["Auth:TokenSecret"] ?? "ticketprime-dev-token-secret-change-this";
var bootstrapAdminKey = builder.Configuration["Auth:BootstrapAdminKey"] ?? "ticketprime-bootstrap-admin";

try
{
    await EnsureAuthSchemaAsync(connectionString, app.Logger);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Nao foi possivel preparar o esquema no banco durante a inicializacao. A API vai continuar subindo, mas as rotas que dependem do banco podem falhar.");
}

// ── EVENTOS ──────────────────────────────────────────────────────────────────

app.MapGet("/api/eventos/publico", async (string? categoria) =>
{
    var categoriaNormalizada = NormalizeEventCategory(categoria);
    if (!string.IsNullOrWhiteSpace(categoria) && categoriaNormalizada is null)
        return Results.BadRequest("Categoria invalida. Use: musicais, cinema, eventos-diversos ou viagens.");

    await using var connection = new NpgsqlConnection(connectionString);
    var eventos = categoriaNormalizada is null
        ? await connection.QueryAsync(@"
        SELECT e.Id, e.Nome, e.CapacidadeTotal, e.DataEvento, e.PrecoPadrao, e.ImagemUrl, e.Categoria
        FROM Eventos e
        INNER JOIN Usuarios u ON e.CriadoPorCpf = u.Cpf
        WHERE u.Ativa = TRUE
                    AND e.Ativo = TRUE
        ORDER BY e.DataEvento DESC")
        : await connection.QueryAsync(@"
        SELECT e.Id, e.Nome, e.CapacidadeTotal, e.DataEvento, e.PrecoPadrao, e.ImagemUrl, e.Categoria
        FROM Eventos e
        INNER JOIN Usuarios u ON e.CriadoPorCpf = u.Cpf
        WHERE u.Ativa = TRUE
                    AND e.Ativo = TRUE
          AND e.Categoria = @Categoria
        ORDER BY e.DataEvento DESC", new { Categoria = categoriaNormalizada });
    return Results.Ok(eventos);
})
.WithName("ListarEventosPublicos")
.WithDescription("Lista eventos de admins ativos para exibicao publica")
.Produces(200);

app.MapGet("/api/eventos", async (string? categoria) =>
{
    var categoriaNormalizada = NormalizeEventCategory(categoria);
    if (!string.IsNullOrWhiteSpace(categoria) && categoriaNormalizada is null)
        return Results.BadRequest("Categoria invalida. Use: musicais, cinema, eventos-diversos ou viagens.");

    await using var connection = new NpgsqlConnection(connectionString);
    var eventos = categoriaNormalizada is null
        ? await connection.QueryAsync(@"
        SELECT Id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao, ImagemUrl, Categoria
        FROM Eventos
                WHERE Ativo = TRUE
        ORDER BY DataEvento DESC")
        : await connection.QueryAsync(@"
        SELECT Id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao, ImagemUrl, Categoria
        FROM Eventos
                WHERE Ativo = TRUE
                    AND Categoria = @Categoria
        ORDER BY DataEvento DESC", new { Categoria = categoriaNormalizada });
    return Results.Ok(eventos);
})
.AllowAnonymous()
.WithName("ListarEventos")
.WithDescription("Lista todos os eventos disponíveis")
.Produces(200);

app.MapGet("/api/eventos/categorias", () => Results.Ok(new[]
{
    "musicais",
    "cinema",
    "eventos-diversos",
    "viagens"
}))
.AllowAnonymous()
.WithName("CategoriasEvento")
.WithDescription("Lista categorias de evento aceitas pela API")
.Produces(200);

app.MapPost("/api/eventos", async (CriarEventoRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    if (string.IsNullOrWhiteSpace(request.Nome))
        return Results.BadRequest("Nome do evento é obrigatório");
    if (request.CapacidadeTotal <= 0)
        return Results.BadRequest("Capacidade deve ser maior que zero");
    if (request.PrecoPadrao < 0)
        return Results.BadRequest("Preço não pode ser negativo");

    var categoriaNormalizada = NormalizeEventCategory(request.Categoria);
    if (!string.IsNullOrWhiteSpace(request.Categoria) && categoriaNormalizada is null)
        return Results.BadRequest("Categoria invalida. Use: musicais, cinema, eventos-diversos ou viagens.");

    categoriaNormalizada ??= "musicais";

    await using var connection = new NpgsqlConnection(connectionString);
    
    var adminAtivo = await connection.QueryFirstOrDefaultAsync(
        "SELECT Ativa FROM Usuarios WHERE Cpf = @Cpf AND TipoConta = 'adm'",
        new { Cpf = auth!.Cpf });
    
    if (adminAtivo is null || !(bool)adminAtivo.Ativa)
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    await connection.ExecuteAsync(@"
        INSERT INTO Eventos (Nome, CapacidadeTotal, DataEvento, PrecoPadrao, CriadoPorCpf, ImagemUrl, Categoria)
        VALUES (@Nome, @CapacidadeTotal, @DataEvento, @PrecoPadrao, @CriadoPorCpf, @ImagemUrl, @Categoria)",
        new
        {
            request.Nome,
            request.CapacidadeTotal,
            request.DataEvento,
            request.PrecoPadrao,
            CriadoPorCpf = auth!.Cpf,
            request.ImagemUrl,
            Categoria = categoriaNormalizada
        });

    return Results.Created("/api/eventos", null);
})
.WithName("CriarEvento")
.WithDescription("Cria evento vinculado ao administrador autenticado (deve estar ativo)")
.Produces(201).Produces(401).Produces(403);

app.MapPut("/api/eventos/{id:int}", async (int id, AtualizarEventoRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    if (string.IsNullOrWhiteSpace(request.Nome))
        return Results.BadRequest("Nome do evento e obrigatorio.");
    if (request.CapacidadeTotal <= 0)
        return Results.BadRequest("Capacidade deve ser maior que zero.");
    if (request.PrecoPadrao < 0)
        return Results.BadRequest("Preco nao pode ser negativo.");

    var categoriaNormalizada = NormalizeEventCategory(request.Categoria);
    if (!string.IsNullOrWhiteSpace(request.Categoria) && categoriaNormalizada is null)
        return Results.BadRequest("Categoria invalida. Use: musicais, cinema, eventos-diversos ou viagens.");

    categoriaNormalizada ??= "musicais";

    await using var connection = new NpgsqlConnection(connectionString);

    var evento = await connection.QueryFirstOrDefaultAsync(@"
        SELECT Id, CriadoPorCpf
        FROM Eventos
        WHERE Id = @Id", new { Id = id });

    if (evento is null)
        return Results.NotFound("Evento nao encontrado.");

    if (!string.Equals((string)evento.CriadoPorCpf, auth!.Cpf, StringComparison.Ordinal))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    var reservasAtivas = await connection.QueryFirstAsync<int>(
        "SELECT COUNT(1) FROM Reservas WHERE EventoId = @EventoId AND Status != 'cancelada'",
        new { EventoId = id });

    if (request.CapacidadeTotal < reservasAtivas)
        return Results.BadRequest($"Capacidade total nao pode ser menor que o numero de reservas ativas ({reservasAtivas}).");

    await connection.ExecuteAsync(@"
        UPDATE Eventos
        SET Nome = @Nome,
            CapacidadeTotal = @CapacidadeTotal,
            DataEvento = @DataEvento,
            PrecoPadrao = @PrecoPadrao,
            ImagemUrl = @ImagemUrl,
            Categoria = @Categoria
        WHERE Id = @Id",
        new
        {
            Id = id,
            request.Nome,
            request.CapacidadeTotal,
            request.DataEvento,
            request.PrecoPadrao,
            request.ImagemUrl,
            Categoria = categoriaNormalizada
        });

    if (!string.Equals(categoriaNormalizada, "viagens", StringComparison.Ordinal))
    {
        await connection.ExecuteAsync(
            "DELETE FROM EventosViagem WHERE EventoId = @EventoId",
            new { EventoId = id });
    }

    return Results.Ok(new
    {
        Id = id,
        request.Nome,
        request.CapacidadeTotal,
        request.DataEvento,
        request.PrecoPadrao,
        request.ImagemUrl,
        Categoria = categoriaNormalizada
    });
})
.WithName("AtualizarEvento")
.WithDescription("Atualiza evento do administrador autenticado")
.Produces(200).Produces(400).Produces(401).Produces(403).Produces(404);

app.MapDelete("/api/eventos/{id:int}", async (int id, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);

    var evento = await connection.QueryFirstOrDefaultAsync(@"
        SELECT Id, CriadoPorCpf, Ativo
        FROM Eventos
        WHERE Id = @Id", new { Id = id });

    if (evento is null)
        return Results.NotFound("Evento nao encontrado.");

    if (!string.Equals((string)evento.CriadoPorCpf, auth!.Cpf, StringComparison.Ordinal))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    if (!(bool)evento.Ativo)
        return Results.BadRequest("Evento ja esta inativo.");

    var reservasAtivas = await connection.QueryFirstAsync<int>(
        "SELECT COUNT(1) FROM Reservas WHERE EventoId = @EventoId AND Status != 'cancelada'",
        new { EventoId = id });

    if (reservasAtivas > 0)
        return Results.BadRequest("Nao e possivel inativar evento com reservas ativas.");

    await connection.ExecuteAsync("UPDATE Eventos SET Ativo = FALSE WHERE Id = @Id", new { Id = id });
    await connection.ExecuteAsync("UPDATE TiposIngresso SET Ativo = FALSE WHERE EventoId = @EventoId", new { EventoId = id });

    return Results.Ok(new { mensagem = "Evento inativado com sucesso." });
})
.WithName("InativarEvento")
.WithDescription("Inativa evento do administrador autenticado quando nao houver reservas ativas")
.Produces(200).Produces(400).Produces(401).Produces(403).Produces(404);

// ── DETALHES DE VIAGEM ──────────────────────────────────────────────────────

app.MapGet("/api/eventos/{eventoId:int}/viagem", async (int eventoId) =>
{
    await using var connection = new NpgsqlConnection(connectionString);

    var evento = await connection.QueryFirstOrDefaultAsync(@"
        SELECT Id, Categoria, Ativo
        FROM Eventos
        WHERE Id = @EventoId", new { EventoId = eventoId });

    if (evento is null || !(bool)evento.Ativo)
        return Results.NotFound("Evento nao encontrado.");

    var categoria = NormalizeEventCategory((string?)evento.Categoria) ?? "musicais";
    if (!string.Equals(categoria, "viagens", StringComparison.Ordinal))
        return Results.BadRequest("Detalhes de viagem sao permitidos apenas para eventos da categoria viagens.");

    var detalhes = await connection.QueryFirstOrDefaultAsync(@"
        SELECT EventoId, Origem, Destino, Companhia, PartidaEm, ChegadaEm, BagagemIncluida, Observacoes
        FROM EventosViagem
        WHERE EventoId = @EventoId", new { EventoId = eventoId });

    if (detalhes is null)
        return Results.NotFound("Detalhes de viagem nao cadastrados para este evento.");

    return Results.Ok(detalhes);
})
.AllowAnonymous()
.WithName("ObterDetalhesViagem")
.WithDescription("Retorna os detalhes de viagem de um evento da categoria viagens")
.Produces(200).Produces(400).Produces(404);

app.MapPut("/api/eventos/{eventoId:int}/viagem", async (int eventoId, AtualizarDetalhesViagemRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    if (string.IsNullOrWhiteSpace(request.Origem)
        || string.IsNullOrWhiteSpace(request.Destino)
        || string.IsNullOrWhiteSpace(request.Companhia))
        return Results.BadRequest("Origem, destino e companhia sao obrigatorios.");

    if (request.ChegadaEm <= request.PartidaEm)
        return Results.BadRequest("ChegadaEm deve ser maior que PartidaEm.");

    await using var connection = new NpgsqlConnection(connectionString);

    var evento = await connection.QueryFirstOrDefaultAsync(@"
        SELECT Id, CriadoPorCpf, Categoria, Ativo
        FROM Eventos
        WHERE Id = @EventoId", new { EventoId = eventoId });

    if (evento is null)
        return Results.NotFound("Evento nao encontrado.");

    if (!string.Equals((string)evento.CriadoPorCpf, auth!.Cpf, StringComparison.Ordinal))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    if (!(bool)evento.Ativo)
        return Results.BadRequest("Nao e possivel atualizar detalhes de viagem para evento inativo.");

    var categoria = NormalizeEventCategory((string?)evento.Categoria) ?? "musicais";
    if (!string.Equals(categoria, "viagens", StringComparison.Ordinal))
        return Results.BadRequest("Detalhes de viagem sao permitidos apenas para eventos da categoria viagens.");

    var origem = request.Origem.Trim();
    var destino = request.Destino.Trim();
    var companhia = request.Companhia.Trim();
    var observacoes = string.IsNullOrWhiteSpace(request.Observacoes) ? null : request.Observacoes.Trim();

    await connection.ExecuteAsync(@"
        INSERT INTO EventosViagem (EventoId, Origem, Destino, Companhia, PartidaEm, ChegadaEm, BagagemIncluida, Observacoes)
        VALUES (@EventoId, @Origem, @Destino, @Companhia, @PartidaEm, @ChegadaEm, @BagagemIncluida, @Observacoes)
        ON CONFLICT (EventoId) DO UPDATE
        SET Origem = EXCLUDED.Origem,
            Destino = EXCLUDED.Destino,
            Companhia = EXCLUDED.Companhia,
            PartidaEm = EXCLUDED.PartidaEm,
            ChegadaEm = EXCLUDED.ChegadaEm,
            BagagemIncluida = EXCLUDED.BagagemIncluida,
            Observacoes = EXCLUDED.Observacoes",
        new
        {
            EventoId = eventoId,
            Origem = origem,
            Destino = destino,
            Companhia = companhia,
            request.PartidaEm,
            request.ChegadaEm,
            request.BagagemIncluida,
            Observacoes = observacoes
        });

    return Results.Ok(new
    {
        EventoId = eventoId,
        Origem = origem,
        Destino = destino,
        Companhia = companhia,
        request.PartidaEm,
        request.ChegadaEm,
        request.BagagemIncluida,
        Observacoes = observacoes
    });
})
.WithName("SalvarDetalhesViagem")
.WithDescription("Cria ou atualiza detalhes de viagem de evento do administrador autenticado")
.Produces(200).Produces(400).Produces(401).Produces(403).Produces(404);

app.MapDelete("/api/eventos/{eventoId:int}/viagem", async (int eventoId, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);

    var evento = await connection.QueryFirstOrDefaultAsync(@"
        SELECT Id, CriadoPorCpf, Categoria
        FROM Eventos
        WHERE Id = @EventoId", new { EventoId = eventoId });

    if (evento is null)
        return Results.NotFound("Evento nao encontrado.");

    if (!string.Equals((string)evento.CriadoPorCpf, auth!.Cpf, StringComparison.Ordinal))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    var categoria = NormalizeEventCategory((string?)evento.Categoria) ?? "musicais";
    if (!string.Equals(categoria, "viagens", StringComparison.Ordinal))
        return Results.BadRequest("Detalhes de viagem sao permitidos apenas para eventos da categoria viagens.");

    var rows = await connection.ExecuteAsync(
        "DELETE FROM EventosViagem WHERE EventoId = @EventoId",
        new { EventoId = eventoId });

    if (rows == 0)
        return Results.NotFound("Detalhes de viagem nao cadastrados para este evento.");

    return Results.Ok(new { mensagem = "Detalhes de viagem removidos com sucesso." });
})
.WithName("RemoverDetalhesViagem")
.WithDescription("Remove detalhes de viagem de evento do administrador autenticado")
.Produces(200).Produces(400).Produces(401).Produces(403).Produces(404);

// ── TIPOS DE INGRESSO ───────────────────────────────────────────────────────

app.MapGet("/api/eventos/{eventoId:int}/tipos-ingresso", async (int eventoId) =>
{
    await using var connection = new NpgsqlConnection(connectionString);

    var eventoExiste = await connection.ExecuteScalarAsync<bool>(
        "SELECT EXISTS(SELECT 1 FROM Eventos WHERE Id = @EventoId AND Ativo = TRUE)",
        new { EventoId = eventoId });

    if (!eventoExiste)
        return Results.NotFound("Evento nao encontrado.");

    var tipos = await connection.QueryAsync(@"
        SELECT Id, EventoId, Nome, Preco, EstoqueTotal, Ativo
        FROM TiposIngresso
        WHERE EventoId = @EventoId AND Ativo = TRUE
        ORDER BY Preco ASC, Nome ASC",
        new { EventoId = eventoId });

    return Results.Ok(tipos);
})
.AllowAnonymous()
.WithName("ListarTiposIngressoPorEvento")
.WithDescription("Lista tipos de ingresso ativos de um evento")
.Produces(200).Produces(404);

app.MapGet("/api/tipos-ingresso", async (HttpContext httpContext, int? eventoId) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);

    var tipos = eventoId is null
        ? await connection.QueryAsync(@"
            SELECT t.Id, t.EventoId, e.Nome AS NomeEvento, t.Nome, t.Preco, t.EstoqueTotal, t.Ativo
            FROM TiposIngresso t
            INNER JOIN Eventos e ON e.Id = t.EventoId
            WHERE e.CriadoPorCpf = @Cpf
            ORDER BY t.EventoId DESC, t.Preco ASC", new { Cpf = auth!.Cpf })
        : await connection.QueryAsync(@"
            SELECT t.Id, t.EventoId, e.Nome AS NomeEvento, t.Nome, t.Preco, t.EstoqueTotal, t.Ativo
            FROM TiposIngresso t
            INNER JOIN Eventos e ON e.Id = t.EventoId
            WHERE e.CriadoPorCpf = @Cpf
              AND t.EventoId = @EventoId
            ORDER BY t.Preco ASC", new { Cpf = auth!.Cpf, EventoId = eventoId.Value });

    return Results.Ok(tipos);
})
.WithName("ListarTiposIngressoAdmin")
.WithDescription("Lista tipos de ingresso dos eventos do admin autenticado")
.Produces(200).Produces(401).Produces(403);

app.MapPost("/api/eventos/{eventoId:int}/tipos-ingresso", async (int eventoId, CriarTipoIngressoRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    if (string.IsNullOrWhiteSpace(request.Nome))
        return Results.BadRequest("Nome do tipo de ingresso e obrigatorio.");
    if (request.Preco < 0)
        return Results.BadRequest("Preco nao pode ser negativo.");
    if (request.EstoqueTotal <= 0)
        return Results.BadRequest("Estoque total deve ser maior que zero.");

    var nomeTipo = request.Nome.Trim();

    await using var connection = new NpgsqlConnection(connectionString);

    var evento = await connection.QueryFirstOrDefaultAsync(@"
        SELECT e.Id, e.CriadoPorCpf, e.Ativo, u.Ativa
        FROM Eventos e
        INNER JOIN Usuarios u ON u.Cpf = e.CriadoPorCpf
        WHERE e.Id = @EventoId", new { EventoId = eventoId });

    if (evento is null)
        return Results.NotFound("Evento nao encontrado.");

    if (!string.Equals((string)evento.CriadoPorCpf, auth!.Cpf, StringComparison.Ordinal))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    if (!(bool)evento.Ativo)
        return Results.BadRequest("Nao e possivel criar tipo de ingresso para evento inativo.");

    if (!(bool)evento.Ativa)
        return Results.BadRequest("Nao e possivel criar tipo de ingresso para evento de administrador inativo.");

    int tipoId;
    try
    {
        tipoId = await connection.QueryFirstAsync<int>(@"
            INSERT INTO TiposIngresso (EventoId, Nome, Preco, EstoqueTotal, Ativo)
            VALUES (@EventoId, @Nome, @Preco, @EstoqueTotal, TRUE)
            RETURNING Id",
            new
            {
                EventoId = eventoId,
                Nome = nomeTipo,
                request.Preco,
                request.EstoqueTotal
            });
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.BadRequest("Ja existe um tipo de ingresso com este nome para o evento informado.");
    }

    return Results.Created($"/api/tipos-ingresso/{tipoId}", new
    {
        Id = tipoId,
        EventoId = eventoId,
        Nome = nomeTipo,
        request.Preco,
        request.EstoqueTotal,
        Ativo = true
    });
})
.WithName("CriarTipoIngresso")
.WithDescription("Cria tipo de ingresso para um evento do administrador autenticado")
.Produces(201).Produces(400).Produces(401).Produces(403).Produces(404);

app.MapPut("/api/tipos-ingresso/{id:int}", async (int id, AtualizarTipoIngressoRequest request, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    if (string.IsNullOrWhiteSpace(request.Nome))
        return Results.BadRequest("Nome do tipo de ingresso e obrigatorio.");
    if (request.Preco < 0)
        return Results.BadRequest("Preco nao pode ser negativo.");
    if (request.EstoqueTotal <= 0)
        return Results.BadRequest("Estoque total deve ser maior que zero.");

    var nomeTipo = request.Nome.Trim();

    await using var connection = new NpgsqlConnection(connectionString);

    var tipo = await connection.QueryFirstOrDefaultAsync(@"
        SELECT t.Id, t.EventoId, t.Ativo, e.CriadoPorCpf
        FROM TiposIngresso t
        INNER JOIN Eventos e ON e.Id = t.EventoId
        WHERE t.Id = @Id", new { Id = id });

    if (tipo is null)
        return Results.NotFound("Tipo de ingresso nao encontrado.");

    if (!string.Equals((string)tipo.CriadoPorCpf, auth!.Cpf, StringComparison.Ordinal))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    var vendidosTipo = await connection.QueryFirstAsync<int>(
        "SELECT COUNT(1) FROM Reservas WHERE TipoIngressoId = @TipoIngressoId AND Status != 'cancelada'",
        new { TipoIngressoId = id });

    if (request.EstoqueTotal < vendidosTipo)
        return Results.BadRequest($"Estoque total nao pode ser menor que o numero de reservas ativas ({vendidosTipo}).");

    try
    {
        await connection.ExecuteAsync(@"
            UPDATE TiposIngresso
            SET Nome = @Nome,
                Preco = @Preco,
                EstoqueTotal = @EstoqueTotal,
                Ativo = @Ativo
            WHERE Id = @Id",
            new
            {
                Id = id,
                Nome = nomeTipo,
                request.Preco,
                request.EstoqueTotal,
                request.Ativo
            });
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Results.BadRequest("Ja existe um tipo de ingresso com este nome para o evento informado.");
    }

    return Results.Ok(new
    {
        Id = id,
        EventoId = (int)tipo.EventoId,
        Nome = nomeTipo,
        request.Preco,
        request.EstoqueTotal,
        request.Ativo
    });
})
.WithName("AtualizarTipoIngresso")
.WithDescription("Atualiza tipo de ingresso do administrador autenticado")
.Produces(200).Produces(400).Produces(401).Produces(403).Produces(404);

app.MapDelete("/api/tipos-ingresso/{id:int}", async (int id, HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth, "adm");
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);

    var tipo = await connection.QueryFirstOrDefaultAsync(@"
        SELECT t.Id, t.Ativo, e.CriadoPorCpf
        FROM TiposIngresso t
        INNER JOIN Eventos e ON e.Id = t.EventoId
        WHERE t.Id = @Id", new { Id = id });

    if (tipo is null)
        return Results.NotFound("Tipo de ingresso nao encontrado.");

    if (!string.Equals((string)tipo.CriadoPorCpf, auth!.Cpf, StringComparison.Ordinal))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    if (!(bool)tipo.Ativo)
        return Results.BadRequest("Tipo de ingresso ja esta inativo.");

    await connection.ExecuteAsync("UPDATE TiposIngresso SET Ativo = FALSE WHERE Id = @Id", new { Id = id });

    return Results.Ok(new { mensagem = "Tipo de ingresso desativado com sucesso." });
})
.WithName("DesativarTipoIngresso")
.WithDescription("Desativa tipo de ingresso do administrador autenticado")
.Produces(200).Produces(400).Produces(401).Produces(403).Produces(404);

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

    if (string.IsNullOrWhiteSpace(request.Codigo))
        return Results.BadRequest("Código do cupom é obrigatório");
    if (request.PorcentagemDesconto < 0 || request.PorcentagemDesconto > 100)
        return Results.BadRequest("Desconto deve estar entre 0% e 100%");
    if (request.ValorMinimoRegra < 0)
        return Results.BadRequest("Valor mínimo não pode ser negativo");

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
    string? usuarioCpf = request.UsuarioCpf;

    if (string.IsNullOrWhiteSpace(usuarioCpf))
    {
        var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
        if (authError is not null) return authError;
        usuarioCpf = auth!.Cpf;
    }

    if (string.IsNullOrWhiteSpace(usuarioCpf))
        return Results.BadRequest("UsuarioCpf e obrigatorio.");

    var cpfNormalizado = NormalizeCpf(usuarioCpf);

    await using var connection = new NpgsqlConnection(connectionString);

    // [Regra R1] Validação de Integridade: UsuarioCpf ou o EventoId não existirem
    var usuarioExiste = await connection.ExecuteScalarAsync<bool>(
        "SELECT EXISTS(SELECT 1 FROM Usuarios WHERE Cpf = @Cpf)",
        new { Cpf = cpfNormalizado });
    
    if (!usuarioExiste)
        return Results.BadRequest("UsuarioCpf nao existe.");

    var evento = await connection.QueryFirstOrDefaultAsync<Evento>(
        "SELECT Id, Nome, CapacidadeTotal, PrecoPadrao, Categoria, Ativo FROM Eventos WHERE Id = @Id",
        new { Id = request.EventoId });

    if (evento is null || !evento.Ativo)
        return Results.BadRequest("EventoId nao existe.");

    TipoIngresso? tipoIngresso = null;
    if (request.TipoIngressoId.HasValue)
    {
        tipoIngresso = await connection.QueryFirstOrDefaultAsync<TipoIngresso>(@"
            SELECT Id, EventoId, Nome, Preco, EstoqueTotal, Ativo
            FROM TiposIngresso
            WHERE Id = @Id AND EventoId = @EventoId",
            new { Id = request.TipoIngressoId.Value, EventoId = request.EventoId });

        if (tipoIngresso is null || !tipoIngresso.Ativo)
            return Results.BadRequest("TipoIngressoId invalido para este evento.");
    }

    // [Regra R2] Limite por CPF: O mesmo CPF não pode ter mais de 2 reservas para o mesmo EventoId
    var reservasCpf = await connection.QueryFirstAsync<int>(
        "SELECT COUNT(1) FROM Reservas WHERE EventoId = @EventoId AND UsuarioCpf = @Cpf AND Status != 'cancelada'",
        new { EventoId = request.EventoId, Cpf = cpfNormalizado });

    if (reservasCpf >= 2)
        return Results.BadRequest("O mesmo CPF nao pode ter mais de 2 reservas para o mesmo EventoId.");

    // [Regra R3] Controle de Estoque: O número de reservas já existentes for igual à CapacidadeTotal, compra bloqueada
    var reservados = await connection.QueryFirstAsync<int>(
        "SELECT COUNT(1) FROM Reservas WHERE EventoId = @EventoId AND Status != 'cancelada'",
        new { EventoId = request.EventoId });

    if (reservados >= evento.CapacidadeTotal)
        return Results.BadRequest("Evento sem vagas disponiveis.");

    if (tipoIngresso is not null)
    {
        var vendidosTipo = await connection.QueryFirstAsync<int>(
            "SELECT COUNT(1) FROM Reservas WHERE EventoId = @EventoId AND TipoIngressoId = @TipoIngressoId AND Status != 'cancelada'",
            new { EventoId = request.EventoId, TipoIngressoId = tipoIngresso.Id });

        if (vendidosTipo >= tipoIngresso.EstoqueTotal)
            return Results.BadRequest("Tipo de ingresso sem vagas disponiveis.");
    }

    var precoBase = tipoIngresso?.Preco ?? evento.PrecoPadrao;

    // [Regra R4] Motor de Cupons: Buscar o cupom no banco. O desconto só é aplicado sobre o preço base se ele for maior ou igual ao ValorMinimoRegra do cupom.
    decimal precoFinal = precoBase;
    string? codigoCupomAplicado = null;

    if (!string.IsNullOrWhiteSpace(request.CodigoCupom))
    {
        var cupom = await connection.QueryFirstOrDefaultAsync<Cupom>(
            "SELECT Codigo, PorcentagemDesconto, ValorMinimoRegra FROM Cupons WHERE Codigo = @Codigo",
            new { Codigo = request.CodigoCupom.Trim().ToUpperInvariant() });

        if (cupom is null)
            return Results.BadRequest("Cupom invalido.");

        if (precoBase < cupom.ValorMinimoRegra)
            return Results.BadRequest($"Preco minimo para este cupom e R$ {cupom.ValorMinimoRegra:F2}.");

        var desconto = precoBase * (cupom.PorcentagemDesconto / 100m);
        precoFinal = precoBase - desconto;
        codigoCupomAplicado = cupom.Codigo;
    }

    var categoriaEvento = NormalizeEventCategory(evento.Categoria) ?? "musicais";
    var assentoObrigatorio = categoriaEvento is "musicais" or "cinema";
    if (assentoObrigatorio && string.IsNullOrWhiteSpace(request.Assento))
        return Results.BadRequest("Assento e obrigatorio para categorias musicais e cinema.");

    // Assento check
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

    // Cria a reserva
    var reservaId = await connection.QueryFirstAsync<int>(@"
        INSERT INTO Reservas (EventoId, UsuarioCpf, PrecoFinal, CupomCodigo, Status, CriadoEm, Assento, TipoIngressoId)
        VALUES (@EventoId, @UsuarioCpf, @PrecoFinal, @CupomCodigo, 'confirmada', NOW(), @Assento, @TipoIngressoId)
        RETURNING Id",
        new
        {
            EventoId = request.EventoId,
            UsuarioCpf = cpfNormalizado,
            PrecoFinal = precoFinal,
            CupomCodigo = codigoCupomAplicado,
            Assento = string.IsNullOrWhiteSpace(request.Assento) ? null : request.Assento.Trim(),
            TipoIngressoId = request.TipoIngressoId
        });

    return Results.Created($"/api/reservas/{reservaId}", new
    {
        Id = reservaId,
        EventoId = request.EventoId,
        NomeEvento = evento.Nome,
        TipoIngressoId = request.TipoIngressoId,
        TipoIngressoNome = tipoIngresso?.Nome,
        PrecoOriginal = precoBase,
        PrecoFinal = precoFinal,
        CupomAplicado = codigoCupomAplicado,
        Status = "confirmada"
    });
})
.WithName("CriarReserva")
.WithDescription("Reserva ingresso para um evento com validacoes complexas")
.Produces(201).Produces(400).Produces(401);

app.MapGet("/api/reservas/{cpf}", async (string cpf) =>
{
    var cpfNormalizado = NormalizeCpf(cpf);
    await using var connection = new NpgsqlConnection(connectionString);
    var reservas = await connection.QueryAsync(@"
        SELECT r.Id, r.EventoId, e.Nome AS NomeEvento, e.DataEvento,
             r.PrecoFinal, r.CupomCodigo, r.Status, r.CriadoEm, r.Assento,
             r.TipoIngressoId, ti.Nome AS TipoIngressoNome
        FROM Reservas r
        INNER JOIN Eventos e ON e.Id = r.EventoId
         LEFT JOIN TiposIngresso ti ON ti.Id = r.TipoIngressoId
        WHERE r.UsuarioCpf = @Cpf
        ORDER BY r.CriadoEm DESC",
        new { Cpf = cpfNormalizado });

    return Results.Ok(reservas);
})
.WithName("ReservasPorCpf")
.WithDescription("Traz as reservas de um cliente usando CPF com INNER JOIN")
.Produces(200);

app.MapGet("/api/reservas", async (HttpContext httpContext) =>
{
    var authError = TryAuthenticate(httpContext, tokenSecret, out var auth);
    if (authError is not null) return authError;

    await using var connection = new NpgsqlConnection(connectionString);
    var reservas = await connection.QueryAsync(@"
        SELECT r.Id, r.EventoId, e.Nome AS NomeEvento, e.DataEvento,
             r.PrecoFinal, r.CupomCodigo, r.Status, r.CriadoEm,
             r.TipoIngressoId, ti.Nome AS TipoIngressoNome
        FROM Reservas r
        INNER JOIN Eventos e ON e.Id = r.EventoId
         LEFT JOIN TiposIngresso ti ON ti.Id = r.TipoIngressoId
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

app.MapPost("/api/usuarios", async (CriarUsuarioRequest request) =>
{
    var tipoConta = NormalizeAccountType(request.TipoConta);
    if (tipoConta is null)
        return Results.BadRequest("TipoConta invalido. Use 'usuario' ou 'adm'.");

    return await CreateAccountAsync(connectionString, new AccountCreateData(
        request.Cpf, request.Nome, request.Email, request.Senha, tipoConta));
})
.AllowAnonymous()
.WithName("CriarUsuario")
.WithDescription("Cria usuario ou administrador")
.Produces(201).Produces(400);

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

// ✅ PORTA CORRIGIDA - Railway define PORT em runtime
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Logger.LogInformation("🚀 App será executada na porta: {Port}", port);
app.Run($"http://0.0.0.0:{port}");

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

static string? NormalizeEventCategory(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return null;
    var normalized = value.Trim().ToLowerInvariant();
    return normalized is "musicais" or "cinema" or "eventos-diversos" or "viagens"
        ? normalized
        : null;
}

static string? ResolveConnectionString(IConfiguration configuration)
{
    var configuredConnectionString = configuration.GetConnectionString("DefaultConnection")
        ?? configuration.GetConnectionString("NeonDB");

    if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        return configuredConnectionString;

    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? configuration["DATABASE_URL"];

    if (!string.IsNullOrWhiteSpace(databaseUrl))
        return BuildNpgsqlConnectionStringFromDatabaseUrl(databaseUrl);

    return null;
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
            CriadoPorCpf VARCHAR(11) NOT NULL REFERENCES Usuarios(Cpf),
            Categoria TEXT NOT NULL DEFAULT 'musicais',
            Ativo BOOLEAN NOT NULL DEFAULT TRUE
        );
        ALTER TABLE Eventos ADD COLUMN IF NOT EXISTS ImagemUrl TEXT;
        ALTER TABLE Eventos ADD COLUMN IF NOT EXISTS Categoria TEXT;
        ALTER TABLE Eventos ADD COLUMN IF NOT EXISTS Ativo BOOLEAN;
        UPDATE Eventos SET Categoria = 'musicais' WHERE Categoria IS NULL OR btrim(Categoria) = '';
        UPDATE Eventos SET Ativo = TRUE WHERE Ativo IS NULL;
        ALTER TABLE Eventos ALTER COLUMN Categoria SET DEFAULT 'musicais';
        ALTER TABLE Eventos ALTER COLUMN Categoria SET NOT NULL;
        ALTER TABLE Eventos ALTER COLUMN Ativo SET DEFAULT TRUE;
        ALTER TABLE Eventos ALTER COLUMN Ativo SET NOT NULL;
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'ck_eventos_categoria'
            ) THEN
                ALTER TABLE Eventos
                ADD CONSTRAINT ck_eventos_categoria
                CHECK (Categoria IN ('musicais', 'cinema', 'eventos-diversos', 'viagens'));
            END IF;
        END $$;
        CREATE INDEX IF NOT EXISTS IX_Eventos_Categoria_DataEvento ON Eventos (Categoria, DataEvento DESC);
        CREATE TABLE IF NOT EXISTS Cupons (
            Codigo TEXT PRIMARY KEY,
            PorcentagemDesconto NUMERIC(5,2) NOT NULL,
            ValorMinimoRegra NUMERIC(10,2) NOT NULL,
            CriadoPorCpf VARCHAR(11) REFERENCES Usuarios(Cpf)
        );
        CREATE TABLE IF NOT EXISTS TiposIngresso (
            Id SERIAL PRIMARY KEY,
            EventoId INTEGER NOT NULL REFERENCES Eventos(Id) ON DELETE CASCADE,
            Nome TEXT NOT NULL,
            Preco NUMERIC(10,2) NOT NULL,
            EstoqueTotal INTEGER NOT NULL,
            Ativo BOOLEAN NOT NULL DEFAULT TRUE,
            CONSTRAINT ck_tipos_ingresso_preco_nonnegative CHECK (Preco >= 0),
            CONSTRAINT ck_tipos_ingresso_estoque_positive CHECK (EstoqueTotal > 0)
        );
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'uq_tipos_ingresso_evento_nome'
            ) THEN
                ALTER TABLE TiposIngresso
                ADD CONSTRAINT uq_tipos_ingresso_evento_nome UNIQUE (EventoId, Nome);
            END IF;
        END $$;
        CREATE INDEX IF NOT EXISTS IX_TiposIngresso_EventoId_Ativo ON TiposIngresso (EventoId, Ativo);
        CREATE TABLE IF NOT EXISTS EventosViagem (
            EventoId INTEGER PRIMARY KEY REFERENCES Eventos(Id) ON DELETE CASCADE,
            Origem TEXT NOT NULL,
            Destino TEXT NOT NULL,
            Companhia TEXT NOT NULL,
            PartidaEm TIMESTAMPTZ NOT NULL,
            ChegadaEm TIMESTAMPTZ NOT NULL,
            BagagemIncluida BOOLEAN NOT NULL DEFAULT FALSE,
            Observacoes TEXT,
            CONSTRAINT ck_eventos_viagem_periodo CHECK (ChegadaEm > PartidaEm)
        );
        CREATE TABLE IF NOT EXISTS Reservas (
            Id SERIAL PRIMARY KEY,
            EventoId INTEGER NOT NULL REFERENCES Eventos(Id),
            UsuarioCpf VARCHAR(11) NOT NULL REFERENCES Usuarios(Cpf),
            PrecoFinal NUMERIC(10,2) NOT NULL,
            CupomCodigo TEXT,
            Status TEXT NOT NULL DEFAULT 'confirmada',
            CriadoEm TIMESTAMPTZ NOT NULL,
            Assento TEXT,
            TipoIngressoId INTEGER
        );
        ALTER TABLE Reservas ADD COLUMN IF NOT EXISTS Assento TEXT;
        ALTER TABLE Reservas ADD COLUMN IF NOT EXISTS TipoIngressoId INTEGER;
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'fk_reservas_tipoingresso'
            ) THEN
                ALTER TABLE Reservas
                ADD CONSTRAINT fk_reservas_tipoingresso
                FOREIGN KEY (TipoIngressoId) REFERENCES TiposIngresso(Id);
            END IF;
        END $$;
        CREATE INDEX IF NOT EXISTS IX_Reservas_EventoId_TipoIngressoId ON Reservas (EventoId, TipoIngressoId);
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

record CriarEventoRequest(string Nome, int CapacidadeTotal, DateTime DataEvento, decimal PrecoPadrao, string? ImagemUrl, string? Categoria);
record AtualizarEventoRequest(string Nome, int CapacidadeTotal, DateTime DataEvento, decimal PrecoPadrao, string? ImagemUrl, string? Categoria);
record CriarCupomRequest(string Codigo, decimal PorcentagemDesconto, decimal ValorMinimoRegra);
record CriarTipoIngressoRequest(string Nome, decimal Preco, int EstoqueTotal);
record AtualizarTipoIngressoRequest(string Nome, decimal Preco, int EstoqueTotal, bool Ativo);
record AtualizarDetalhesViagemRequest(string Origem, string Destino, string Companhia, DateTime PartidaEm, DateTime ChegadaEm, bool BagagemIncluida, string? Observacoes);
record CriarReservaRequest(int EventoId, string? UsuarioCpf, string? CodigoCupom, string? Assento, int? TipoIngressoId);
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

public class Evento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int CapacidadeTotal { get; set; }
    public decimal PrecoPadrao { get; set; }
    public string? Categoria { get; set; }
    public bool Ativo { get; set; }
}

public class Cupom
{
    public string Codigo { get; set; } = string.Empty;
    public decimal PorcentagemDesconto { get; set; }
    public decimal ValorMinimoRegra { get; set; }
}

public class TipoIngresso
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int EstoqueTotal { get; set; }
    public bool Ativo { get; set; }
}

public class EventoViagem
{
    public int EventoId { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string Companhia { get; set; } = string.Empty;
    public DateTime PartidaEm { get; set; }
    public DateTime ChegadaEm { get; set; }
    public bool BagagemIncluida { get; set; }
    public string? Observacoes { get; set; }
}
