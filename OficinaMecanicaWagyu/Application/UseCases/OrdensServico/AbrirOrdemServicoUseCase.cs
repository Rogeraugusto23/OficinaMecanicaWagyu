using Microsoft.Extensions.Logging;
using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Application.DTOs.OrdensServico;
using OficinaMecanicaWagyu.Domain.Interfaces;
using DomainOrdemServico = OficinaMecanicaWagyu.Domain.Entities.OrdemServico;

namespace OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

public class AbrirOrdemServicoUseCase
{
    private readonly IOrdemServicoRepository _repository;
    private readonly ILogger<AbrirOrdemServicoUseCase> _logger;

    public AbrirOrdemServicoUseCase(IOrdemServicoRepository repository, ILogger<AbrirOrdemServicoUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OperationResult<DomainOrdemServico>> ExecutarAsync(AbrirOrdemServicoInput input)
    {
        var novaOrdem = new DomainOrdemServico(input.ClienteId, input.VeiculoId);

        if (input.Servicos != null)
            foreach (var s in input.Servicos)
                novaOrdem.AdicionarServico(s.Descricao, s.Preco);

        if (input.Pecas != null)
            foreach (var p in input.Pecas)
                novaOrdem.AdicionarPeca(p.Nome, p.Quantidade, p.PrecoUnitario);

        await _repository.AdicionarAsync(novaOrdem);
        await _repository.SalvarAlteracoesAsync();

        // Log estruturado de negócio (não confundir com o log de requisição HTTP,
        // que só mostra método/rota/status). As propriedades nomeadas abaixo viram
        // atributos consultáveis no New Relic — alimentam o dashboard "Volume
        // diário de Ordens de Serviço" (contagem de eventos "OrdemServicoAberta"
        // agrupados por dia) e servem de base para o cálculo de "tempo médio de
        // execução por status" (ver os demais Use Cases, que logam a transição
        // de status com o tempo decorrido desde DataAbertura).
        _logger.LogInformation(
            "OrdemServicoAberta {NumeroOS} {ClienteId} {VeiculoId} {ValorTotal} {Status}",
            novaOrdem.NumeroOS, novaOrdem.ClienteId, novaOrdem.VeiculoId, novaOrdem.ValorTotal, novaOrdem.Status);

        return OperationResult<DomainOrdemServico>.Ok(novaOrdem, "Ordem de serviço aberta com sucesso.");
    }
}
