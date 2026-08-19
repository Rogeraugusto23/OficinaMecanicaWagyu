variable "cluster_name" {
  description = "Nome do cluster Kind local"
  type        = string
  default     = "oficina-wagyu"
}

variable "namespace" {
  description = "Namespace Kubernetes onde a aplicação e o banco rodam"
  type        = string
  default     = "oficina-wagyu"
}

variable "sa_password" {
  description = "Senha do usuário sa do SQL Server (uso local/dev)"
  type        = string
  default     = "Oficina@1234"
  sensitive   = true
}

variable "email_webhook_secret" {
  description = "Segredo compartilhado do webhook de atualização de status via e-mail"
  type        = string
  default     = "webhook-secret-local-dev-troque-em-producao"
  sensitive   = true
}
