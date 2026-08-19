# ADR-001: Manter arquitetura em camadas em vez de migrar para Clean Architecture/Hexagonal

## Status
Aceito

## Contexto

O enunciado da Fase 2 do Tech Challenge pede, como parte da "evolução da aplicação",
que o código seja refatorado aplicando Clean Architecture ou Arquitetura Hexagonal,
com separação adequada de camadas e dependências.

A aplicação, desde a Fase 1, está organizada em três camadas físicas:

```
API/            → Controllers, autenticação, composição da aplicação (Program.cs)
Domain/         → Entidades, Value Objects, Enums, Validators
Infrastructure/ → DbContext (EF Core), Migrations
```

Essa organização já separa razoavelmente bem as responsabilidades e mantém o
`Domain` livre de atributos ou dependências diretas do EF Core (o mapeamento
objeto-relacional fica isolado em `OficinaDbContext.OnModelCreating`). Porém,
ela **não** segue à risca a Regra de Dependência do Clean Architecture: os
`Controllers` da camada `API` dependem diretamente do `OficinaDbContext`
(um detalhe de infraestrutura), não existe uma camada de Application/Use Cases
isolando a orquestração de regras de negócio da camada HTTP, e não há
abstrações de repositório (`IOrdemServicoRepository` etc.) que permitiriam
trocar a implementação de persistência sem alterar os controllers.

Uma migração completa para Clean Architecture (adicionando uma camada
`Application` com Use Cases, DTOs e interfaces de repositório, e uma camada de
`Infrastructure/Repositories` implementando essas interfaces) impacta,
direta ou indiretamente, todos os controllers e o `DbContext` do projeto.

## Decisão

Para esta fase, **optamos por manter a arquitetura em camadas atual**, sem
migrar para Clean Architecture/Hexagonal completa, e concentrar o tempo
disponível nos demais requisitos obrigatórios da fase: containerização,
Kubernetes (Deployment, Service, ConfigMap/Secret, HPA), Terraform e
pipeline de CI/CD — todos com dependências de ambiente (Docker, cluster
local) que consumiram parte relevante do tempo disponível para a entrega.

## Alternativas consideradas

1. **Migração completa para Clean Architecture** (Application layer + Use
   Cases + interfaces de repositório em todos os módulos).
   - Prós: atende ao requisito à risca; maior testabilidade; desacoplamento
     real de infraestrutura.
   - Contras: escopo grande o suficiente para colocar em risco a entrega dos
     demais requisitos obrigatórios (K8s, Terraform, CI/CD) dentro do prazo
     desta fase; risco de introduzir regressões em fluxos já testados
     (28 testes unitários existentes) sem tempo hábil para reescrevê-los.

2. **Migração parcial, começando por um único fluxo vertical** (ex: apenas
   `OrdensServico` migrado para Use Cases + Repository, como prova de
   conceito, mantendo os demais controllers como estão).
   - Prós: menor risco; demonstra o padrão sem comprometer o prazo.
   - Contras: gera inconsistência arquitetural dentro do próprio código
     (parte do projeto em um estilo, parte em outro), o que pode ser
     confuso para quem for dar manutenção depois.

3. **Manter a arquitetura em camadas atual** (decisão adotada).
   - Prós: zero risco de regressão; tempo redirecionado para os requisitos
     de infraestrutura, que têm peso equivalente na avaliação e que já
     demandaram esforço considerável de configuração de ambiente local
     (Docker/Kubernetes); a separação Domain/Infrastructure/API já
     existente atenua parcialmente o acoplamento (o Domain continua livre
     de anotações de EF Core).
   - Contras: não atende ao requisito de Clean Architecture/Hexagonal como
     descrito no enunciado; controllers continuam acoplados diretamente ao
     `DbContext`, dificultando testes de unidade puros da lógica de negócio
     e a substituição futura do provedor de persistência.

## Consequências

- O débito técnico de não ter uma camada de Application/Use Cases e
  abstrações de repositório permanece registrado e deve ser tratado em uma
  iteração futura (por exemplo, no início da Fase 3, antes de acoplar novas
  integrações como API Gateway e autenticação serverless, que se beneficiam
  de um domínio bem isolado).
- Novos testes automatizados adicionados nesta fase continuam exercitando os
  controllers com o `DbContext`, em vez de testar Use Cases isolados — o que
  é uma limitação assumida conscientemente por esta decisão.
- Esta decisão não impede a adoção de Clean Architecture em módulos futuros;
  o time pode migrar incrementalmente, controller por controller, sem
  quebrar contrato de API, a partir do padrão descrito na seção "Alternativas
  consideradas" (opção 2).
