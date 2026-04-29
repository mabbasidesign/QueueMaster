# QueueMaster Azure Container Apps Guide

This document explains how QueueMaster backend APIs are being deployed to Azure Container Apps (ACA), how the infrastructure is wired in Bicep, and what the current deployment status is.

## Components

1. Azure Container Registry (ACR): stores container images for OrderService and PaymentService
2. Azure Container Apps Environment: shared runtime environment for backend APIs
3. OrderService Container App: public HTTP API
4. PaymentService Container App: public HTTP API
5. NotificationFunction: remains deployed as Azure Functions and is not moved to ACA

## Current Architecture

```text
ACR (acrqueuemasterdev.azurecr.io)
    -> stores images
    -> orderservice:v1
    -> paymentservice:v1 (pending)

ACA Environment (cae-queuemaster-dev)
    -> OrderService Container App
    -> PaymentService Container App (pending)

APIM
    -> /orders/** forwards to OrderService backend
    -> /payments/** forwards to PaymentService backend
```

## Infrastructure Files

ACA-related infrastructure is now modularized under `infra`:

1. `infra/acr.bicep`
2. `infra/containerapps-env.bicep`
3. `infra/orderservice-aca.bicep`
4. `infra/paymentservice-aca.bicep`
5. `infra/main.bicep`
6. `infra/main.bicepparam`

## Deployment Flags

The root template uses deployment flags so ACA can be rolled out one slice at a time.

Current flags in `infra/main.bicep`:

1. `deployContainerAppsBase`
   - Creates ACR and ACA environment
2. `deployOrderServiceContainerApp`
   - Creates OrderService Container App
3. `deployPaymentServiceContainerApp`
   - Creates PaymentService Container App

This allows incremental rollout without forcing all services to deploy at once.

## Container Registry

Current ACR settings:

1. Name: `acrqueuemasterdev`
2. Login server: `acrqueuemasterdev.azurecr.io`
3. SKU: `Basic`

Image publishing approach:

1. Images are built remotely with `az acr build`
2. Docker Desktop is not required for this workflow
3. Source is uploaded to ACR build service and built there

Example OrderService image build:

```powershell
az acr build \
  --registry acrqueuemasterdev \
  --image orderservice:v1 \
  --file src/OrderService/Dockerfile \
  .
```

## Container Apps Environment

Current ACA environment:

1. Name: `cae-queuemaster-dev`
2. Location: `canadaeast`

Important note:

1. The ACA environment is the shared host/runtime boundary
2. Each API is still deployed as its own `Microsoft.App/containerApps` resource
3. In Azure Portal UX, container apps appear under the environment, but they are separate resources attached through `managedEnvironmentId`

## OrderService Deployment

### Container Packaging

OrderService containerization files:

1. `src/OrderService/Dockerfile`
2. `.dockerignore`

Container behavior:

1. Base image: `mcr.microsoft.com/dotnet/aspnet:9.0`
2. Exposed port: `8080`
3. `ASPNETCORE_URLS=http://+:8080`

### Current Deployment Status

OrderService Container App has been deployed successfully.

Resource details:

1. Container App name: `ca-orderservice-dev`
2. Public URL: `https://ca-orderservice-dev.politetree-44a10582.canadaeast.azurecontainerapps.io`
3. Provisioning state: `Succeeded`
4. Image: `acrqueuemasterdev.azurecr.io/orderservice:v1`

### Current Validation Results

Observed endpoint behavior:

1. `GET /health` -> `200`
2. `GET /api/orders` -> `401`

Interpretation:

1. The container app is reachable publicly
2. The app is listening on the expected port
3. Authentication is enforced correctly on protected endpoints

### Temporary SQL Secret Note

ACA rejects empty secret values.

Because `orderServiceSqlConnectionString` was empty during first deployment attempt, ACA validation failed with:

1. `ContainerAppSecretInvalid`

To complete the initial deployment, a temporary non-empty placeholder SQL connection string was supplied.

This means:

1. The OrderService ACA resource is created and reachable
2. You should replace the placeholder SQL connection string with the real cloud database connection string before treating this deployment as final

## PaymentService Deployment

PaymentService ACA infrastructure is scaffolded but not yet deployed.

Prepared items:

