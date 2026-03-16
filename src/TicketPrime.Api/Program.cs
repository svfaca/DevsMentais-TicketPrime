using Dapper;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string connectionString =
"Server=(localdb)\\MSSQLLocalDB;Database=TicketPrime;Trusted_Connection=True;TrustServerCertificate=True;";

app.MapGet("/api/eventos", async () =>
{
    using var connection = new SqlConnection(connectionString);

    var eventos = await connection.QueryAsync("SELECT * FROM Eventos");

    return Results.Ok(eventos);
});

app.MapPost("/api/eventos", async (string nome, int capacidadeTotal, DateTime dataEvento, decimal precoPadrao) =>
{
    using var connection = new SqlConnection(connectionString);

    var sql = @"
        INSERT INTO Eventos (Nome, CapacidadeTotal, DataEvento, PrecoPadrao)
        VALUES (@nome, @capacidadeTotal, @dataEvento, @precoPadrao)";

    await connection.ExecuteAsync(sql, new
    {
        nome,
        capacidadeTotal,
        dataEvento,
        precoPadrao
    });

    return Results.Created();
});

app.MapPost("/api/cupons", async (string codigo, decimal porcentagemDesconto, decimal valorMinimoRegra) =>
{
    using var connection = new SqlConnection(connectionString);

    var sql = @"
        INSERT INTO Cupons (Codigo, PorcentagemDesconto, ValorMinimoRegra)
        VALUES (@codigo, @porcentagemDesconto, @valorMinimoRegra)";

    await connection.ExecuteAsync(sql, new
    {
        codigo,
        porcentagemDesconto,
        valorMinimoRegra
    });

    return Results.Created();
});

app.MapPost("/api/usuarios", async (string cpf, string nome, string email) =>
{
    using var connection = new SqlConnection(connectionString);

    var usuarioExistente = await connection.QueryFirstOrDefaultAsync(
        "SELECT * FROM Usuarios WHERE Cpf = @cpf",
        new { cpf }
    );

    if (usuarioExistente != null)
    {
        return Results.BadRequest("CPF já cadastrado");
    }

    var sql = @"
        INSERT INTO Usuarios (Cpf, Nome, Email)
        VALUES (@cpf, @nome, @email)";

    await connection.ExecuteAsync(sql, new
    {
        cpf,
        nome,
        email
    });

    return Results.Created();
});

app.Run();