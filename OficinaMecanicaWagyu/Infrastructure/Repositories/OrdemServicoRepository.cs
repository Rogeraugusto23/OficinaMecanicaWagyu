using Microsoft.EntityFrameworkCore;
using OficinaMecanicaWagyu.Domain.Entities;
using OficinaMecanicaWagyu.Domain.Interfaces;
using OficinaMecanicaWagyu.Infrastructure.Data;

namespace OficinaMecanicaWagyu.Infrastructure.Repositories;

public class OrdemServicoRepository : IOrdemServicoRepository
{
    private readonly OficinaDbContext _context;

    public OrdemServicoRepository(OficinaDbContext context)
    {
        _context = context;
    }

    public async Task<OrdemServico?> ObterPorIdAsync(Guid id)
    {
        return await _context.OrdensServico
            .Include(o => o.Servicos)
            .Include(o => o.Pecas)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<OrdemServico?> ObterPorNumeroOSAsync(string numeroOS)
    {
        return await _context.OrdensServico
            .FirstOrDefaultAsync(o => o.NumeroOS == numeroOS);
    }

    public async Task<IEnumerable<OrdemServico>> ListarAsync()
    {
        return await _context.OrdensServico
            .Include(o => o.Servicos)
            .Include(o => o.Pecas)
            .ToListAsync();
    }

    public async Task AdicionarAsync(OrdemServico ordemServico)
    {
        await _context.OrdensServico.AddAsync(ordemServico);
    }

    public async Task SalvarAlteracoesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