1. `infra/paymentservice-aca.bicep`
2. Deployment flag in `infra/main.bicep`
3. Image tag parameter in `infra/main.bicepparam`

Pending work:

1. Create PaymentService Dockerfile if needed
2. Build `paymentservice:v1` in ACR
3. Deploy PaymentService Container App
4. Validate `/health`
5. Validate `/api/payments` expected auth behavior

## NotificationFunction Status

NotificationFunction remains on Azure Functions.

Current design decision:

1. OrderService and PaymentService move to ACA
2. NotificationFunction stays on Azure Functions isolated worker
3. Service Bus remains the event backbone between services

## APIM Relationship

APIM is already provisioned and API operations are already defined.

Current state:

1. APIM routing exists for OrderService and PaymentService
2. APIM backend URLs still need to be updated to ACA URLs as services are finalized
3. JWT and CORS policies are intentionally deferred until routing and backend readiness are fully validated

Recommended next step after each ACA deployment:

1. Update `orderServiceBackendUrl` or `paymentServiceBackendUrl`
2. Redeploy `infra/main.bicep`
3. Re-test APIM `/orders/health` and `/payments/health`

## Configuration Requirements

Each ACA service currently needs these categories of settings:

1. SQL connection string
2. Service Bus connection string
3. Application Insights connection string
4. Entra tenant ID
5. Entra audience

Examples of parameters already wired in Bicep:

1. `orderServiceSqlConnectionString`
2. `paymentServiceSqlConnectionString`
3. `authTenantId`
4. `authAudience`
5. `orderServiceImageTag`
6. `paymentServiceImageTag`

## Deployment Flow

Recommended rollout sequence:

1. Deploy ACR and ACA environment
2. Build OrderService image in ACR
3. Deploy OrderService Container App
4. Validate OrderService public URL and endpoints
5. Update APIM backend URL for OrderService
6. Build PaymentService image in ACR
7. Deploy PaymentService Container App
8. Validate PaymentService public URL and endpoints
9. Update APIM backend URL for PaymentService

## Example Commands

### Preview base ACA infrastructure

```powershell
az deployment group what-if \
  --name qm-aca-base-preview \
  --resource-group rg-queuemaster-dev \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam deployContainerAppsBase=true deployOrderServiceContainerApp=false deployPaymentServiceContainerApp=false
```

### Deploy base ACA infrastructure

```powershell
az deployment group create \
  --name qm-aca-base-create \
  --resource-group rg-queuemaster-dev \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam deployContainerAppsBase=true deployOrderServiceContainerApp=false deployPaymentServiceContainerApp=false \
  --mode Incremental
```

### Deploy OrderService ACA

```powershell
az deployment group create \
  --name qm-orderservice-aca-create \
  --resource-group rg-queuemaster-dev \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam deployContainerAppsBase=true deployOrderServiceContainerApp=true deployPaymentServiceContainerApp=false \
  --mode Incremental
```

### Validate OrderService ACA

```powershell
curl.exe -s -o NUL -w "health:%{http_code}\n" "https://ca-orderservice-dev.politetree-44a10582.canadaeast.azurecontainerapps.io/health"
curl.exe -s -o NUL -w "orders:%{http_code}\n" "https://ca-orderservice-dev.politetree-44a10582.canadaeast.azurecontainerapps.io/api/orders"
```

## Known Issues and Notes

1. Empty ACA secrets are not allowed. Secure parameters must have non-empty values.
2. Health endpoints depend on real database connectivity because the APIs call `Database.CanConnect()`.
3. Protected endpoints returning `401` is expected when no valid JWT is supplied.
4. Current Bicep warnings (`BCP318`) come from conditionally deployed modules and should be cleaned up later for stricter validation hygiene.

## Current Status Summary

1. ACR deployed: yes
2. ACA environment deployed: yes
3. OrderService image built and pushed: yes
4. OrderService Container App deployed: yes
5. PaymentService image built and pushed: no
6. PaymentService Container App deployed: no
7. APIM backend switched to ACA URLs: not yet

## Next Steps

1. Replace the placeholder OrderService SQL connection string with the real cloud value
2. Update APIM `orderServiceBackendUrl` to the ACA URL
3. Build and deploy PaymentService to ACA
4. Update APIM `paymentServiceBackendUrl`
5. Re-test APIM routing end-to-end