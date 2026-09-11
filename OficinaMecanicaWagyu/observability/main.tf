# ─────────────────────────────────────────────────────────
# Dashboard — atende aos 3 requisitos explícitos da Fase 3:
# volume diário de OS, tempo médio por status, erros de integração.
# Inclui também uma página de infraestrutura (latência, CPU/memória).
#
# NOTA: os nomes de atributo usados nas queries NRQL (ex: NumeroOS,
# StatusAtual, TempoDecorridoMinutos) correspondem às propriedades logadas
# pela aplicação via Serilog (ver Application/UseCases/OrdensServico/*).
# Confirme os nomes exatos no New Relic (Logs -> escolha um log -> painel de
# atributos) após os primeiros logs chegarem, pois o parser de JSON do
# New Relic pode renomear ligeiramente alguns campos.
# ─────────────────────────────────────────────────────────
resource "newrelic_one_dashboard" "oficina_wagyu" {
  name = "Oficina Mecânica Wagyu - Fase 3"

  page {
    name = "Negócio - Ordens de Serviço"

    widget_bar {
      title  = "Volume diário de Ordens de Serviço abertas"
      row    = 1
      column = 1
      width  = 6
      height = 3

      nrql_query {
        query = "SELECT count(*) FROM Log WHERE message LIKE '%OrdemServicoAberta%' FACET dateOf(timestamp) SINCE 30 days ago"
      }
    }

    widget_bar {
      title  = "Tempo médio de execução por status (minutos)"
      row    = 1
      column = 7
      width  = 6
      height = 3

      nrql_query {
        query = "SELECT average(TempoDecorridoMinutos) FROM Log WHERE message LIKE '%OrdemServicoStatusAlterado%' FACET StatusAtual SINCE 7 days ago"
      }
    }

    widget_table {
      title  = "Erros e falhas nas integrações"
      row    = 4
      column = 1
      width  = 6
      height = 3

      nrql_query {
        query = "SELECT count(*) FROM Log WHERE message LIKE '%IntegracaoFalhou%' OR message LIKE '%OrdemServicoTransicaoFalhou%' FACET Integracao, Motivo SINCE 7 days ago"
      }
    }

    widget_billboard {
      title  = "Ordens de Serviço abertas hoje"
      row    = 4
      column = 7
      width  = 6
      height = 3

      nrql_query {
        query = "SELECT count(*) FROM Log WHERE message LIKE '%OrdemServicoAberta%' SINCE today"
      }
    }
  }

  page {
    name = "Infraestrutura e Performance"

    widget_line {
      title  = "Latência das APIs (ms)"
      row    = 1
      column = 1
      width  = 6
      height = 3

      nrql_query {
        query = "SELECT average(Elapsed) FROM Log WHERE message LIKE '%HTTP%' FACET RequestPath SINCE 1 day ago TIMESERIES"
      }
    }

    widget_line {
      title  = "Consumo de CPU dos pods (Kubernetes)"
      row    = 1
      column = 7
      width  = 6
      height = 3

      nrql_query {
        query = "SELECT average(cpuUsedCores) FROM K8sContainerSample WHERE containerName = 'oficina-api' TIMESERIES SINCE 1 day ago"
      }
    }

    widget_line {
      title  = "Consumo de memória dos pods (Kubernetes)"
      row    = 4
      column = 1
      width  = 6
      height = 3

      nrql_query {
        query = "SELECT average(memoryUsedBytes) FROM K8sContainerSample WHERE containerName = 'oficina-api' TIMESERIES SINCE 1 day ago"
      }
    }

    widget_billboard {
      title  = "Uptime (healthcheck)"
      row    = 4
      column = 7
      width  = 6
      height = 3

      nrql_query {
        query = "SELECT percentage(count(*), WHERE result = 'SUCCESS') FROM SyntheticCheck WHERE monitorName = 'oficina-wagyu-healthcheck' SINCE 1 day ago"
      }
    }
  }
}

# ─────────────────────────────────────────────────────────
# Monitor de uptime/healthcheck (Synthetics) — ping simples no /health.
# Substitua o endpoint pelo IP público real da instância EC2 (ou pelo
# domínio, se configurar um) após o deploy da aplicação.
# ─────────────────────────────────────────────────────────
resource "newrelic_synthetics_monitor" "healthcheck" {
  name             = "oficina-wagyu-healthcheck"
  type             = "SIMPLE"
  locations_public = ["AWS_US_EAST_1"]
  period           = "EVERY_5_MINUTES"
  status           = "ENABLED"
  uri              = var.healthcheck_url

  sla_threshold = 99.0
}

# ─────────────────────────────────────────────────────────
# Alerta: falhas no processamento de Ordens de Serviço.
# Dispara quando há mais de 3 eventos de falha de transição/integração
# em uma janela de 5 minutos.
# ─────────────────────────────────────────────────────────
resource "newrelic_alert_policy" "oficina_wagyu" {
  name                = "Oficina Wagyu - Falhas de processamento de OS"
  incident_preference = "PER_CONDITION"
}

resource "newrelic_nrql_alert_condition" "falha_processamento_os" {
  account_id = var.newrelic_account_id
  policy_id  = newrelic_alert_policy.oficina_wagyu.id
  type       = "static"
  name       = "Falhas no processamento de Ordens de Serviço"
  enabled    = true

  nrql {
    query = "SELECT count(*) FROM Log WHERE message LIKE '%IntegracaoFalhou%' OR message LIKE '%OrdemServicoTransicaoFalhou%'"
  }

  critical {
    operator              = "above"
    threshold             = 3
    threshold_duration    = 300
    threshold_occurrences = "at_least_once"
  }

  fill_option = "none"
}
