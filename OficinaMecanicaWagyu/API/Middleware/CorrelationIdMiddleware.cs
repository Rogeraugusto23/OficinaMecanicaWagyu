using Serilog.Context;

namespace OficinaMecanicaWagyu.API.Middleware;

/// <summary>
/// Garante que toda requisição tenha um Correlation ID (reaproveitando o do
/// header X-Correlation-Id se o cliente já enviar um, ou gerando um novo).
/// O ID é: (1) devolvido no header de resposta, para o cliente correlacionar
/// com seus próprios logs, e (2) injetado no contexto do Serilog, para que
/// TODO log emitido durante essa requisição inclua o campo "CorrelationId" —
/// essencial para rastrear uma requisição através de múltiplos pods/réplicas
/// no Kubernetes (HPA pode ter 2 a 6 pods respondendo em paralelo).
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
