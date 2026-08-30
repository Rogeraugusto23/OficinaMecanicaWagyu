# ADR-002: Migrar para Clean Architecture, começando pelo módulo OrdensServico

## Status
Aceito. Supersede o [ADR-001](./ADR-001-arquitetura-em-camadas-SUPERSEDED.md).

## Contexto

O ADR-001 registrou a decisão de manter a arquitetura em camadas simples da
Fase 1, priorizando a entrega dos requisitos de infraestrutura (Docker,
Kubernetes, Terraform, CI/CD) dentro do prazo. Concluídos esses requisitos,
e com a Fase 3 se aproximando — que adiciona API Gateway, autenticação
serverless e mais integrações externas — o time revisou a decisão: adicionar
essas novas integrações em cima de controllers acoplados diretamente ao
`OficinaDbContext` (EF Core) tornaria o código progressivamente mais difícil
de testar e evoluir, exatamente no momento em que a aplicação passa a crescer
em complexidade.

## Decisão

Migrar para Clean Architecture, com quatro camadas e a Regra de Dependência
clássica (`API → Application → Domain`; `Infrastructure` implementa
interfaces definidas no `Domain`):

```
Domain/          → Entidades, Value Objects, Enums, Validators, Interfaces
                    (contratos de repositório — sem dependência de EF Core)
Application/     → Use Cases (um por operação de negócio) e DTOs de
                    entrada/saída. Não depende de ASP.NET Core nem de EF Core.
Infrastructure/  → DbContext (EF Core) e Repositories (implementações
                    concretas das interfaces do Domain)
API/             → Controllers finos: recebem HTTP, chamam um Use Case,
                    traduzem o resultado para a resposta HTTP
```

Dado o prazo apertado até a entrega da Fase 3 (15/09), a migração foi feita
**incrementalmente, por módulo**, começando pelo módulo `OrdensServico` —
o núcleo dos requisitos funcionais da Fase 2 (abertura de OS, consulta de
status, aprovação/rejeição de orçamento, listagem ordenada, atualização via
e-mail). Os demais controllers (`Pecas`, `Servicos`, `Veiculos`, `Auth`,
`ConsultaPublica`) permanecem no padrão anterior por ora, e serão migrados
seguindo o mesmo padrão à medida que o tempo permitir — sem necessidade de
uma reescrita "big bang" simultânea.

### O que mudou concretamente no módulo OrdensServico

- `Domain/Interfaces/IOrdemServicoRepository.cs` — contrato de acesso a dados.
- `Infrastructure/Repositories/OrdemServicoRepository.cs` — implementação
  concreta com EF Core.
- `Application/UseCases/OrdensServico/*` — um Use Case por operação:
  `AbrirOrdemServicoUseCase`, `ListarOrdensServicoUseCase`,
  `ConsultarOrdemServicoUseCase`, `AvancarStatusUseCase`,
  `EnviarOrcamentoUseCase`, `AprovarOrcamentoUseCase`,
  `RejeitarOrcamentoUseCase`, `AtualizarStatusPorEmailUseCase`.
- `Application/Common/OperationResult.cs` — wrapper de resultado que mantém a
  Application layer livre de qualquer dependência de ASP.NET Core
  (`ActionResult`, `StatusCode`); quem traduz o resultado para HTTP é o
  Controller.
- `Application/DTOs/OrdensServico/*` — DTOs de entrada/saída, movidos de
  dentro do Controller para a Application.
- `API/OrdensServicoController.cs` — reescrito para não depender mais do
  `OficinaDbContext`; injeta os Use Cases e só traduz HTTP.
- Testes automatizados novos (`OrdensServicoUseCasesTests.cs`) exercitam os
  Use Cases diretamente com EF Core InMemory, sem precisar de um
  `ControllerBase` nem de um banco de verdade.

A validação do segredo do webhook de e-mail (`X-Webhook-Secret`)
permaneceu no Controller: é uma preocupação de transporte/segurança HTTP,
não uma regra de negócio de domínio.

## Alternativas consideradas

1. **Migrar tudo de uma vez (todos os controllers).**
   Descartada por risco de regressão dado o prazo apertado até a Fase 3 —
   ver ADR-001 para o raciocínio original, que continua válido para o
   dimensionamento do esforço.
2. **Não migrar (manter ADR-001 como estava).**
   Descartada porque a Fase 3 introduz complexidade adicional (Gateway,
   autenticação serverless via CPF, mais integrações) que se beneficia
   diretamente de um Domain isolado de infraestrutura.
3. **Migração incremental por módulo, começando por OrdensServico (adotada).**
   Menor risco, entrega o padrão de referência para os próximos módulos, e
   prioriza o módulo com maior densidade de regras de negócio exigidas pelo
   desafio.

## Consequências

- Os controllers `Pecas`, `Servicos`, `Veiculos`, `Auth` e `ConsultaPublica`
  ainda dependem diretamente do `OficinaDbContext`. Isso é um débito técnico
  assumido conscientemente, a ser resolvido seguindo o mesmo padrão do
  módulo `OrdensServico`.
- Novas integrações da Fase 3 que envolvam Ordens de Serviço (por exemplo,
  a autenticação via CPF chamando a API protegida) podem ser construídas
  como novos Use Cases, sem tocar em detalhes de EF Core.
- Os testes unitários dos Use Cases não sobem um `WebApplicationFactory` nem
  um `ControllerBase` — rodam rápido, isolados, usando apenas EF Core
  InMemory por trás da interface de repositório.
