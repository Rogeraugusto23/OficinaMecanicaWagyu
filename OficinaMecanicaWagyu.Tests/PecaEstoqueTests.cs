using FluentAssertions;
using OficinaMecanicaWagyu.Domain.Entities;

namespace OficinaMecanicaWagyu.Tests;

public class PecaEstoqueTests
{
    [Fact]
    public void CriarPeca_EstoqueDeveIniciarZerado()
    {
        var peca = new Peca("Filtro de Óleo", "FO-001", 45.00m);

        peca.QuantidadeEstoque.Should().Be(0);
        peca.EstoqueAbaixoDoMinimo.Should().BeTrue();
    }

    [Fact]
    public void EntradaEstoque_DeveAumentarQuantidade()
    {
        var peca = new Peca("Filtro de Óleo", "FO-001", 45.00m);

        peca.EntradaEstoque(10);

        peca.QuantidadeEstoque.Should().Be(10);
        peca.EstoqueAbaixoDoMinimo.Should().BeFalse();
    }

    [Fact]
    public void SaidaEstoque_DeveReduzirQuantidade()
    {
        var peca = new Peca("Filtro de Óleo", "FO-001", 45.00m);
        peca.EntradaEstoque(10);

        peca.SaidaEstoque(3);

        peca.QuantidadeEstoque.Should().Be(7);
    }

    [Fact]
    public void SaidaEstoque_SemEstoqueSuficiente_DeveLancarExcecao()
    {
        var peca = new Peca("Filtro de Óleo", "FO-001", 45.00m);
        peca.EntradaEstoque(2);

        Action acao = () => peca.SaidaEstoque(5);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*Estoque insuficiente*");
    }

    [Fact]
    public void CriarPeca_PrecoZero_DeveLancarExcecao()
    {
        Action acao = () => new Peca("Filtro", "FO-001", 0);

        acao.Should().Throw<ArgumentException>()
            .WithMessage("*Preço deve ser maior que zero*");
    }
}