# Diagrama de Componentes — Oficina Mecânica Wagyu (Fase 3)

Este diagrama mostra a arquitetura de nuvem completa: autenticação via CPF,
a aplicação rodando em Kubernetes, o banco de dados gerenciado, a pipeline
de CI/CD e a camada de observabilidade.

```mermaid
graph TB
    subgraph CLIENTE["Cliente / Usuário"]
        USR[Cliente da Oficina<br/>navegador / Postman]
    end

    subgraph AWS["AWS (AWS Academy Learner Lab)"]
        subgraph GATEWAY["API Gateway"]
            APIGW["API Gateway HTTP API<br/>POST /auth/cpf"]
        end

        subgraph SERVERLESS["Function Serverless"]
            LAMBDA["Lambda: oficina-wagyu-auth-cpf<br/>(Node.js 20)<br/>- Valida CPF<br/>- Consulta status do cliente<br/>- Emite JWT"]
        end

        subgraph K8S["Cluster Kubernetes (K3s em EC2)"]
            SVC["Service (NodePort 30080)"]
            subgraph DEPLOY["Deployment oficina-api (HPA 2-6 réplicas)"]
                POD1["Pod: oficina-api #1"]
                POD2["Pod: oficina-api #2"]
            end
            CM["ConfigMap"]
            SEC["Secret<br/>(connection string, JWT secret,<br/>webhook secret)"]
        end

        subgraph DB["Banco de Dados Gerenciado"]
            RDS[("RDS SQL Server Express<br/>OficinaMecanicaDB")]
        end

        subgraph OBS["Observabilidade"]
            AGENT["Datadog / New Relic Agent"]
            DASH["Dashboards:<br/>- Volume diário de OS<br/>- Tempo médio por status<br/>- Erros de integração"]
            ALERT["Alertas de falha"]
        end
    end

    subgraph CICD["CI/CD (GitHub Actions)"]
        GH1["Repo: OficinaMecanicaWagyu"]
        GH2["Repo: oficina-wagyu-lambda-auth"]
        GH3["Repo: oficina-wagyu-infra-k8s"]
        GH4["Repo: oficina-wagyu-infra-database"]
    end

    USR -- "1 - POST CPF" --> APIGW
    APIGW -- "invoca" --> LAMBDA
    LAMBDA -- "2 - consulta Clientes<br/>(existência + Ativo)" --> RDS
    LAMBDA -- "3 - devolve JWT" --> USR

    USR -- "4 - chamadas às APIs protegidas<br/>Authorization Bearer JWT" --> SVC
    SVC --> POD1
    SVC --> POD2
    POD1 -. lê .-> CM
    POD1 -. lê .-> SEC
    POD2 -. lê .-> CM
    POD2 -. lê .-> SEC
    POD1 -- "EF Core / SqlClient" --> RDS
    POD2 -- "EF Core / SqlClient" --> RDS

    POD1 -. logs JSON estruturados .-> AGENT
    POD2 -. logs JSON estruturados .-> AGENT
    AGENT --> DASH
    AGENT --> ALERT

    GH1 -- "build, testa, builda imagem,<br/>deploy no cluster" --> K8S
    GH2 -- "empacota e faz deploy<br/>da function" --> LAMBDA
    GH3 -- "terraform apply<br/>(provisiona EC2 + K3s)" --> K8S
    GH4 -- "terraform apply<br/>(provisiona RDS)" --> RDS
```

## Legenda dos fluxos numerados

1. **Autenticação**: o cliente envia o CPF para o API Gateway, que roteia
   para a Lambda `oficina-wagyu-auth-cpf`.
2. A Lambda consulta a tabela `Clientes` no RDS — checando **existência**
   (documento cadastrado) **e status** (campo `Ativo`).
3. Se o CPF for válido, existir, e o cliente estiver ativo, a Lambda emite
   um **JWT** assinado com um segredo compartilhado (`Jwt__Secret`) e
   devolve ao cliente.
4. O cliente usa esse JWT (`Authorization: Bearer <token>`) para consumir
   as APIs protegidas da aplicação, que rodam como pods no cluster K3s.

## Por que separar Lambda, K8s e Banco em repositórios diferentes

Ver os READMEs de cada repositório
(`oficina-wagyu-lambda-auth`, `oficina-wagyu-infra-k8s`,
`oficina-wagyu-infra-database`) e o RFC de escolha de arquitetura de
implantação (a ser adicionado). Em resumo: o desafio exige repositórios
segregados, cada um com seu próprio pipeline de CI/CD e ciclo de deploy
independente — trocar a versão da Lambda não deveria exigir reaplicar o
Terraform do cluster Kubernetes, por exemplo.

## Onde este diagrama se encaixa nos demais documentos

- **Diagrama de Sequência** (a ser adicionado): detalha, passo a passo, o
  fluxo de autenticação e o fluxo de abertura de uma Ordem de Serviço.
- **RFCs** (a ser adicionado): justificam as escolhas mostradas aqui — por
  que AWS, por que RDS SQL Server, por que K3s em vez de EKS, por que JWT
  compartilhado entre a Lambda e a aplicação.
- **ADRs** (`OficinaMecanicaWagyu/docs/adr/`): decisões arquiteturais
  permanentes, como a arquitetura em camadas/Clean Architecture (ADR-001,
  ADR-002).
