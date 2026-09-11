# CI/CD — Deploy automático na AWS

A pipeline `.github/workflows/ci-cd.yml` foi atualizada para fazer deploy
direto no cluster K3s da AWS via SSH, sem precisar de runner self-hosted
(diferente da abordagem local usada na Fase 2).

## Configurar os GitHub Secrets (uma vez)

Em **Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Valor |
|---|---|
| `EC2_HOST` | IP público da instância EC2 (ex: `18.206.197.109` — confira com `terraform output instance_public_ip` no repositório `oficina-wagyu-infra-k8s`) |
| `EC2_SSH_KEY` | Conteúdo **completo** do arquivo `.pem` da chave (abra com `Get-Content $env:USERPROFILE\.aws\oficina-wagyu-key.pem` e cole tudo, incluindo as linhas `-----BEGIN...` e `-----END...`) |

## Pré-requisito

Os objetos base do Kubernetes (Namespace, ConfigMap, Secret, Deployment,
Service, HPA) precisam já existir no cluster — aplicados uma vez a partir
do repositório `oficina-wagyu-infra-k8s` (ver o README de lá). Esta
pipeline só builda a imagem nova e reinicia o rollout; ela não cria esses
objetos do zero.

## ⚠️ Nota sobre o IP da EC2 mudar

Se o cluster for recriado (`terraform destroy` + `apply` no repositório
`oficina-wagyu-infra-k8s`), a instância ganha um **IP público novo**. Nesse
caso, é preciso atualizar o secret `EC2_HOST` com o valor novo antes da
próxima execução da pipeline — senão o `ssh` vai falhar tentando conectar
no IP antigo.
