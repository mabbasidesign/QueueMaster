# QueueMaster SQL Guide

This document explains Azure SQL setup for QueueMaster services, with current focus on OrderService.

## Scope

1. Provision Azure SQL server and database for OrderService via Bicep
2. Feed SQL connection string into OrderService Container App deployment
3. Establish secure secret handling with Azure Key Vault
4. Prepare pattern for future service databases (for example PaymentService SQL)

## Current SQL Infrastructure

SQL resources are defined in:

1. infra/orderservice-sql.bicep
2. infra/main.bicep
3. infra/main.bicepparam

Current module behavior in infra/orderservice-sql.bicep:

1. Creates SQL logical server
2. Creates OrderService database
3. Optionally creates `AllowAzureServices` firewall rule (`0.0.0.0`)

Current Key Vault behavior in infra/keyvault.bicep:

1. Creates Key Vault with RBAC authorization enabled
2. Keeps purge protection enabled
3. Can create OrderService SQL secrets when enabled
4. Exposes vault URI and vault resource details as outputs

## Key Deployment Parameters

Primary SQL flags and parameters in infra/main.bicep:

1. deployOrderServiceSql
2. orderServiceSqlServerName
3. orderServiceSqlDatabaseName
4. orderServiceSqlAdminLogin
5. orderServiceSqlAdminPassword
6. orderServiceSqlAllowAzureServices
7. orderServiceSqlConnectionString

Key Vault-related parameters in infra/main.bicep:

1. deployKeyVault
2. keyVaultName
3. keyVaultTenantId
4. createOrderServiceSqlSecretsInKeyVault

In main.bicepparam these are environment-variable driven.

## Connection String Flow

Current flow is:

1. If `deployOrderServiceSql=true`, main.bicep composes a SQL connection string using SQL server output and SQL admin credentials
2. That effective connection string is passed to `orderservice-aca.bicep`
3. Container App secret `db-connection-string` is created from that value
4. App setting `ConnectionStrings__DefaultConnection` references that secret

## Key Vault Recommendation

For production-grade secret handling, store credentials and connection strings in Key Vault.

Recommended pattern:

1. Store SQL admin password in Key Vault
2. Store final SQL connection string in Key Vault
3. Grant read access to deployment identity and/or workload identity
4. Reference Key Vault secrets during deployment instead of passing plaintext values

Current project secret names:

1. orderservice-sql-admin-password
2. orderservice-sql-connection-string

Current Key Vault target:

1. Name: kv-queuemaster-dev
2. URI: https://kv-queuemaster-dev.vault.azure.net/

## Deploy Key Vault Only

Use this command when to provision only Key Vault without touching unrelated resources:

```bash
az deployment group create \
   --name qm-keyvault-create-<timestamp> \
   --resource-group rg-queuemaster-dev \
   --template-file infra/keyvault.bicep \
   --parameters location=canadaeast keyVaultName=kv-queuemaster-dev tenantId=<tenant-id> createOrderServiceSqlSecrets=false \
   --mode Incremental
```

## Store SQL Secrets in Key Vault

Store secrets with Azure CLI:

```bash
az keyvault secret set --vault-name kv-queuemaster-dev --name orderservice-sql-admin-password --value "<sql-admin-password>"
az keyvault secret set --vault-name kv-queuemaster-dev --name orderservice-sql-connection-string --value "<sql-connection-string>"
```

Verify secrets exist:

```bash
az keyvault secret show --vault-name kv-queuemaster-dev --name orderservice-sql-admin-password --query id -o tsv
az keyvault secret show --vault-name kv-queuemaster-dev --name orderservice-sql-connection-string --query id -o tsv
```

## SQL Identity and Database User Model

Recommended model by stage:

1. Initial rollout: SQL login authentication (already supported by current Bicep wiring)
2. Hardened state: Microsoft Entra authentication with managed identity for workloads

Why this progression:

1. SQL login is simpler for first deployment and migration bootstrapping
2. Managed identity removes password rotation burden from application runtime
3. Entra users in SQL provide cleaner least-privilege access control

## SQL Login and App User Setup (Current Path)

Current infrastructure creates SQL server admin login and password.

After database creation, create a least-privilege app user in OrderService database:

```sql
-- Connect to QueueMasterOrderService database as SQL admin
CREATE USER [orderservice_app] WITH PASSWORD = 'StrongPasswordHere';

ALTER ROLE db_datareader ADD MEMBER [orderservice_app];
ALTER ROLE db_datawriter ADD MEMBER [orderservice_app];
ALTER ROLE db_ddladmin ADD MEMBER [orderservice_app];
```

Notes:

1. `db_ddladmin` is included only to simplify EF migration execution.
2. For stricter runtime posture, use a separate migration identity and remove `db_ddladmin` from app runtime user.

## Managed Identity + Entra User Setup (Target Path)

For production hardening, move to managed identity with Entra-based SQL users.

High-level steps:

1. Enable system-assigned or user-assigned managed identity on Container App
2. Configure Azure SQL server Entra admin
3. Connect to database with Entra admin and create user from external provider
4. Grant least-privilege roles

Example SQL (run as Entra admin on target database):

```sql
CREATE USER [ca-orderservice-dev] FROM EXTERNAL PROVIDER;

ALTER ROLE db_datareader ADD MEMBER [ca-orderservice-dev];
ALTER ROLE db_datawriter ADD MEMBER [ca-orderservice-dev];
-- Add schema/migration role only when needed for migrations
```

## Runtime and Migration Identity Split (Recommended)

Use separate principals:

1. Runtime app identity: `db_datareader` + `db_datawriter`
2. Migration identity: temporary elevated role (for schema changes), used only during deployment pipeline

Benefits:

1. Reduces blast radius of application credentials/identity
2. Keeps schema-change permissions out of continuous runtime path
3. Aligns with least-privilege and audit best practices

## Practical Secure Flow

1. Provision Key Vault
2. Add secret: `orderservice-sql-admin-password`
3. Add secret: `orderservice-sql-connection-string`
4. Configure deployment to resolve secrets from Key Vault
5. Deploy SQL resources and OrderService ACA using resolved values

If want template-managed secret creation during deployment, set:

1. deployKeyVault=true
2. createOrderServiceSqlSecretsInKeyVault=true
3. Provide either:
   - orderServiceSqlAdminPassword and deployOrderServiceSql=true (template composes connection string), or
   - orderServiceSqlConnectionString directly

## Deployment Sequence

1. Deploy base container resources:
   - ACR
   - ACA environment
2. Deploy OrderService SQL resources:
   - SQL server
   - SQL database
3. Deploy or redeploy OrderService ACA with real SQL secret
4. Validate endpoints:
   - /health
   - /api/orders
5. Run EF Core migrations against Azure SQL

## Validation Checklist

1. SQL server exists and is in Succeeded state
2. SQL database exists and is in Succeeded state
3. OrderService ACA can connect to SQL
4. `/health` returns success
5. Protected endpoints still require auth token

## Future Service Pattern

As more services are added, keep one SQL module per service database.

Suggested structure:

1. infra/orderservice-sql.bicep
2. infra/paymentservice-sql.bicep
3. infra/<future-service>-sql.bicep

Use shared helper modules only for truly common pieces (for example tags, diagnostics, or networking primitives).

## Notes

1. `AllowAzureServices` firewall rule is useful for initial rollout but should be tightened later
2. Prefer private networking and managed identity access for long-term hardening
3. Avoid storing secrets in source control or static bicepparam files