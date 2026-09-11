variable "newrelic_account_id" {
  description = "ID da conta New Relic (Account settings -> Account ID)"
  type        = string
}

variable "newrelic_api_key" {
  description = "User API Key do New Relic (começa com NRAK-...)"
  type        = string
  sensitive   = true
}
