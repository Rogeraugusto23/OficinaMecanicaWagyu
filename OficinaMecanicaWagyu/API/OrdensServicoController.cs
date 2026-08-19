using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OficinaMecanicaWagyu.Domain.Entities;
using OficinaMecanicaWagyu.Domain.Enums;
using OficinaMecanicaWagyu.Infrastructure.Data;

namespace OficinaMecanicaWagyu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdensServicoController : ControllerBase
    {
        private readonly OficinaDbContext _context;
        private readonly IConfiguration _config;

        public OrdensServicoController(OficinaDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/OrdensServico
        // Regra de negócio (Fase 2):
        // - Não lista OS Finalizada/Entregue (exclusão lógica, não física — continuam no banco)
        // - Ordena por prioridade de status: EmExecucao > AguardandoAprovacao > EmDiagnostico > Recebida
        // - Dentro do mesmo status, mais antigas primeiro
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrdemServico>>> GetOrdensServico()
        {
            var statusExcluidos = new[] { StatusOrdemServico.Finalizada, StatusOrdemServico.Entregue };

            var ordens = await _context.OrdensServico
                .Include(o => o.Servicos)
                .Include(o => o.Pecas)
                .Where(o => !statusExcluidos.Contains(o.Status))
                .ToListAsync();

            var prioridade = new Dictionary<StatusOrdemServico, int>
            {
                { StatusOrdemServico.EmExecucao, 1 },
                { StatusOrdemServico.AguardandoAprovacao, 2 },
                { StatusOrdemServico.EmDiagnostico, 3 },
                { StatusOrdemServico.Recebida, 4 },
                { StatusOrdemServico.Cancelada, 5 }
            };

            var ordenadas = ordens
                .OrderBy(o => prioridade.GetValueOrDefault(o.Status, 99))
                .ThenBy(o => o.DataAbertura)
                .ToList();

            return ordenadas;
        }

        [HttpPost]
        public async Task<ActionResult<OrdemServico>> PostOrdemServico(OrdemServicoInputModel input)
        {
            var novaOrdem = new OrdemServico(input.ClienteId, input.VeiculoId);

            if (input.Servicos != null)
                foreach (var s in input.Servicos)
                    novaOrdem.AdicionarServico(s.Descricao, s.Preco);

            if (input.Pecas != null)
                foreach (var p in input.Pecas)
                    novaOrdem.AdicionarPeca(p.Nome, p.Quantidade, p.PrecoUnitario);

            _context.OrdensServico.Add(novaOrdem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrdemServico), new { id = novaOrdem.Id }, novaOrdem);
        }

        // PUT: api/OrdensServico/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id)
        {
            var ordem = await _context.OrdensServico.FindAsync(id);
            if (ordem == null) return NotFound();

            // ✅ Usa o método da entidade em vez de setar direto (setter é privado)
            ordem.AvancarStatus();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrdemServico>> GetOrdemServico(Guid id)
        {
            var ordemServico = await _context.OrdensServico
                .Include(o => o.Servicos)
                .Include(o => o.Pecas)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ordemServico == null) return NotFound();

            return ordemServico;
        }

        // POST: api/OrdensServico/{id}/enviar-orcamento
        [HttpPost("{id}/enviar-orcamento")]
        public async Task<IActionResult> EnviarOrcamento(Guid id)
        {
            var ordem = await _context.OrdensServico
                .Include(o => o.Servicos)
                .Include(o => o.Pecas)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ordem == null) return NotFound();

            if (ordem.Status != StatusOrdemServico.EmDiagnostico)
                return BadRequest($"A OS precisa estar Em Diagnóstico. Status atual: {ordem.Status}.");

            if (!ordem.Servicos.Any() && !ordem.Pecas.Any())
                return BadRequest("Não é possível enviar orçamento sem serviços ou peças.");

            ordem.AvancarStatus(); // EmDiagnostico → AguardandoAprovacao
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Orçamento enviado ao cliente para aprovação.",
                numeroOS = ordem.NumeroOS,
                valorTotal = ordem.ValorTotal,
                status = ordem.Status.ToString(),
                servicos = ordem.Servicos.Select(s => new { s.Descricao, s.Preco }),
                pecas = ordem.Pecas.Select(p => new { p.Nome, p.Quantidade, p.PrecoUnitario })
            });
        }

        // POST: api/OrdensServico/{id}/aprovar-orcamento
        [HttpPost("{id}/aprovar-orcamento")]
        public async Task<IActionResult> AprovarOrcamento(Guid id)
        {
            var ordem = await _context.OrdensServico.FindAsync(id);
            if (ordem == null) return NotFound();

            if (ordem.Status != StatusOrdemServico.AguardandoAprovacao)
                return BadRequest($"A OS precisa estar Aguardando Aprovação. Status atual: {ordem.Status}.");

            ordem.AvancarStatus(); // AguardandoAprovacao → EmExecucao
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Orçamento aprovado! OS iniciada.",
                numeroOS = ordem.NumeroOS,
                status = ordem.Status.ToString()
            });
        }

        // POST: api/OrdensServico/{id}/rejeitar-orcamento
        [HttpPost("{id}/rejeitar-orcamento")]
        public async Task<IActionResult> RejeitarOrcamento(Guid id)
        {
            var ordem = await _context.OrdensServico.FindAsync(id);
            if (ordem == null) return NotFound();

            if (ordem.Status != StatusOrdemServico.AguardandoAprovacao)
                return BadRequest($"A OS precisa estar Aguardando Aprovação. Status atual: {ordem.Status}.");

            ordem.CancelarOS(); // ✅ novo método que vamos adicionar na entidade
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Orçamento rejeitado pelo cliente. OS cancelada.",
                numeroOS = ordem.NumeroOS,
                status = ordem.Status.ToString()
            });
        }

        // POST: api/OrdensServico/webhook/email-status
        // Webhook de atualização de status via e-mail.
        // Integra com um serviço de e-mail que faz o parsing (ex: SendGrid Inbound Parse,
        // Power Automate, Zapier) e envia aqui o número da OS + o status informado no e-mail.
        // Não exige [Authorize] de usuário, mas é protegido por uma chave secreta simples
        // (X-Webhook-Secret), validada contra configuração (appsettings / Secret do K8s).
        [HttpPost("webhook/email-status")]
        [AllowAnonymous]
        public async Task<IActionResult> AtualizarStatusPorEmail(
            [FromBody] AtualizacaoStatusEmailInput input,
            [FromHeader(Name = "X-Webhook-Secret")] string? webhookSecret)
        {
            var secretEsperado = _config["EmailWebhook:Secret"];
            if (string.IsNullOrEmpty(secretEsperado) || webhookSecret != secretEsperado)
                return Unauthorized("Webhook secret inválido.");

            var ordem = await _context.OrdensServico
                .FirstOrDefaultAsync(o => o.NumeroOS == input.NumeroOS);

            if (ordem == null) return NotFound($"OS {input.NumeroOS} não encontrada.");

            if (!Enum.TryParse<StatusOrdemServico>(input.NovoStatus, true, out var novoStatus))
                return BadRequest($"Status '{input.NovoStatus}' inválido.");

            ordem.DefinirStatus(novoStatus);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Status atualizado via e-mail com sucesso.",
                numeroOS = ordem.NumeroOS,
                statusAnterior = input.NovoStatus,
                statusAtual = ordem.Status.ToString(),
                origemEmail = input.RemetenteEmail
            });
        }

        public class AtualizacaoStatusEmailInput
        {
            public string NumeroOS { get; set; } = "";
            public string NovoStatus { get; set; } = "";
            public string? RemetenteEmail { get; set; }
            public string? AssuntoOriginal { get; set; }
        }

        // Input Models
        public class OrdemServicoInputModel
        {
            public Guid ClienteId { get; set; }
            public Guid VeiculoId { get; set; }
            public List<ServicoInput>? Servicos { get; set; }
            public List<PecaInput>? Pecas { get; set; }
        }

        public class ServicoInput
        {
            public string Descricao { get; set; } = "";
            public decimal Preco { get; set; }
        }

        public class PecaInput
        {
            public string Nome { get; set; } = "";
            public int Quantidade { get; set; }
            public decimal PrecoUnitario { get; set; }
        }
    }
}