using Microsoft.EntityFrameworkCore;
using OficinaMecanicaWagyu.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using OficinaMecanicaWagyu.Domain.Interfaces;
using OficinaMecanicaWagyu.Infrastructure.Repositories;
using OficinaMecanicaWagyu.Application.UseCases.OrdensServico;
using OficinaMecanicaWagyu.Application.UseCases.Clientes;
using Serilog;
using Serilog.Formatting.Compact;
using OficinaMecanicaWagyu.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 0. Logging estruturado em JSON (Serilog), com CorrelationId injetado por
// requisição via CorrelationIdMiddleware. CompactJsonFormatter produz um
// objeto JSON por linha de log — formato consumido nativamente por
// Datadog/New Relic/CloudWatch Logs (ver docs/observability).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "OficinaMecanicaWagyu")
    .WriteTo.Console(new CompactJsonFormatter()));

// 1. Configuração do Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=OficinaMecanicaDB;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<OficinaDbContext>(options =>
    options.UseSqlServer(connectionString));

// 1.1 Repositórios (Infrastructure implementa contratos do Domain)
builder.Services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

// 1.2 Use Cases (Application) — um por operação de negócio do módulo OrdensServico
builder.Services.AddScoped<AbrirOrdemServicoUseCase>();
builder.Services.AddScoped<ListarOrdensServicoUseCase>();
builder.Services.AddScoped<ConsultarOrdemServicoUseCase>();
builder.Services.AddScoped<AvancarStatusUseCase>();
builder.Services.AddScoped<EnviarOrcamentoUseCase>();
builder.Services.AddScoped<AprovarOrcamentoUseCase>();
builder.Services.AddScoped<RejeitarOrcamentoUseCase>();
builder.Services.AddScoped<AtualizarStatusPorEmailUseCase>();

// 1.3 Use Cases (Application) — módulo Clientes
builder.Services.AddScoped<CriarClienteUseCase>();
builder.Services.AddScoped<ListarClientesUseCase>();
builder.Services.AddScoped<ConsultarClienteUseCase>();
builder.Services.AddScoped<InativarClienteUseCase>();
builder.Services.AddScoped<ReativarClienteUseCase>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Configuração do Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Oficina API - Projeto Wagyu", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\""
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 3. Configuração JWT
// A chave é lida de configuração (Jwt:Secret / variável de ambiente Jwt__Secret)
// para poder ser IDÊNTICA à usada pela Lambda oficina-wagyu-auth-cpf (ver RFC-003),
// permitindo que a aplicação valide tokens emitidos por ela. O valor padrão
// mantém compatibilidade com o ambiente local/Fase 2, onde essa variável
// ainda não estava configurada.
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ChaveSecretaOficinaWagyu2026_MuitoLonga";
var key = Encoding.ASCII.GetBytes(jwtSecret);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseCorrelationId();
app.UseSerilogRequestLogging(); // loga cada requisição HTTP (método, path, status, duração) em JSON estruturado

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Healthcheck simples, usado pelos probes do Kubernetes (readiness/liveness)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

app.Run();