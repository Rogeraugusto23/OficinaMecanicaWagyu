using Microsoft.Extensions.Logging;
using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Application.DTOs.OrdensServico;
using OficinaMecanicaWagyu.Domain.Enums;
using OficinaMecanicaWagyu.Domain.Interfaces;

namespace OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

public class AprovarOrcamentoUseCase
{
    private readonly IOrdemServicoRepository _repository;
    private readonly ILogger<AprovarOrcamentoUseCase> _logger;

    public AprovarOrcamentoUseCase(IOrdemServicoRepository repository, ILogger<AprovarOrcamentoUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OperationResult<StatusAtualizadoOutput>> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id);
        if (ordem == null)
            return OperationResult<StatusAtualizadoOutput>.Falha(TipoErro.NaoEncontrado, "Ordem de serviço não encontrada.");

        if (ordem.Status != StatusOrdemServico.AguardandoAprovacao)
        {
            _logger.LogWarning(
                "OrdemServicoTransicaoFalhou {NumeroOS} {StatusAnterior} {Motivo}",
                ordem.NumeroOS, ordem.Status, "Esperava AguardandoAprovacao");
            return OperationResult<StatusAtualizadoOutput>.Falha(
                TipoErro.OperacaoInvalida,
                $"A OS precisa estar Aguardando Aprovação. Status atual: {ordem.Status}.");
        }

        var statusAnterior = ordem.Status;
        ordem.AvancarStatus(); // AguardandoAprovacao → EmExecucao
        await _repository.SalvarAlteracoesAsync();

        var tempoDecorridoMinutos = (DateTime.UtcNow - ordem.DataAbertura).TotalMinutes;
        _logger.LogInformation(
            "OrdemServicoStatusAlterado {NumeroOS} {StatusAnterior} {StatusAtual} {TempoDecorridoMinutos}",
            ordem.NumeroOS, statusAnterior, ordem.Status, tempoDecorridoMinutos);

        return OperationResult<StatusAtualizadoOutput>.Ok(new StatusAtualizadoOutput
        {
            Mensagem = "Orçamento aprovado! OS iniciada.",
            NumeroOS = ordem.NumeroOS,
            Status = ordem.Status.ToString()
        });
    }
}
