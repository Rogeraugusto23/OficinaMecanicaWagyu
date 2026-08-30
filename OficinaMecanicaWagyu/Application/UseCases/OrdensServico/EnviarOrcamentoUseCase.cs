using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Application.DTOs.OrdensServico;
using OficinaMecanicaWagyu.Domain.Enums;
using OficinaMecanicaWagyu.Domain.Interfaces;

namespace OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

public class EnviarOrcamentoUseCase
{
    private readonly IOrdemServicoRepository _repository;

    public EnviarOrcamentoUseCase(IOrdemServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<OrcamentoEnviadoOutput>> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id);
        if (ordem == null)
            return OperationResult<OrcamentoEnviadoOutput>.Falha(TipoErro.NaoEncontrado, "Ordem de serviço não encontrada.");

        if (ordem.Status != StatusOrdemServico.EmDiagnostico)
            return OperationResult<OrcamentoEnviadoOutput>.Falha(
                TipoErro.OperacaoInvalida,
                $"A OS precisa estar Em Diagnóstico. Status atual: {ordem.Status}.");

        if (!ordem.Servicos.Any() && !ordem.Pecas.Any())
            return OperationResult<OrcamentoEnviadoOutput>.Falha(
                TipoErro.ValidacaoFalhou,
                "Não é possível enviar orçamento sem serviços ou peças.");

        ordem.AvancarStatus(); // EmDiagnostico → AguardandoAprovacao
        await _repository.SalvarAlteracoesAsync();

        var output = new OrcamentoEnviadoOutput
        {
            Mensagem = "Orçamento enviado ao cliente para aprovação.",
            NumeroOS = ordem.NumeroOS,
            ValorTotal = ordem.ValorTotal,
            Status = ordem.Status.ToString(),
            Servicos = ordem.Servicos.Select(s => (object)new { s.Descricao, s.Preco }),
            Pecas = ordem.Pecas.Select(p => (object)new { p.Nome, p.Quantidade, p.PrecoUnitario })
        };

        return OperationResult<OrcamentoEnviadoOutput>.Ok(output);
    }
}
