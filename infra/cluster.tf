# Provisiona um cluster Kubernetes local (Kind) — sem custo de cloud.
# Requer Docker rodando na máquina (o Kind sobe os nós como containers Docker).
resource "kind_cluster" "oficina" {
  name           = var.cluster_name
  wait_for_ready = true

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    node {
      role = "control-plane"

      # Expõe a porta do NodePort da API no host, pra acessar via localhost
      extra_port_mappings {
        container_port = 30080
        host_port       = 8080
        protocol        = "TCP"
      }
    }

    node {
      role = "worker"
    }
  }
}

provider "kubernetes" {
  host                   = kind_cluster.oficina.endpoint
  cluster_ca_certificate = kind_cluster.oficina.cluster_ca_certificate
  client_certificate     = kind_cluster.oficina.client_certificate
  client_key             = kind_cluster.oficina.client_key
}
