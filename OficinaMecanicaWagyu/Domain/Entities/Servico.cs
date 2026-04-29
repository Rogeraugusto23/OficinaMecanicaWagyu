namespace OficinaMecanicaWagyu.Domain.Entities;

public class Servico
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public int TempoEstimadoMinutos { get; private set; }

    private Servico() { }

    public Servico(string nome, string descricao, decimal preco, int tempoEstimadoMinutos)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.");

        if (preco <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.");

        if (tempoEstimadoMinutos <= 0)
            throw new ArgumentException("Tempo estimado deve ser maior que zero.");

        Id = Guid.NewGuid();
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        TempoEstimadoMinutos = tempoEstimadoMinutos;
    }

    public void Atualizar(string nome, string descricao, decimal preco, int tempoEstimadoMinutos)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.");

        if (preco <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.");

        if (tempoEstimadoMinutos <= 0)
            throw new ArgumentException("Tempo estimado deve ser maior que zero.");

        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        TempoEstimadoMinutos = tempoEstimadoMinutos;
    }
}