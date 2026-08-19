namespace OficinaMecanicaWagyu.Domain.Entities;

public class Peca
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Codigo { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public int QuantidadeEstoque { get; private set; }
    public int EstoqueMinimo { get; private set; }

    private Peca() { }

    public Peca(string nome, string codigo, decimal precoUnitario, int estoqueMinimo = 5)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("Código é obrigatório.");

        if (precoUnitario <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.");

        Id = Guid.NewGuid();
        Nome = nome;
        Codigo = codigo.Trim().ToUpper();
        PrecoUnitario = precoUnitario;
        EstoqueMinimo = estoqueMinimo;
        QuantidadeEstoque = 0;
    }

    public void EntradaEstoque(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");
        QuantidadeEstoque += quantidade;
    }

    public void SaidaEstoque(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");
        if (quantidade > QuantidadeEstoque)
            throw new InvalidOperationException($"Estoque insuficiente. Disponível: {QuantidadeEstoque}");
        QuantidadeEstoque -= quantidade;
    }

    public void AtualizarPreco(decimal novoPreco)
    {
        if (novoPreco <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.");
        PrecoUnitario = novoPreco;
    }

    public bool EstoqueAbaixoDoMinimo => QuantidadeEstoque < EstoqueMinimo;
}