# Infraestrutura (Terraform) — Oficina Mecânica Wagyu

Provisiona, localmente, via [Kind](https://kind.sigs.k8s.io/) (Kubernetes-in-Docker):

- Um cluster Kubernetes local (1 control-plane + 1 worker)
- O namespace `oficina-wagyu`
- O `Secret` e o `ConfigMap` usados pela aplicação
- O banco de dados (`SQL Server 2022`, como `StatefulSet` com volume persistente)

A **aplicação em si** (Deployment + Service + HPA) é aplicada separadamente, via
`kubectl apply` — manualmente durante o desenvolvimento local, ou automaticamente
pela pipeline de CI/CD. Assim, trocar a imagem da API (novo deploy) não exige
reaplicar o Terraform.

## Pré-requisitos

- [Docker](https://www.docker.com/products/docker-desktop/) rodando (o Kind sobe os nós como containers)
- [Kind](https://kind.sigs.k8s.io/docs/user/quick-start/#installation) instalado
- [Terraform](https://developer.hashicorp.com/terraform/install) >= 1.6
- [kubectl](https://kubernetes.io/docs/tasks/tools/#kubectl)

## Como provisionar

```bash
cd infra
terraform init
terraform plan
terraform apply
```

Isso cria o cluster e o banco. Para apontar o `kubectl` pro cluster criado:

```bash
export KUBECONFIG=$(terraform output -raw kubeconfig_path)
kubectl get nodes
kubectl -n oficina-wagyu get pods
```

## Aplicando a aplicação (Deployment/Service/HPA)

Depois que o Terraform terminar (cluster + banco no ar), builda e carrega a
imagem da API no cluster local, e aplica os manifestos da aplicação:

```bash
# a partir da raiz do repositório
docker build -t oficina-api:local -f Dockerfile .
kind load docker-image oficina-api:local --name oficina-wagyu

kubectl apply -f k8s/04-api-deployment.yaml
kubectl apply -f k8s/05-hpa.yaml
```

> O HPA precisa do `metrics-server` no cluster para funcionar. No Kind, instale com:
> ```bash
> kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
> kubectl patch deployment metrics-server -n kube-system --type='json' \
>   -p='[{"op":"add","path":"/spec/template/spec/containers/0/args/-","value":"--kubelet-insecure-tls"}]'
> ```
> (o patch é necessário porque o Kind usa certificados self-signed nos nós)

## Acessando a API

```bash
kubectl -n oficina-wagyu port-forward svc/oficina-api 8080:80
```

Depois acesse `http://localhost:8080/swagger`.

## Destruindo tudo

```bash
terraform destroy
```

## Recursos criados

| Recurso | Arquivo | Descrição |
|---|---|---|
| `kind_cluster.oficina` | `cluster.tf` | Cluster Kubernetes local (Kind) |
| `kubernetes_namespace.oficina` | `database.tf` | Namespace `oficina-wagyu` |
| `kubernetes_secret.oficina_api_secret` | `database.tf` | Senha do SA, connection string, segredo do webhook |
| `kubernetes_config_map.oficina_api_config` | `database.tf` | Variáveis não sensíveis da aplicação |
| `kubernetes_stateful_set.sqlserver` | `database.tf` | SQL Server 2022 com volume persistente (2Gi) |
| `kubernetes_service.sqlserver` | `database.tf` | Service headless para o SQL Server |
