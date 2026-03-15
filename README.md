# OrderFlow

Backend de processamento de pedidos desenvolvido com .NET 9, demonstrando arquitetura limpa, CQRS, comunicação assíncrona entre microsserviços e padrões de resiliência.

---

## Arquitetura

A solução é composta por dois serviços independentes que se comunicam de forma assíncrona via RabbitMQ:

```
┌─────────────────────────────────────────────────────┐
│                    OrderFlow.Api                    │
│              REST API  ·  .NET 9  ·  EF Core        │
│                                                     │
│  POST /api/orders  ──►  CreateOrderHandler          │
│                              │                      │
│                              ├── Salva no PostgreSQL│
│                              └── Publica evento ───►├──┐
└─────────────────────────────────────────────────────┘   │
                                                          │ RabbitMQ
┌─────────────────────────────────────────────────────┐   │ exchange: orderflow
│                  OrderFlow.Worker                   │◄──┘ routing key: orders.created
│           Background Service  ·  .NET 9             │
│                                                     │
│  OrderCreatedConsumer (AsyncEventingBasicConsumer)  │
│  ├── ACK em caso de sucesso                         │
│  └── NACK + requeue em caso de falha                │
└─────────────────────────────────────────────────────┘
```

### Estrutura do projeto

```
src/
  OrderFlow.Domain/          # Entidades, enums, eventos, exceções — sem dependências externas
  OrderFlow.Application/     # Handlers CQRS, validadores, interfaces
  OrderFlow.Infrastructure/  # EF Core, publisher RabbitMQ, implementações de repositório
  OrderFlow.Api/             # API REST ASP.NET Core
  OrderFlow.Worker/          # Serviço consumidor em background
tests/
  OrderFlow.UnitTests/       # xUnit + FluentAssertions + Moq
```

---

## Stack

| Responsabilidade | Tecnologia |
|---|---|
| Runtime | .NET 9 |
| API | ASP.NET Core |
| ORM | Entity Framework Core 8 + Npgsql |
| Banco de dados | PostgreSQL 16 |
| Mensageria | RabbitMQ 3.13 |
| CQRS | MediatR 12 |
| Validação | FluentValidation 11 |
| Resiliência | Polly 8 (retry, circuit breaker, timeout) |
| Logs | Serilog |
| Testes | xUnit + FluentAssertions + Moq |
| Containers | Docker + Docker Compose |
| CI/CD | GitHub Actions |

---

## Decisões de arquitetura

**Arquitetura limpa com regras estritas de dependência**
A camada de domínio não possui nenhuma dependência externa. A camada de aplicação define interfaces, não implementa. A infraestrutura implementa as interfaces definidas pela aplicação — nunca o contrário. Isso mantém as regras de negócio completamente isoladas e testáveis sem precisar subir banco de dados ou broker.

**CQRS via MediatR**
Comandos e queries são separados em classes distintas, cada um com um único handler. O pipeline behavior `ValidationBehavior<TRequest, TResponse>` intercepta toda requisição e executa o FluentValidation antes do handler, mantendo a validação completamente fora dos controllers.

**Máquina de estados no domínio**
`Order` expõe métodos de transição explícitos (`Confirm`, `Process`, `Ship`, `Deliver`, `Cancel`) e valida transições inválidas internamente lançando `DomainException`. Nenhum código externo consegue colocar um pedido em estado inválido.

**Publicação resiliente com Polly**
O `RabbitMqPublisher` envolve cada publicação em um `ResiliencePipeline` composto por:
- Retry com backoff exponencial (3 tentativas, delay base de 200ms)
- Circuit breaker (abre após 50% de falha em 5+ chamadas, pausa de 15s)
- Timeout de 5 segundos

**Consumer com acknowledgement manual**
O worker utiliza `autoAck: false` e `BasicQos(prefetchCount: 1)`. A mensagem só recebe ACK após processamento bem-sucedido. Em caso de falha recebe NACK com requeue, evitando perda de mensagens.

---

## Executando localmente

**Pré-requisito:** Docker e Docker Compose.

```bash
git clone https://github.com/seu-usuario/orderflow.git
cd orderflow
docker compose up -d
```

Serviços disponíveis em:
- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)
- Health check: http://localhost:8080/health

**Aplicar migrations do banco:**
```bash
dotnet ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
```

---

## Referência da API

### Criar pedido
```http
POST /api/orders
Content-Type: application/json

{
  "customerId": "cliente-123",
  "items": [
    {
      "productId": "prod-abc",
      "productName": "Teclado Mecânico",
      "quantity": 1,
      "unitPrice": 349.90
    }
  ]
}
```

### Listar pedidos (paginado)
```http
GET /api/orders?page=1&pageSize=20
```

### Buscar pedido por ID
```http
GET /api/orders/{id}
```

### Atualizar status do pedido
```http
PATCH /api/orders/{id}/status
Content-Type: application/json

{
  "status": 1
}
```

Valores de status: `0` Pendente · `1` Confirmado · `2` Em processamento · `3` Enviado · `4` Entregue · `5` Cancelado

---

## Testes

```bash
dotnet test
```

```bash
# Com relatório de cobertura
dotnet test --collect:"XPlat Code Coverage"
```

---

## CI/CD

O pipeline do GitHub Actions executa a cada push em `main` ou `develop` e em pull requests:

1. Restaurar dependências
2. Build em modo Release
3. Execução dos testes com coleta de cobertura
4. Build das imagens Docker de ambos os serviços

Consulte [`.github/workflows/ci.yml`](.github/workflows/ci.yml).
