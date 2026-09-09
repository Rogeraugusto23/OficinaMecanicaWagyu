# ADR-004: Uso do Horizontal Pod Autoscaler (HPA) para escalabilidade

## Status
Aceito

## Contexto

O desafio exige um "Cluster Kubernetes com escalabilidade" e,
especificamente, um HPA "escalando conforme consumo de CPU/memória". É
preciso decidir os parâmetros de escalonamento.

## Decisão

Configurar o HPA do Deployment `oficina-api` com:

- **Réplicas mínimas:** 2 (garante alta disponibilidade básica — uma
  atualização ou falha em um pod não derruba o serviço)
- **Réplicas máximas:** 6
- **Métricas de escalonamento:** CPU (alvo: 70% de utilização média) e
  memória (alvo: 80% de utilização média) — o HPA escala se **qualquer**
  uma das duas métricas ultrapassar o alvo
- **Comportamento de scale-down:** janela de estabilização de 60 segundos
  (evita "flapping" — escalar para baixo e para cima repetidamente em
  resposta a picos curtos de carga)
- **Comportamento de scale-up:** sem janela de estabilização (reage
  imediatamente a picos, priorizando disponibilidade sobre economia de
  recursos)

## Alternativas consideradas

### Escalonamento manual (sem HPA)
- **Contras:** não atende ao requisito explícito do desafio; exigiria
  intervenção humana para reagir a picos de carga.

### KEDA (Kubernetes Event-Driven Autoscaling) baseado em métricas customizadas
Escalar com base em métricas de negócio (ex: número de OS na fila de
"Recebida" aguardando processamento) em vez de CPU/memória.

- **Prós:** escalonamento mais alinhado à carga de negócio real.
- **Contras:** exigiria instrumentar a aplicação para expor essas métricas
  em um formato compatível (Prometheus), instalar e configurar o KEDA no
  cluster, e desenvolver/testar os `ScaledObject` correspondentes —
  complexidade não justificada pelo prazo e escopo do desafio, que pede
  explicitamente escalonamento "conforme consumo de CPU/memória" (ou seja,
  o HPA nativo já atende ao requisito como especificado).

## Justificativa

O HPA nativo baseado em `metrics-server` (já incluso por padrão no K3s,
sem necessidade de instalação adicional) atende exatamente ao que o
desafio pede, com o menor esforço de implementação e manutenção. Os
valores de 70%/80% seguem a prática comum de deixar margem de segurança
antes de saturar os recursos do pod, e o mínimo de 2 réplicas evita um
único ponto de falha mesmo sem carga.

## Consequências

- A demonstração de escalabilidade no vídeo (requisito explícito da
  entrega) deve gerar carga suficiente para ultrapassar 70% de CPU em pelo
  menos um pod, para que o HPA visivelmente crie novas réplicas — uma
  ferramenta simples de carga (ex: `hey`, `ab`, ou várias chamadas
  concorrentes via script) é suficiente para essa demonstração.
- Os `resources.requests`/`resources.limits` definidos no Deployment
  (`200m`/`500m` CPU, `256Mi`/`512Mi` memória) foram dimensionados para
  tornar esse teste de carga alcançável sem exigir volume de requisições
  irrealista para uma demonstração em vídeo de poucos minutos.
