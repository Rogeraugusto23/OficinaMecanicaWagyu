using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Application.DTOs.OrdensServico;
using OficinaMecanicaWagyu.Domain.Enums;
using OficinaMecanicaWagyu.Domain.Interfaces;

namespace OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

public class RejeitarOrcamentoUseCase
{
    private readonly IOrdemServicoRepository _repository;

    public RejeitarOrcamentoUseCase(IOrdemServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<StatusAtualizadoOutput>> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id);
        if (ordem == null)
            return OperationResult<StatusAtualizadoOutput>.Falha(TipoErro.NaoEncontrado, "Ordem de serviço não encontrada.");

        if (ordem.Status != StatusOrdemServico.AguardandoAprovacao)
            return OperationResult<StatusAtualizadoOutput>.Falha(
                TipoErro.OperacaoInvalida,
                $"A OS precisa estar Aguardando Aprovação. Status atual: {ordem.Status}.");

        ordem.CancelarOS();
        await _repository.SalvarAlteracoesAsync();

        return OperationResult<StatusAtualizadoOutput>.Ok(new StatusAtualizadoOutput
        {
            Mensagem = "Orçamento rejeitado pelo cliente. OS cancelada.",
            NumeroOS = ordem.NumeroOS,
            Status = ordem.Status.ToString()
        });
    }
}
