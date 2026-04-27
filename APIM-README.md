# Azure API Management (APIM) Integration Plan

This document captures the plan for adding Azure API Management in front of OrderService and PaymentService.

## Why APIM

- Single entry point for all API consumers — no direct service URLs exposed
- Centralized JWT validation (Entra ID) — offload auth from individual services
- Rate limiting, throttling, and quota enforcement
- CORS policy in one place
- API versioning, developer portal, and subscription key management
- Observability: request/response logs, metrics, and Application Insights integration

## Architecture After APIM

```
Client
  └── APIM Gateway (apim-queuemaster-<env>.azure-api.net)
        ├── /orders/**   → OrderService  (App Service / Container App)
        └── /payments/** → PaymentService (App Service / Container App)
```

NotificationFunction continues to consume from Service Bus directly — APIM is not placed in front of it.

## Entra ID Details

Already provisioned — used for backend JWT validation in APIM policy:

- Tenant ID: `d02d2542-ac01-461b-b2ee-1c0e87591daa`
- App (audience): `api://109ec5a0-c6bb-425f-ad2a-e532b14483b0`
- Issuer (v2): `https://login.microsoftonline.com/d02d2542-ac01-461b-b2ee-1c0e87591daa/v2.0`
- Issuer (v1): `https://sts.windows.net/d02d2542-ac01-461b-b2ee-1c0e87591daa/`

## Implementation Steps

### Phase 1 — Bicep Infrastructure

#### Step 1: Create `infra/apim.bicep`

Resource: `Microsoft.ApiManagement/service`

Key parameters:
- `apimName` — e.g. `apim-queuemaster-dev`
- `publisherEmail` — required by APIM
- `publisherName` — display name
- `sku` — `Developer` for dev/test, `Standard` or `Premium` for production
- `location`
- `appInsightsConnectionString` + `appInsightsId` — wire to existing App Insights

Outputs:
- `gatewayUrl` — e.g. `https://apim-queuemaster-dev.azure-api.net`
- `developerPortalUrl`

#### Step 2: Update `infra/main.bicep`

Add a module block:

```bicep
module apim 'apim.bicep' = {
  params: {
    location: location
    environmentName: environmentName
    apimName: 'apim-queuemaster-${environmentName}'
    publisherEmail: apimPublisherEmail
    publisherName: apimPublisherName
    appInsightsConnectionString: appinsights.outputs.connectionString
    appInsightsId: appinsights.outputs.id
    orderServiceBackendUrl: orderServiceBackendUrl
    paymentServiceBackendUrl: paymentServiceBackendUrl
  }
}
```

Add new params to `main.bicep`:
- `apimPublisherEmail`
- `apimPublisherName`
- `orderServiceBackendUrl`
- `paymentServiceBackendUrl`

#### Step 3: Update `infra/main.bicepparam`

Add values for the new params above.

---

### Phase 2 — API Definitions Inside APIM

#### Step 4: Import OrderService API

Inside `apim.bicep`, define:
- `Microsoft.ApiManagement/service/apis` resource named `order-api`
- Path prefix: `orders`
- Operations matching: `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders`, `PUT /api/orders/{id}`, `DELETE /api/orders/{id}`
- Backend URL pointing to OrderService host

#### Step 5: Import PaymentService API

Same pattern as above:
- API named `payment-api`
- Path prefix: `payments`
- Operations matching PaymentService endpoints
- Backend URL pointing to PaymentService host

---

### Phase 3 — Policies

#### Step 6: Global Inbound Policy (all APIs)

Apply at the APIM service or product level:

```xml
<inbound>
  <validate-jwt header-name="Authorization" failed-validation-httpcode="401" require-expiration-time="true">
    <openid-config url="https://login.microsoftonline.com/d02d2542-ac01-461b-b2ee-1c0e87591daa/v2.0/.well-known/openid-configuration" />
    <audiences>
      <audience>api://109ec5a0-c6bb-425f-ad2a-e532b14483b0</audience>
    </audiences>
    <issuers>
      <issuer>https://login.microsoftonline.com/d02d2542-ac01-461b-b2ee-1c0e87591daa/v2.0</issuer>
      <issuer>https://sts.windows.net/d02d2542-ac01-461b-b2ee-1c0e87591daa/</issuer>
    </issuers>
  </validate-jwt>
  <cors>
    <allowed-origins>
      <origin>*</origin>
    </allowed-origins>
    <allowed-methods>
      <method>*</method>
    </allowed-methods>
    <allowed-headers>
      <header>*</header>
    </allowed-headers>
  </cors>
</inbound>
```

Tighten CORS `allowed-origins` before production — replace `*` with actual frontend domain(s).

#### Step 7: Rate Limiting (optional but recommended)

Add to inbound policy per product or API:

```xml
<rate-limit calls="100" renewal-period="60" />
```

Adjust values based on expected traffic.

#### Step 8: Health Probe Forwarding

Map `GET /health` at the APIM level or allow the load balancer/health check to bypass APIM and hit the backend `/health` directly. Do not apply `validate-jwt` to the health endpoint.

---

### Phase 4 — Local / Pre-Deploy Validation

#### Step 9: What-If Before Deploy

```bash
az deployment group what-if \
  --resource-group rg-queuemaster-dev \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam
```

APIM Developer SKU takes ~30–45 minutes to provision — plan accordingly.

#### Step 10: Deploy

```bash
az deployment group create \
  --name qm-apim-<timestamp> \
  --resource-group rg-queuemaster-dev \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --mode Incremental
```

---

### Phase 5 — Post-Deploy Verification

- Hit `https://apim-queuemaster-dev.azure-api.net/orders` with a valid Bearer token — expect `200`
- Hit without token — expect `401`
- Check Application Insights for APIM request traces
- Open APIM Developer Portal and verify API definitions appear

---

## SKU Comparison

| SKU | Use Case | Provisioning Time | Cost |
|---|---|---|---|
| Developer | Dev/test only, no SLA | ~30–45 min | Low |
| Basic | Low-traffic production | ~30 min | Medium |
| Standard | Production with scaling | ~30 min | Higher |
| Premium | Multi-region, VNet | ~30–45 min | High |

Use **Developer** for this environment. Switch to **Standard** or **Premium** before any production go-live.

---

## Notes

- Backend services (OrderService, PaymentService) still validate JWT independently as a defense-in-depth measure. APIM validation is the first line of defense; service-level checks catch anything that bypasses APIM.
- APIM Managed Identity should be used for any calls APIM makes to backends that require Azure AD authentication — avoids embedding credentials in policy XML.
- If services are moved behind a VNet, APIM will need to be in the same VNet or use a private endpoint.
- Subscription keys (`Ocp-Apim-Subscription-Key`) can be used alongside JWT for internal service-to-service calls if needed.
