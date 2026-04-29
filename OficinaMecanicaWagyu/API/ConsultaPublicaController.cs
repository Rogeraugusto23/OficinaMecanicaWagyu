using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaWagyu.Domain.Enums;
using OficinaMecanicaWagyu.Infrastructure.Data;

namespace OficinaMecanicaWagyu.API.Controllers;

[ApiController]
[Route("api/consulta")]
public class ConsultaPublicaController : ControllerBase
{
    private readonly OficinaDbContext _context;

    public ConsultaPublicaController(OficinaDbContext context)
    {
        _context = context;
    }

    // GET: api/consulta/{numeroOS}
    // ✅ Sem [Authorize] — acesso público para o cliente acompanhar a OS
    [HttpGet("{numeroOS}")]
    public async Task<IActionResult> ConsultarOS(string numeroOS)
    {
        var os = await _context.OrdensServico
            .Include(o => o.Servicos)
            .Include(o => o.Pecas)
            .FirstOrDefaultAsync(o => o.NumeroOS == numeroOS);

        if (os == null)
            return NotFound("Ordem de serviço não encontrada.");

        // ✅ Retorna apenas o necessário — sem expor dados internos
        return Ok(new
        {
            numeroOS = os.NumeroOS,
            dataAbertura = os.DataAbertura,
            status = os.Status.ToString(),
            servicos = os.Servicos.Select(s => new
            {
                descricao = s.Descricao,
                preco = s.Preco
            }),
            pecas = os.Pecas.Select(p => new
            {
                nome = p.Nome,
                quantidade = p.Quantidade,
                precoUnitario = p.PrecoUnitario
            }),
            valorTotal = os.ValorTotal
        });
    }
}