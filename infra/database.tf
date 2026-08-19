resource "kubernetes_namespace" "oficina" {
  metadata {
    name = var.namespace
  }

  depends_on = [kind_cluster.oficina]
}

resource "kubernetes_secret" "oficina_api_secret" {
  metadata {
    name      = "oficina-api-secret"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }

  data = {
    "SA_PASSWORD" = var.sa_password
    "ConnectionStrings__DefaultConnection" = "Server=oficina-sqlserver,1433;Database=OficinaMecanicaDB;User Id=sa;Password=${var.sa_password};TrustServerCertificate=True;MultipleActiveResultSets=true"
    "EmailWebhook__Secret" = var.email_webhook_secret
  }

  type = "Opaque"
}

resource "kubernetes_config_map" "oficina_api_config" {
  metadata {
    name      = "oficina-api-config"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }

  data = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://+:8080"
    SQLSERVER_HOST         = "oficina-sqlserver"
    SQLSERVER_DATABASE     = "OficinaMecanicaDB"
  }
}

resource "kubernetes_stateful_set" "sqlserver" {
  metadata {
    name      = "oficina-sqlserver"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }

  spec {
    service_name = "oficina-sqlserver"
    replicas     = 1

    selector {
      match_labels = { app = "oficina-sqlserver" }
    }

    template {
      metadata {
        labels = { app = "oficina-sqlserver" }
      }

      spec {
        container {
          name  = "sqlserver"
          image = "mcr.microsoft.com/mssql/server:2022-latest"

          port {
            container_port = 1433
          }

          env {
            name  = "ACCEPT_EULA"
            value = "Y"
          }
          env {
            name = "MSSQL_SA_PASSWORD"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.oficina_api_secret.metadata[0].name
                key  = "SA_PASSWORD"
              }
            }
          }

          resources {
            requests = { cpu = "250m", memory = "1Gi" }
            limits   = { cpu = "1", memory = "2Gi" }
          }

          volume_mount {
            name       = "sqlserver-data"
            mount_path = "/var/opt/mssql"
          }
        }
      }
    }

    volume_claim_template {
      metadata {
        name = "sqlserver-data"
      }
      spec {
        access_modes = ["ReadWriteOnce"]
        resources {
          requests = { storage = "2Gi" }
        }
      }
    }
  }
}

resource "kubernetes_service" "sqlserver" {
  metadata {
    name      = "oficina-sqlserver"
    namespace = kubernetes_namespace.oficina.metadata[0].name
  }

  spec {
    selector   = { app = "oficina-sqlserver" }
    cluster_ip = "None"

    port {
      port        = 1433
      target_port = 1433
    }
  }
}
