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
- main.bicep
- main.bicepparam

The current templates provision diagnostics settings for Service Bus and Communication/Email resources into Log Analytics via the Application Insights workspace.

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

## Status

Active development.
