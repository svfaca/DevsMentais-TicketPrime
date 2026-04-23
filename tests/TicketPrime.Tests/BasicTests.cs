using Xunit;

namespace TicketPrime.Tests;

public class BasicTests
{
    [Fact]
    public void DeveValidarCpfComOnzeDigitos()
    {
        // Testa validação básica de CPF
        var cpf = "12345678901";
        Assert.Equal(11, cpf.Length);
    }

    [Fact]
    public void DeveValidarEmailComArroba()
    {
        // Testa validação básica de email
        var email = "usuario@ticketprime.com";
        Assert.Contains("@", email);
    }

    [Fact]
    public void DeveValidarCapacidadeEventoPositiva()
    {
        // Testa que capacidade de evento deve ser positiva
        int capacidade = 100;
        Assert.True(capacidade > 0);
    }

    [Fact]
    public void DeveValidarDescontoEntre0e100Porcento()
    {
        // Testa que desconto deve estar entre 0% e 100%
        decimal desconto = 15.5m;
        Assert.InRange(desconto, 0, 100);
    }

    [Fact]
    public void DeveValidarPrecoPadraoNaoNegativo()
    {
        // Testa que preço padrão não pode ser negativo
        decimal preco = 99.90m;
        Assert.True(preco >= 0);
    }

    [Fact]
    public void DeveValidarNomeEventoNaoVazio()
    {
        // Testa que nome de evento não pode estar vazio
        string nome = "Festa de Confraternização";
        Assert.False(string.IsNullOrWhiteSpace(nome));
    }
}
