using Microsoft.Extensions.Logging;
using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Domain.Interfaces;
using DomainOrdemServico = OficinaMecanicaWagyu.Domain.Entities.OrdemServico;

namespace OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

public class AvancarStatusUseCase
{
    private readonly IOrdemServicoRepository _repository;
    private readonly ILogger<AvancarStatusUseCase> _logger;

    public AvancarStatusUseCase(IOrdemServicoRepository repository, ILogger<AvancarStatusUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OperationResult<DomainOrdemServico>> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id);
        if (ordem == null)
            return OperationResult<DomainOrdemServico>.Falha(TipoErro.NaoEncontrado, "Ordem de serviço não encontrada.");

        var statusAnterior = ordem.Status;

        try
        {
            ordem.AvancarStatus();
        }
        catch (InvalidOperationException ex)
        {
            // Log de erro estruturado — alimenta o dashboard "Erros e falhas nas
            // integrações"/processamento de OS.
            _logger.LogWarning(
                "OrdemServicoTransicaoFalhou {NumeroOS} {StatusAnterior} {Motivo}",
                ordem.NumeroOS, statusAnterior, ex.Message);
            return OperationResult<DomainOrdemServico>.Falha(TipoErro.OperacaoInvalida, ex.Message);
        }

        await _repository.SalvarAlteracoesAsync();

        // Tempo decorrido desde a abertura até esta transição — usado para
        // calcular o "tempo médio de execução por status" no dashboard.
        var tempoDecorridoMinutos = (DateTime.UtcNow - ordem.DataAbertura).TotalMinutes;
        _logger.LogInformation(
            "OrdemServicoStatusAlterado {NumeroOS} {StatusAnterior} {StatusAtual} {TempoDecorridoMinutos}",
            ordem.NumeroOS, statusAnterior, ordem.Status, tempoDecorridoMinutos);

        return OperationResult<DomainOrdemServico>.Ok(ordem);
    }
}
