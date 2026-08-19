output "cluster_name" {
  value = kind_cluster.oficina.name
}

output "kubeconfig_path" {
  description = "Caminho do kubeconfig gerado pelo Kind — use com 'export KUBECONFIG=<valor>' ou 'kubectl --kubeconfig <valor>'"
  value       = kind_cluster.oficina.kubeconfig_path
}

output "namespace" {
  value = kubernetes_namespace.oficina.metadata[0].name
}

output "api_local_url" {
  description = "URL para acessar a API depois de aplicar k8s/04-api-deployment.yaml e fazer o port-forward"
  value       = "http://localhost:8080  (rode: kubectl -n oficina-wagyu port-forward svc/oficina-api 8080:80)"
}
