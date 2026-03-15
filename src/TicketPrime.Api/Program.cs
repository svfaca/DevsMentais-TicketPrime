using Microsoft.Data.SqlClient;
using Dapper;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string connectionString = "Server=localhost;Database=TicketPrime;Trusted_Connection=True;TrustServerCertificate=True;";

app.MapGet("/api/eventos", async () =>
{
    using var connection = new SqlConnection(connectionString);

    var eventos = await connection.QueryAsync("SELECT * FROM Eventos");

    return Results.Ok(eventos);
});

app.Run();