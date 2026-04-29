# 🔧 Oficina Mecânica Wagyu

Sistema de gestão de ordens de serviço desenvolvido como projeto de Pós-Graduação em Arquitetura de Software.

A ideia é simples: digitalizar o fluxo de uma oficina mecânica, desde a abertura da OS até a entrega do veículo ao cliente.

---

## ▶️ Rodando o projeto

A forma mais fácil é com Docker. Com um único comando você sobe a API e o banco de dados:

```bash
docker compose up --build
```

Pronto. Acesse o Swagger em **http://localhost:8080/swagger** e explore os endpoints.

Para parar tudo:
```bash
docker compose down
```

---

## 🖥️ Prefere rodar sem Docker?

Sem problema. Você vai precisar do .NET 10 e do SQL Server (ou LocalDB) instalados.

```bash
cd OficinaMecanicaWagyu
dotnet restore
dotnet ef database update
dotnet run
```

---

## 🏗️ Como o projeto está organizado

Optei por um monolito em camadas, que é o suficiente para um MVP e mais fácil de evoluir:

- **API** → endpoints REST, autenticação JWT e documentação Swagger
- **Domain** → coração do sistema: entidades, regras de negócio, validações
- **Infrastructure** → banco de dados com Entity Framework Core e migrations

---

## 🗄️ Por que SQL Server?

Escolhi o SQL Server pela integração nativa com o Entity Framework Core e por ser o banco mais familiar no ecossistema .NET. Para desenvolvimento uso o LocalDB (zero configuração), e no Docker sobe um container com SQL Server 2022.

---

## 🔐 Como autenticar

A API administrativa é protegida por JWT. O fluxo é:

1. Faça login em `POST /api/Auth/login`
Usuario: admin
Senha: 123456
2. Copie o token da resposta
3. No Swagger, clique em **Authorize** e cole: `Bearer {seu_token}`

O endpoint de consulta pública (`GET /api/consulta/{numeroOS}`) não precisa de autenticação — ele foi feito para o cliente final acompanhar a OS pelo número.

---

## ✅ O que o sistema faz

- Abre e gerencia Ordens de Serviço
- Valida CPF/CNPJ dos clientes e placa dos veículos (padrão antigo e Mercosul)
- Calcula o orçamento automaticamente com base nos serviços e peças
- Controla o estoque de peças e avisa quando está abaixo do mínimo
- Acompanha o status da OS do recebimento até a entrega
- Permite que o cliente consulte o andamento da OS sem precisar de login

---

## 🧪 Testes

```bash
cd OficinaMecanicaWagyu.Tests
dotnet test
```

28 testes unitários cobrindo as regras de negócio principais: cálculo de orçamento, validação de documentos, controle de estoque e fluxo de status da OS.