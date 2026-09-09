# ADR-003: Padrão de comunicação síncrona (REST/HTTP) entre os componentes

## Status
Aceito

## Contexto

A Fase 3 introduz múltiplos componentes que precisam se comunicar: o
Cliente, o API Gateway, a Lambda de autenticação, e os pods da aplicação no
cluster Kubernetes. É preciso decidir o padrão de comunicação entre eles.

## Decisão

Comunicação **síncrona, via REST sobre HTTP**, em todos os pontos:

- Cliente → API Gateway → Lambda: HTTP síncrono (o cliente aguarda a
  resposta com o JWT antes de prosseguir).
- Cliente → Service do Kubernetes → Pods: HTTP síncrono (REST, já era o
  padrão desde a Fase 1).
- A validação do JWT pela aplicação é **local** (verificação de assinatura
  HS256), não envolve uma chamada de rede de volta à Lambda ou a um serviço
  de autorização — ver RFC-003.

## Alternativas consideradas

### Comunicação assíncrona via fila/eventos (SQS/SNS)
Por exemplo, a abertura de uma OS poderia publicar um evento em uma fila, e
a persistência no banco aconteceria de forma desacoplada, processada por um
worker.

- **Prós:** maior resiliência a picos de carga; desacopla o tempo de
  resposta do cliente do tempo de processamento.
- **Contras:** introduz complexidade de infraestrutura adicional (fila,
  consumidor, tratamento de mensagens duplicadas/fora de ordem) que não é
  exigida pelo desafio nem justificada pelo volume de dados de um projeto
  acadêmico. O cliente também esperaria receber o número da OS
  imediatamente na resposta do `POST /api/OrdensServico` (conforme
  especificado no desafio: "retornando a identificação única da OS") — o
  que exige, na prática, que a criação já tenha acontecido de forma
  síncrona antes da resposta.

## Justificativa

O escopo e os requisitos funcionais do desafio (respostas imediatas com
IDs gerados, autenticação bloqueante antes de liberar acesso) casam
naturalmente com comunicação síncrona. Introduzir mensageria assíncrona
adicionaria complexidade operacional sem resolver nenhum requisito
funcional pendente, dentro do prazo e do volume de uso esperado (ambiente
acadêmico de demonstração, não um sistema de produção sob carga real).

## Consequências

- Uma indisponibilidade momentânea do RDS ou da Lambda se reflete
  diretamente como erro para o cliente (não há fila amortecendo picos).
  Isso é aceitável no escopo atual e mitigado pelo HPA (ver ADR-004) e
  pelas réplicas mínimas configuradas no Deployment.
- Caso o projeto evolua para volumes de produção reais, esta decisão
  deveria ser revisitada — particularmente para o fluxo de notificação de
  aprovação/recusa de orçamento, que já é naturalmente assíncrono do ponto
  de vista de negócio (o cliente aprova em um momento posterior e
  desconhecido) mas está implementado hoje como um endpoint REST comum,
  aguardando ser chamado externamente.
