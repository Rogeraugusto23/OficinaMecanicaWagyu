using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Domain.Interfaces;
using DomainCliente = OficinaMecanicaWagyu.Domain.Entities.Cliente;

namespace OficinaMecanicaWagyu.Application.UseCases.Clientes;

public class InativarClienteUseCase
{
    private readonly IClienteRepository _repository;

    public InativarClienteUseCase(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<DomainCliente>> ExecutarAsync(Guid id)
    {
        var cliente = await _repository.ObterPorIdAsync(id);
        if (cliente == null)
            return OperationResult<DomainCliente>.Falha(TipoErro.NaoEncontrado, "Cliente não encontrado.");

        cliente.Inativar();
        await _repository.SalvarAlteracoesAsync();

        return OperationResult<DomainCliente>.Ok(cliente, "Cliente inativado.");
    }
}
