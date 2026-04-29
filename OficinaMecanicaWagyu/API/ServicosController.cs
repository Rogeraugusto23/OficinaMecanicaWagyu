using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaWagyu.Domain.Entities;
using OficinaMecanicaWagyu.Infrastructure.Data;

namespace OficinaMecanicaWagyu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicosController : ControllerBase
{
    private readonly OficinaDbContext _context;

    public ServicosController(OficinaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Servico>>> GetServicos()
    {
        return await _context.Servicos.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Servico>> GetServico(Guid id)
    {
        var servico = await _context.Servicos.FindAsync(id);
        if (servico == null) return NotFound();
        return servico;
    }

    [HttpPost]
    public async Task<ActionResult<Servico>> PostServico(ServicoInputModel input)
    {
        try
        {
            var servico = new Servico(input.Nome, input.Descricao, input.Preco, input.TempoEstimadoMinutos);
            _context.Servicos.Add(servico);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetServico), new { id = servico.Id }, servico);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutServico(Guid id, ServicoInputModel input)
    {
        var servico = await _context.Servicos.FindAsync(id);
        if (servico == null) return NotFound();

        try
        {
            servico.Atualizar(input.Nome, input.Descricao, input.Preco, input.TempoEstimadoMinutos);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServico(Guid id)
    {
        var servico = await _context.Servicos.FindAsync(id);
        if (servico == null) return NotFound();

        _context.Servicos.Remove(servico);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public class ServicoInputModel
    {
        public string Nome { get; set; } = "";
        public string Descricao { get; set; } = "";
        public decimal Preco { get; set; }
        public int TempoEstimadoMinutos { get; set; }
    }
}