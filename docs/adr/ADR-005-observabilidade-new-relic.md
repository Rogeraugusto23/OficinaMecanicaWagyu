# ADR-005: New Relic e observabilidade baseada em logs estruturados

## Status
Aceito

## Contexto

A Fase 3 exige integração com Datadog ou New Relic (escolha livre),
monitorando latência das APIs, consumo de CPU/memória do Kubernetes,
healthchecks/uptime, alertas de falha, e logs estruturados em JSON com
correlação entre requisições — além de 3 dashboards específicos (volume
diário de OS, tempo médio por status, erros de integração).

## Decisão

**New Relic**, com uma estratégia de observabilidade **baseada
majoritariamente em logs estruturados** (em vez de um agente de APM
completo instrumentando o runtime .NET).

## Alternativas consideradas

### Datadog
- **Contras:** o free tier do Datadog é um trial completo de 14 dias, que
  depois se converte automaticamente para um plano gratuito limitado (até
  5 hosts, sem alguns recursos). Dado que a demonstração em vídeo e a
  avaliação podem acontecer em um momento não totalmente previsível dentro
  do prazo, o New Relic — cujo free tier (100GB de ingestão de dados por
  mês) não expira — reduz o risco de o ambiente de observabilidade parar de
  funcionar por motivo de billing no meio da avaliação.

### APM completo (agente .NET instrumentando o runtime)
Tanto Datadog quanto New Relic oferecem agentes de APM que se anexam ao
runtime .NET e capturam traces distribuídos automaticamente, sem precisar
de código de logging manual.

- **Prós:** instrumentação mais rica (traces distribuídos, profiling).
- **Contras:** instalar e configurar corretamente um agente de APM dentro
  de um container Docker rodando em Kubernetes (variáveis de ambiente,
  volumes para arquivos do agente, ou modificação do Dockerfile para
  incluir o agente) adiciona superfície de risco e tempo de configuração
  não essencial: **todos os requisitos explícitos do desafio** (latência,
  CPU/memória, healthcheck, alertas, logs correlacionados, os 3 dashboards
  específicos) já são atendíveis com logs estruturados + a integração de
  infraestrutura do Kubernetes (que não requer nenhuma mudança na
  aplicação, apenas um Helm chart no cluster).

## Justificativa

A estratégia adotada — Serilog com formatação JSON compacta, middleware de
CorrelationId, e logs de negócio explícitos nos Use Cases (ex:
`OrdemServicoAberta`, `OrdemServicoStatusAlterado`, `IntegracaoFalhou`) —
gera exatamente os dados que os 3 dashboards exigidos precisam, sem exigir
um agente de APM. A latência das APIs já é capturada pelo
`UseSerilogRequestLogging` (tempo de cada requisição HTTP). CPU/memória do
Kubernetes vêm da integração de infraestrutura do New Relic (Helm chart),
que também não depende de mudança na aplicação.

## Consequências

- Não há traces distribuídos automáticos (ex: não é possível ver, num único
  trace, a chamada da API → banco de dados → tempo gasto em cada camada).
  Isso é uma limitação assumida conscientemente: o CorrelationId permite
  correlacionar logs de uma mesma requisição manualmente, mas não oferece
  a visualização de waterfall que um APM completo daria.
- Caso o projeto evolua além do escopo acadêmico, adicionar o agente de
  APM do New Relic permanece uma opção incremental — não exige reverter
  nada da estratégia de logging já implementada, apenas complementá-la.
- A precisão dos nomes de atributos nas queries NRQL dos dashboards
  depende de como o New Relic efetivamente parseia o JSON dos logs
  ingeridos — documentado como um passo de verificação manual no
  README de observabilidade.
