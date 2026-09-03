# Diagrama de Sequência — Autenticação via CPF e Abertura de OS

## Fluxo 1 — Autenticação via CPF

```mermaid
sequenceDiagram
    participant C as Cliente
    participant GW as API Gateway
    participant L as Lambda auth-cpf
    participant DB as RDS (Clientes)

    C->>GW: POST /auth/cpf { cpf }
    GW->>L: invoca function
    L->>L: valida dígitos verificadores do CPF
    alt CPF com formato inválido
        L-->>C: 400 Bad Request
    else CPF válido
        L->>DB: SELECT Id, Nome, Ativo FROM Clientes WHERE Documento = @cpf
        alt Cliente não encontrado
            DB-->>L: nenhum registro
            L-->>C: 404 Not Found
        else Cliente inativo
            DB-->>L: Ativo = false
            L-->>C: 403 Forbidden
        else Cliente ativo
            DB-->>L: Id, Nome, Ativo = true
            L->>L: assina JWT (HS256, segredo compartilhado)
            L-->>C: 200 OK { token, expiraEm, cliente }
        end
    end
```

## Fluxo 2 — Abertura de Ordem de Serviço

```mermaid
sequenceDiagram
    participant C as Cliente
    participant API as API (Pod K3s)
    participant DBOS as RDS (OrdensServico)

    C->>API: POST /api/OrdensServico<br/>Authorization: Bearer {token}<br/>{ clienteId, veiculoId, servicos, pecas }
    API->>API: valida o JWT (assinatura e expiração)
    alt Token inválido ou expirado
        API-->>C: 401 Unauthorized
    else Token válido
        API->>API: AbrirOrdemServicoUseCase.ExecutarAsync(input)
        API->>API: cria entidade OrdemServico<br/>(gera NumeroOS, Status = Recebida)
        loop para cada serviço/peça informado
            API->>API: AdicionarServico / AdicionarPeca<br/>(recalcula ValorTotal)
        end
        API->>DBOS: INSERT OrdemServico + Servicos + Pecas
        DBOS-->>API: OK
        API-->>C: 201 Created { id, numeroOS, status: "Recebida", valorTotal }
    end
```

## Notas de implementação

- O JWT emitido pela Lambda usa o mesmo segredo (`Jwt__Secret`) configurado no
  Secret do Kubernetes — é assim que a API, rodando em um processo totalmente
  separado (pod no K3s), consegue validar um token emitido por outro processo
  (a Lambda), sem nenhuma chamada de rede entre os dois no momento da
  validação (validação de JWT é local, apenas verifica a assinatura).
- O `AbrirOrdemServicoUseCase` já existe na camada de Application do
  repositório principal (ver ADR-002 — Clean Architecture) e não precisou de
  nenhuma alteração para a Fase 3; a única mudança foi a origem do token
  (antes: login usuário/senha fixo; agora: também aceita o JWT emitido pela
  Lambda via CPF).
- Erros de validação do CPF (formato, cliente não encontrado, cliente
  inativo) são tratados **dentro da Lambda**, antes de qualquer chamada às
  APIs protegidas — a aplicação principal nunca recebe uma requisição de
  cliente que não deveria ter acesso.
