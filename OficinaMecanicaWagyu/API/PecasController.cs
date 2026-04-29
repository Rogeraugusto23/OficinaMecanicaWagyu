using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaWagyu.Domain.Entities;
using OficinaMecanicaWagyu.Infrastructure.Data;

namespace OficinaMecanicaWagyu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PecasController : ControllerBase
{
    private readonly OficinaDbContext _context;

    public PecasController(OficinaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Peca>>> GetPecas()
    {
        return await _context.PecasCatalogo.ToListAsync();
    }

    // GET: api/Pecas/estoque-baixo
    [HttpGet("estoque-baixo")]
    public async Task<ActionResult<IEnumerable<Peca>>> GetEstoqueBaixo()
    {
        return await _context.PecasCatalogo
            .Where(p => p.QuantidadeEstoque < p.EstoqueMinimo)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Peca>> GetPeca(Guid id)
    {
        var peca = await _context.PecasCatalogo.FindAsync(id);
        if (peca == null) return NotFound();
        return peca;
    }

    [HttpPost]
    public async Task<ActionResult<Peca>> PostPeca(PecaInputModel input)
    {
        try
        {
            var peca = new Peca(input.Nome, input.Codigo, input.PrecoUnitario, input.EstoqueMinimo);
            _context.PecasCatalogo.Add(peca);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPeca), new { id = peca.Id }, peca);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException)
        {
            return Conflict("Já existe uma peça com este código.");
        }
    }

    // PUT: api/Pecas/{id}/preco
    [HttpPut("{id}/preco")]
    public async Task<IActionResult> AtualizarPreco(Guid id, [FromBody] decimal novoPreco)
    {
        var peca = await _context.PecasCatalogo.FindAsync(id);
        if (peca == null) return NotFound();

        try
        {
            peca.AtualizarPreco(novoPreco);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST: api/Pecas/{id}/entrada
    [HttpPost("{id}/entrada")]
    public async Task<IActionResult> EntradaEstoque(Guid id, [FromBody] int quantidade)
    {
        var peca = await _context.PecasCatalogo.FindAsync(id);
        if (peca == null) return NotFound();

        try
        {
            peca.EntradaEstoque(quantidade);
            await _context.SaveChangesAsync();
            return Ok(new { mensagem = $"Estoque atualizado. Novo total: {peca.QuantidadeEstoque}" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST: api/Pecas/{id}/saida
    [HttpPost("{id}/saida")]
    public async Task<IActionResult> SaidaEstoque(Guid id, [FromBody] int quantidade)
    {
        var peca = await _context.PecasCatalogo.FindAsync(id);
        if (peca == null) return NotFound();

        try
        {
            peca.SaidaEstoque(quantidade);
            await _context.SaveChangesAsync();
            return Ok(new { mensagem = $"Saída registrada. Estoque restante: {peca.QuantidadeEstoque}" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePeca(Guid id)
    {
        var peca = await _context.PecasCatalogo.FindAsync(id);
        if (peca == null) return NotFound();

        _context.PecasCatalogo.Remove(peca);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public class PecaInputModel
    {
        public string Nome { get; set; } = "";
        public string Codigo { get; set; } = "";
        public decimal PrecoUnitario { get; set; }
        public int EstoqueMinimo { get; set; } = 5;
    }
}