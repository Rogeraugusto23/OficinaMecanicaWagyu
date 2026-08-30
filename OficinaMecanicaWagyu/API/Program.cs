using Microsoft.EntityFrameworkCore;
using OficinaMecanicaWagyu.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using OficinaMecanicaWagyu.Domain.Interfaces;
using OficinaMecanicaWagyu.Infrastructure.Repositories;
using OficinaMecanicaWagyu.Application.UseCases.OrdensServico;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=OficinaMecanicaDB;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<OficinaDbContext>(options =>
    options.UseSqlServer(connectionString));

// 1.1 Repositórios (Infrastructure implementa contratos do Domain)
builder.Services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();

// 1.2 Use Cases (Application) — um por operação de negócio do módulo OrdensServico
builder.Services.AddScoped<AbrirOrdemServicoUseCase>();
builder.Services.AddScoped<ListarOrdensServicoUseCase>();
builder.Services.AddScoped<ConsultarOrdemServicoUseCase>();
builder.Services.AddScoped<AvancarStatusUseCase>();
builder.Services.AddScoped<EnviarOrcamentoUseCase>();
builder.Services.AddScoped<AprovarOrcamentoUseCase>();
builder.Services.AddScoped<RejeitarOrcamentoUseCase>();
builder.Services.AddScoped<AtualizarStatusPorEmailUseCase>();

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
var key = Encoding.ASCII.GetBytes("ChaveSecretaOficinaWagyu2026_MuitoLonga");
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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Healthcheck simples, usado pelos probes do Kubernetes (readiness/liveness)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

app.Run();