using Microsoft.Extensions.Logging;
using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Application.DTOs.OrdensServico;
using OficinaMecanicaWagyu.Domain.Enums;
using OficinaMecanicaWagyu.Domain.Interfaces;

namespace OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

public class AtualizarStatusPorEmailUseCase
{
    private readonly IOrdemServicoRepository _repository;
    private readonly ILogger<AtualizarStatusPorEmailUseCase> _logger;

    public AtualizarStatusPorEmailUseCase(IOrdemServicoRepository repository, ILogger<AtualizarStatusPorEmailUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OperationResult<StatusAtualizadoPorEmailOutput>> ExecutarAsync(AtualizacaoStatusEmailInput input)
    {
        // Todo log deste Use Case usa a propriedade {Integracao}="WebhookEmail",
        // permitindo filtrar especificamente falhas dessa integração externa no
        // dashboard "Erros e falhas nas integrações" (ex: NRQL
        // `WHERE Integracao = 'WebhookEmail' AND Sucesso = false`).
        var ordem = await _repository.ObterPorNumeroOSAsync(input.NumeroOS);
        if (ordem == null)
        {
            _logger.LogWarning(
                "IntegracaoFalhou {Integracao} {NumeroOS} {Motivo} {Sucesso}",
                "WebhookEmail", input.NumeroOS, "OS nao encontrada", false);
            return OperationResult<StatusAtualizadoPorEmailOutput>.Falha(
                TipoErro.NaoEncontrado, $"OS {input.NumeroOS} não encontrada.");
        }

        if (!Enum.TryParse<StatusOrdemServico>(input.NovoStatus, true, out var novoStatus))
        {
            _logger.LogWarning(
                "IntegracaoFalhou {Integracao} {NumeroOS} {Motivo} {Sucesso}",
                "WebhookEmail", input.NumeroOS, $"Status invalido: {input.NovoStatus}", false);
            return OperationResult<StatusAtualizadoPorEmailOutput>.Falha(
                TipoErro.ValidacaoFalhou, $"Status '{input.NovoStatus}' inválido.");
        }

        var statusAnterior = ordem.Status;

        try
        {
            ordem.DefinirStatus(novoStatus);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "IntegracaoFalhou {Integracao} {NumeroOS} {Motivo} {Sucesso}",
                "WebhookEmail", input.NumeroOS, ex.Message, false);
            return OperationResult<StatusAtualizadoPorEmailOutput>.Falha(TipoErro.OperacaoInvalida, ex.Message);
        }

        await _repository.SalvarAlteracoesAsync();

        var tempoDecorridoMinutos = (DateTime.UtcNow - ordem.DataAbertura).TotalMinutes;
        _logger.LogInformation(
            "OrdemServicoStatusAlterado {NumeroOS} {StatusAnterior} {StatusAtual} {TempoDecorridoMinutos}",
            ordem.NumeroOS, statusAnterior, ordem.Status, tempoDecorridoMinutos);
        _logger.LogInformation(
            "IntegracaoConcluida {Integracao} {NumeroOS} {Sucesso}",
            "WebhookEmail", ordem.NumeroOS, true);

        return OperationResult<StatusAtualizadoPorEmailOutput>.Ok(new StatusAtualizadoPorEmailOutput
        {
            Mensagem = "Status atualizado via e-mail com sucesso.",
            NumeroOS = ordem.NumeroOS,
            StatusAnterior = input.NovoStatus,
            StatusAtual = ordem.Status.ToString(),
            OrigemEmail = input.RemetenteEmail
        });
    }
}
