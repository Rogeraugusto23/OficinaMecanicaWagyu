using OficinaMecanicaWagyu.Application.Common;
using OficinaMecanicaWagyu.Application.DTOs.OrdensServico;
using OficinaMecanicaWagyu.Domain.Interfaces;
using DomainOrdemServico = OficinaMecanicaWagyu.Domain.Entities.OrdemServico;

namespace OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

public class AbrirOrdemServicoUseCase
{
    private readonly IOrdemServicoRepository _repository;

    public AbrirOrdemServicoUseCase(IOrdemServicoRepository repository)
    {
        _repository = repository;
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

        return OperationResult<DomainOrdemServico>.Ok(novaOrdem, "Ordem de serviço aberta com sucesso.");
    }
}
