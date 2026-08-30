using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Domain.Interfaces;
using DomainOrdemServico = OficinaMecanicaWagyu.Domain.Entities.OrdemServico;

namespace OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

public class AvancarStatusUseCase
{
    private readonly IOrdemServicoRepository _repository;

    public AvancarStatusUseCase(IOrdemServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<DomainOrdemServico>> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id);
        if (ordem == null)
            return OperationResult<DomainOrdemServico>.Falha(TipoErro.NaoEncontrado, "Ordem de serviço não encontrada.");

        try
        {
            ordem.AvancarStatus();
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<DomainOrdemServico>.Falha(TipoErro.OperacaoInvalida, ex.Message);
        }

        await _repository.SalvarAlteracoesAsync();
        return OperationResult<DomainOrdemServico>.Ok(ordem);
    }
}
