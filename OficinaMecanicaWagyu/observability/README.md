# Observabilidade — New Relic

Este diretório contém a infraestrutura como código (Terraform) dos
dashboards e alertas da Fase 3. A **coleta de dados** (logs, métricas de
Kubernetes) é feita por um agente instalado no cluster via Helm — não pelo
Terraform, já que é um componente do cluster, não da conta New Relic.

## Passo 1 — Criar a conta New Relic (gratuita, sem expiração)

1. Acesse https://newrelic.com/signup e crie uma conta gratuita.
2. Pegue o **Account ID**: aparece na URL do painel
   (`one.newrelic.com/nr1-core?account=SEU_ACCOUNT_ID`) ou em
   *Account settings*.
3. Crie uma **User API Key**: *Account settings → API keys → Create a key*
   (tipo "User"). Começa com `NRAK-...`.
4. Pegue a **License Key** (diferente da API Key acima): *Account settings
   → API keys → filtre por "Ingest - License"*. Usada pelo agente do
   Kubernetes.

## Passo 2 — Instalar o agente no cluster (Helm)

No PC com acesso ao cluster K3s (kubeconfig configurado):

```bash
helm repo add newrelic https://helm-charts.newrelic.com
helm repo update

helm upgrade --install newrelic-bundle newrelic/nri-bundle \
  --set global.licenseKey=SUA_LICENSE_KEY_AQUI \
  --set global.cluster=oficina-wagyu-k3s \
  --namespace newrelic --create-namespace \
  --set newrelic-infrastructure.privileged=true \
  --set global.lowDataMode=true \
  --set ksm.enabled=true \
  --set kubeEvents.enabled=true \
  --set logging.enabled=true
```

Isso instala, no cluster:
- **Infrastructure agent** (DaemonSet) — coleta CPU/memória de nós e pods
  (métricas `K8sContainerSample`, usadas no dashboard de infraestrutura).
- **Kube State Metrics** — estado dos recursos Kubernetes (Deployments,
  HPA, etc.).
- **Fluent Bit (logging)** — envia os logs JSON estruturados (Serilog) de
  todos os pods, incluindo o `CorrelationId` de cada requisição, para o
  New Relic Logs.

## Passo 3 — Confirmar que os dados estão chegando

No painel do New Relic: *Logs* (deve mostrar logs do pod `oficina-api`) e
*Infrastructure → Kubernetes* (deve mostrar o cluster `oficina-wagyu-k3s`).
Isso pode levar 2-5 minutos após a instalação do Helm chart.

## Passo 4 — Aplicar dashboards e alertas (Terraform)

```bash
cd observability
cp example.tfvars terraform.tfvars
# preencha newrelic_account_id, newrelic_api_key, healthcheck_url

terraform init
terraform plan
terraform apply
```

Isso cria:
- Um **dashboard** com 2 páginas: "Negócio" (volume diário de OS, tempo
  médio por status, erros de integração) e "Infraestrutura" (latência,
  CPU/memória, uptime).
- Um **monitor de Synthetics** fazendo ping no `/health` a cada 5 minutos.
- Uma **política de alerta** disparando quando há mais de 3 falhas de
  processamento/integração de OS em 5 minutos.

## Ajustando as queries NRQL

Os nomes de atributo usados nas queries (`NumeroOS`, `StatusAtual`,
`TempoDecorridoMinutos`, `Integracao`, `Motivo`) vêm das propriedades
logadas pela aplicação via Serilog (ver
`Application/UseCases/OrdensServico/*.cs`). Depois que os primeiros logs
chegarem, confira os nomes exatos dos atributos em *Logs → clique em um
log → painel de atributos à direita* — o parser de JSON do New Relic pode
ajustar ligeiramente a nomenclatura, e as queries podem precisar de pequenos
ajustes manuais no arquivo `main.tf` deste diretório.

## Notificação de alertas

Este Terraform cria a **condição** de alerta, mas não um canal de
notificação (e-mail/Slack) — isso envolve recursos adicionais
(`newrelic_notification_destination`, `newrelic_workflow`) que podem ser
configurados rapidamente pela própria interface do New Relic
(*Alerts → Notification channels*), sem necessidade de Terraform, para
manter este exemplo enxuto.
