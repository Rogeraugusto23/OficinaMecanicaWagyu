# Oficina Mecânica Wagyu — Aplicação Principal

API de gestão de Ordens de Serviço, Clientes, Veículos e Peças de uma
oficina mecânica. Projeto de Tech Challenge (Pós-Tech FIAP), evoluído em 3
fases: monolito inicial (Fase 1) → containerização e Kubernetes local
(Fase 2) → arquitetura em nuvem com autenticação serverless (Fase 3).

## Repositórios do projeto

Este é o repositório da **aplicação principal**, que roda em Kubernetes. Os
demais componentes da arquitetura vivem em repositórios próprios:

| Repositório | Responsabilidade |
|---|---|
| [`OficinaMecanicaWagyu`](.) (este) | API principal, rodando em Kubernetes |
| [`oficina-wagyu-lambda-auth`](https://github.com/Rogeraugusto23/oficina-wagyu-lambda-auth) | Function Serverless de autenticação via CPF + API Gateway |
| [`oficina-wagyu-infra-k8s`](https://github.com/Rogeraugusto23/oficina-wagyu-infra-k8s) | Terraform do cluster Kubernetes (K3s em EC2) |
| [`oficina-wagyu-infra-database`](https://github.com/Rogeraugusto23/oficina-wagyu-infra-database) | Terraform do banco de dados gerenciado (RDS) |

## Arquitetura

Ver a documentação completa em [`docs/architecture/`](docs/architecture/):
- [Diagrama de Componentes](docs/architecture/diagrama-componentes.md)
- [Diagrama de Sequência](docs/architecture/diagrama-sequencia.md) (autenticação via CPF + abertura de OS)
- [Modelo de dados e diagrama ER](docs/architecture/modelo-dados-er.md)

Decisões arquiteturais documentadas em [`docs/adr/`](docs/adr/) e
[`docs/rfcs/`](docs/rfcs/).

## Tecnologias

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- Clean Architecture (módulos `OrdensServico` e `Clientes` — ver ADR-002)
- Serilog (logging estruturado em JSON, com correlação por requisição)
- JWT Bearer (segredo compartilhado com a Lambda de autenticação — ver RFC-003)
- Docker, Kubernetes (K3s), Terraform
- GitHub Actions (CI/CD)
- xUnit + FluentAssertions (testes automatizados)

## Autenticação

Duas formas de obter um token JWT:

1. **Via CPF** (Fase 3, fluxo principal para clientes): `POST` no endpoint da
   Lambda (repositório `oficina-wagyu-lambda-auth`) com `{ "cpf": "..." }`.
2. **Via usuário/senha** (legado da Fase 1/2, uso administrativo):
   `POST /api/Auth/login` com `usuario`/`senha`.

Ambos emitem um token compatível, aceito nas mesmas rotas protegidas.

## Executando localmente

```bash
docker compose up --build
```

Acesse `http://localhost:8080/swagger`.

## Deploy em Kubernetes

Ver o guia completo no repositório
[`oficina-wagyu-infra-k8s`](https://github.com/Rogeraugusto23/oficina-wagyu-infra-k8s).
Resumo: o cluster e os objetos base (Namespace, ConfigMap, Secret,
Deployment, Service, HPA) são provisionados a partir daquele repositório;
este repositório cuida apenas do build/deploy contínuo da imagem da
aplicação via `.github/workflows/ci-cd.yml`.

### Pipeline de CI/CD

- **Build & Test**: roda em runner padrão da GitHub — build, testes
  automatizados, validação da imagem Docker.
- **Deploy**: também roda em runner padrão da GitHub (não precisa de runner
  self-hosted) — conecta via SSH na instância EC2 pública, importa a
  imagem nova no K3s e reinicia o rollout automaticamente a cada push na
  `main`.

Configuração necessária (GitHub Secrets): `EC2_HOST` (IP público da EC2) e
`EC2_SSH_KEY` (chave privada SSH).

## Testes

```bash
cd OficinaMecanicaWagyu.Tests
dotnet test
```

## Collection de API

Swagger interativo disponível em `/swagger` em qualquer ambiente onde a
aplicação estiver rodando (ex: `http://localhost:8080/swagger` localmente).

## Observabilidade

Estratégia de observabilidade baseada em logs estruturados (Serilog +
correlação por requisição) e integração de infraestrutura Kubernetes via
New Relic — ver [ADR-005](docs/adr/ADR-005-observabilidade-new-relic.md) e
o diretório `observability/` (Terraform dos dashboards e alertas).
