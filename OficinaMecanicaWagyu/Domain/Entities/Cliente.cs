using OficinaMecanicaWagyu.Domain.Validators;

namespace OficinaMecanicaWagyu.Domain.Entities;

public class Cliente
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Documento { get; private set; }

    // ✅ Construtor para o EF Core
    private Cliente() { }

    public Cliente(string nome, string documento)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.");

        if (!DocumentoValidator.Validar(documento))
            throw new ArgumentException("CPF ou CNPJ inválido.");

        Id = Guid.NewGuid();
        Nome = nome;
        Documento = new string(documento.Where(char.IsDigit).ToArray());
    }

    public void AtualizarNome(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new ArgumentException("Nome é obrigatório.");
        Nome = novoNome;
    }
}