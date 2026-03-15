using Dapper;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string connectionString =
    "Server=(localdb)\\MSSQLLocalDB;Database=TicketPrime;Trusted_Connection=True;TrustServerCertificate=True;";

app.MapGet("/", () => "TicketPrime API online");

app.MapGet("/api/eventos", async () =>
{
    using var connection = new SqlConnection(connectionString);

    var eventos = await connection.QueryAsync("SELECT * FROM Eventos");

    return Results.Ok(eventos);
});

app.Run();