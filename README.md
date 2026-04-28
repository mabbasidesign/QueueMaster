# QueueMaster

QueueMaster is an event-driven microservices sample built on .NET 9, Azure Service Bus, Azure Functions, and Bicep infrastructure.

## Overview

The solution includes three core services:

1. OrderService: Minimal API for order CRUD operations, with an outbox publisher for reliable event publishing.
2. PaymentService: Minimal API plus background Service Bus consumer for processing order events.
3. NotificationFunction: Azure Functions isolated worker that consumes order events and sends email notifications through Azure Communication Services Email.

## Architecture

Flow summary:

1. Client calls OrderService HTTP endpoints.
2. OrderService writes order and outbox records.
3. Outbox publisher pushes OrderCreated events to Service Bus topic order-created-topic.
4. PaymentService consumes from subscription payment.
5. NotificationFunction consumes from subscription notification and sends email.

## Messaging Reliability

QueueMaster uses an outbox pattern in OrderService and a Service Bus consumer in PaymentService.

Current behavior summary:

1. Producer reliability: OrderService stores events in an outbox table and retries publish in a background worker.
2. Consumer retry: PaymentService uses manual completion. If processing fails, the message is not completed and is retried by Service Bus.
3. Dead-letter queue: Invalid payloads are dead-lettered explicitly. Other repeated failures are dead-lettered by Service Bus after max delivery count.
4. Idempotency: PaymentService stores processed message IDs and skips duplicate deliveries safely. MessageId is used as the primary idempotency key, with an OrderId-based fallback when MessageId is missing.

For full details, see Service Bus guide: SERVICEBUS-README.md.

## Tech Stack

- .NET 9
- ASP.NET Core Minimal APIs
- Azure Functions v4 (dotnet-isolated)
- Azure Service Bus
- Azure Communication Services Email
- SQL Server (EF Core)
- Azure Application Insights
- Bicep infrastructure modules

## Repository Layout

- QueueMaster.sln
- src/OrderService
- src/PaymentService
- src/NotificationFunction
- infra
- scripts

## Prerequisites

- .NET SDK 9.0+
- Azure Functions Core Tools v4 for local function runtime
- Azure CLI (for infrastructure deployment)
- SQL Server LocalDB or SQL Server instance
- Azure subscription and resource group for cloud deployment

## Local Setup

1. Clone the repository.
2. Restore and build.
3. Configure local settings for each service.
4. Run services.

```bash
git clone https://github.com/mabbasidesign/QueueMaster.git
cd QueueMaster
dotnet restore
dotnet build QueueMaster.sln
```

### Configuration

OrderService config is in src/OrderService/appsettings.json.

- ConnectionStrings:DefaultConnection
- ServiceBus section

PaymentService config is in src/PaymentService/appsettings.json.

- ConnectionStrings:DefaultConnection
- ServiceBus section

NotificationFunction local config is in src/NotificationFunction/local.settings.json.

- ServiceBusConnection
- Notification__ConnectionString
- Notification__SenderAddress
- Notification__RecipientAddresses

Important: do not commit real secrets. Use placeholder values, environment variables, user secrets, or a secure secret store.

## Run Locally

Run each service in a separate terminal.

OrderService:

```bash
cd src/OrderService
dotnet run
```

PaymentService:

```bash
cd src/PaymentService
dotnet run
```

NotificationFunction:

```bash
cd src/NotificationFunction
func host start
```

Default local URLs:

- OrderService: http://localhost:5000
- PaymentService: http://localhost:5243
- NotificationFunction host: default Functions port unless overridden

Swagger is enabled in Development for the two web APIs:

- OrderService: /swagger
- PaymentService: /swagger

## API Endpoints

OrderService:

- GET /api/orders
- GET /api/orders/{id}
- POST /api/orders
- PUT /api/orders/{id}
- DELETE /api/orders/{id}

PaymentService:

- GET /api/payments
- GET /api/payments/{transactionId}
- GET /api/payments/order/{orderId}
- POST /api/payments
- PUT /api/payments/{transactionId}
- DELETE /api/payments/{transactionId}

## Infrastructure

Infrastructure is modularized under infra:

- appinsights.bicep
- servicebus.bicep
- communication-email.bicep
- functionapp.bicep
- apim.bicep
- main.bicep
- main.bicepparam

The current templates provision diagnostics settings for Service Bus, Communication Service, and APIM into Log Analytics via the Application Insights workspace.
Note: Diagnostic settings are not created for Azure Communication Email Service because that resource type does not support diagnostics.

### API Management Routing

APIM base URL:

- https://apim-queuemaster-dev.azure-api.net

Configured APIs and operations (routing only, no APIM auth policies yet):

1. Order API path prefix: /orders
2. Payment API path prefix: /payments

Order routes through APIM:

- GET /orders/health
- GET /orders/api/orders
- GET /orders/api/orders/{id}
- POST /orders/api/orders
- PUT /orders/api/orders/{id}
- DELETE /orders/api/orders/{id}

Payment routes through APIM:

- GET /payments/health
- GET /payments/api/payments
- GET /payments/api/payments/{transactionId}
- GET /payments/api/payments/order/{orderId}
- POST /payments/api/payments
- PUT /payments/api/payments/{transactionId}
- DELETE /payments/api/payments/{transactionId}

Routing status:

1. APIM APIs and operations are deployed successfully.
2. End-to-end health checks currently depend on backend service availability and correct backend host URLs.
3. If APIM returns 500, validate backend service health endpoints first.

Example deployment command:

```bash
az deployment group create \
  --name qm-deploy-<timestamp> \
  --resource-group <your-rg> \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --mode Incremental
```

## Build and Verification

```bash
dotnet build QueueMaster.sln
```

## Troubleshooting

If build fails:

```bash
dotnet restore
dotnet build QueueMaster.sln
```

If a port is in use on Windows:

```powershell
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

Messaging issues:

1. Check unpublished outbox events in OrderService database.
2. Check Service Bus subscription dead-letter queue for poison messages.
3. Check ProcessedMessages table in PaymentService database to confirm duplicate detection state.
4. Review PaymentService logs for message processing exceptions.

## Status

Active development.

Current cloud status summary:

1. APIM foundation and routing operations are deployed.
2. JWT/CORS policies are intentionally deferred until backend routing validation is complete.
