# CI/CD — Configuração do Runner Self-Hosted

O pipeline (`.github/workflows/ci-cd.yml`) é dividido em dois jobs:

1. **`build-and-test`** — roda em um runner padrão da GitHub (nuvem). Faz build,
   roda os testes automatizados e builda a imagem Docker.
2. **`deploy`** — roda em um **runner self-hosted**, instalado na sua própria
   máquina, porque o cluster Kubernetes (Kind) só existe localmente. É esse job
   que carrega a imagem no cluster e aplica os manifestos.

Isso é necessário porque os runners padrão da GitHub rodam em máquinas na nuvem,
sem acesso à rede da sua casa/PC — não têm como "enxergar" um cluster Kind local.
Usar um runner self-hosted é a forma correta e realista de ter deploy automático
de verdade nesse cenário (é o mesmo padrão usado por empresas com clusters
on-premise).

## Passo 1 — Deixe o cluster Kind e o banco no ar

```powershell
cd infra
terraform apply
```

## Passo 2 — Coloque o kubeconfig no local padrão do kubectl

O runner self-hosted usa o `kubectl` "cru", sem nenhuma variável de ambiente
customizada — então o kubeconfig gerado pelo Terraform precisa estar no local
que o `kubectl` procura por padrão: `%USERPROFILE%\.kube\config` no Windows.

```powershell
# a partir da pasta infra/, depois do terraform apply
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.kube" | Out-Null
Copy-Item ".\oficina-wagyu-config" "$env:USERPROFILE\.kube\config" -Force
```

Confirma que funcionou:
```powershell
kubectl get nodes
```
(sem precisar mais definir `$env:KUBECONFIG` manualmente)

> ⚠️ Se você já tiver outros clusters configurados no seu `~/.kube/config`
> (pouco provável em uma máquina de estudo, mas possível), esse comando
> **sobrescreve** o arquivo. Se for o seu caso, use `kubectl config` para
> mesclar os contextos em vez de sobrescrever.

## Passo 3 — Registre o runner self-hosted no repositório

1. No GitHub, vá em **Settings** do repositório → **Actions** → **Runners**
   → **New self-hosted runner**
2. Escolha **Windows** e siga os comandos exibidos na tela (a GitHub gera um
   token de registro único). Em resumo, será algo como:

```powershell
mkdir actions-runner ; cd actions-runner
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/vX.X.X/actions-runner-win-x64-X.X.X.zip -OutFile actions-runner.zip
Expand-Archive -Path actions-runner.zip -DestinationPath .
./config.cmd --url https://github.com/SEU_USUARIO/OficinaMecanicaWagyu --token SEU_TOKEN_AQUI
```

3. Quando perguntado o nome do grupo/labels, pode aceitar os padrões (o workflow
   usa `runs-on: self-hosted`, que casa com qualquer runner self-hosted
   registrado, sem precisar de label customizada).

## Passo 4 — Rode o runner

Para testar manualmente primeiro:
```powershell
./run.cmd
```
Deixa essa janela aberta — é ela que "escuta" por novos jobs do GitHub Actions.

Para produção/uso contínuo, instale como serviço do Windows (fica rodando em
segundo plano, inclusive após reiniciar o PC):
```powershell
./svc.sh install   # ou, no Windows, use o instalador de serviço do próprio runner
```
(o instalador oficial do runner do Windows disponibiliza um comando específico
de "Install as a service" — siga as instruções que aparecem no terminal após
o `config.cmd`)

## Passo 5 — Teste a pipeline

Faz um `git push` de qualquer alteração pra branch `main` (ou abre e faz merge
de um Pull Request). Acompanhe em **Actions**, na aba do repositório no GitHub:

- O job `build-and-test` deve rodar em segundos/minutos num runner da GitHub
- O job `deploy` deve aparecer como "Waiting for a runner" por um instante, e
  depois rodar **no seu PC** (você vai ver a janela do `run.cmd`, se estiver
  rodando em modo manual, reagir e mostrar os logs em tempo real)

## Solução de problemas comuns

- **Job `deploy` fica preso em "Waiting for a runner"**: o runner não está
  rodando (`run.cmd` fechado, ou serviço parado). Volta pro passo 4.
- **`kubectl` falha com "connection refused" no runner**: o cluster Kind não
  está no ar, ou o kubeconfig em `~/.kube/config` está desatualizado (o
  Terraform gera um novo a cada `terraform apply` — se você recriou o
  cluster, repete o Passo 2).
- **Branch protegida bloqueando o push direto**: é esperado — o requisito da
  fase pede branch `main`/`master` protegida, com uso obrigatório de Pull
  Request. Configure isso em **Settings → Branches → Branch protection rules**.
