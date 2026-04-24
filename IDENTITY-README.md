# QueueMaster Identity Setup (Microsoft Entra ID)

This guide explains how to configure Microsoft Entra ID (Azure AD) for QueueMaster APIs.

## 1) Create the API App Registration (Do This First)

Do this first because everything else (JWT audience, scopes, roles) depends on this app.

1. Go to Azure Portal -> Microsoft Entra ID -> App registrations -> New registration.
2. Name: `QueueMaster-Api`.
3. Supported account types: `Accounts in this organizational directory only (Single tenant)`.
4. Click `Register`.

After creation, note these values:
1. `Application (client) ID`
2. `Directory (tenant) ID`

Current values in this project:
1. Application (client) ID: `109ec5a0-c6bb-425f-ad2a-e532b14483b0`
2. Directory (tenant) ID: `d02d2542-ac01-461b-b2ee-1c0e87591daa`

## 2) Configure App Roles (Must Match API Code)

QueueMaster backend currently checks these role values:
1. `QueueMaster.User`
2. `QueueMaster.Admin`

Create them in `QueueMaster-Api`:
1. Open `App roles` -> `Create app role`.
2. Add role:
   - Display name: `User`
   - Allowed member types: `Users/Groups`
   - Value: `QueueMaster.User`
   - Description: `Standard user access`
   - Enabled: `Yes`
3. Add role:
   - Display name: `Admin`
   - Allowed member types: `Users/Groups`
   - Value: `QueueMaster.Admin`
   - Description: `Administrative access`
   - Enabled: `Yes`

## 3) Expose API Scope

1. Open `Expose an API`.
2. Click `Set` on Application ID URI (default is fine):
   - `api://109ec5a0-c6bb-425f-ad2a-e532b14483b0`
3. Click `Add a scope` and create:
   - Scope name: `access_as_user`
   - Who can consent: `Admins and users`
   - Admin consent display name: `Access QueueMaster API`
   - Admin consent description: `Allows the app to access QueueMaster API on behalf of the signed-in user.`
   - User consent display name: `Access QueueMaster API`
   - User consent description: `Allows the app to access QueueMaster API.`
   - State: `Enabled`

Important:
1. Do not create a scope named `QueueMaster-Client`.
2. `QueueMaster-Client` is the client app registration name, not an API scope.

## 4) Assign Roles to Users/Groups

1. Go to Microsoft Entra ID -> Enterprise applications.
2. Open enterprise app for `QueueMaster-Api`.
3. Go to `Users and groups` -> `Add user/group`.
4. Assign users/groups to `QueueMaster.User` or `QueueMaster.Admin` role.

Tip: assign groups instead of individual users.

## 5) Register a Client App (for frontend/Postman)

1. App registrations -> New registration.
2. Name: `QueueMaster-Client` (or your frontend app name).
3. Add redirect URI based on client type:
   - SPA example: `http://localhost:3000`
   - Postman callback: `https://oauth.pstmn.io/v1/callback`
4. Under `API permissions` -> `Add a permission` -> `My APIs` -> `QueueMaster-Api` -> select scope `access_as_user`.
5. Grant admin consent (if required by your tenant policy).
6. Under `Certificates & secrets` -> create a `Client secret` for confidential client flows.

Client secret note:
1. In Postman, use the secret `Value` as `Client Secret`.
2. Do not use `Secret ID` in Postman.
3. Save the secret value immediately when created; it is only shown once.

## 6) Postman OAuth2 Field Mapping (Recommended)

Use Postman Authorization tab with OAuth 2.0 and fill fields as below.

1. Type: `OAuth 2.0`
2. Grant Type: `Authorization Code`
3. Callback URL: `https://oauth.pstmn.io/v1/callback`
4. Auth URL:
   - `https://login.microsoftonline.com/d02d2542-ac01-461b-b2ee-1c0e87591daa/oauth2/v2.0/authorize`
5. Access Token URL:
   - `https://login.microsoftonline.com/d02d2542-ac01-461b-b2ee-1c0e87591daa/oauth2/v2.0/token`
6. Client ID:
   - `QueueMaster-Client` application (client) ID
7. Client Secret:
   - Client secret `Value` from `QueueMaster-Client` (not the `Secret ID`)
8. Scope:
   - `api://109ec5a0-c6bb-425f-ad2a-e532b14483b0/access_as_user openid profile offline_access`
9. Client Authentication:
   - `Send client credentials in body` (recommended for Postman with Entra v2)

After token is generated:
1. Open `access_token` in `jwt.ms` and verify claims.
2. Confirm `aud` is `api://109ec5a0-c6bb-425f-ad2a-e532b14483b0`.
3. Confirm `roles` contains `QueueMaster.Admin` or `QueueMaster.User`.
4. In Swagger Authorize, paste only the raw JWT (no `Bearer ` prefix).

## 7) Backend Configuration Already Wired

QueueMaster APIs are already configured to validate Entra JWT tokens using:
1. Tenant: `d02d2542-ac01-461b-b2ee-1c0e87591daa`
2. Audience: `api://109ec5a0-c6bb-425f-ad2a-e532b14483b0`
3. Roles checked: `QueueMaster.User`, `QueueMaster.Admin`
4. Accepted issuers:
   - `https://login.microsoftonline.com/{tenantId}/v2.0`
   - `https://sts.windows.net/{tenantId}/`
5. Role claim mapping is configured with `MapInboundClaims = false` so role checks use the JWT `roles` claim directly.

Token guidance:
1. Prefer acquiring v2 tokens (`/oauth2/v2.0/token`) for consistency.
2. v1 issuer tokens are still accepted by current API validation settings.

Code references:
1. `src/OrderService/Program.cs`
2. `src/PaymentService/Program.cs`

## 8) Validation Checklist

1. Call API without token -> should return `401 Unauthorized`.
2. Call API with valid token but no required role -> should return `403 Forbidden`.
3. Call GET endpoint with token containing `QueueMaster.User` or `QueueMaster.Admin` -> should succeed.
4. Call POST/PUT/DELETE with token containing `QueueMaster.Admin` -> should succeed.

## 9) Common Problems

1. `401` due to wrong audience:
   - Ensure token `aud` is `api://109ec5a0-c6bb-425f-ad2a-e532b14483b0`.
2. `403` due to missing role:
   - Ensure token has `roles` claim with `QueueMaster.User` or `QueueMaster.Admin`.
   - `403` means authentication succeeded, but authorization policy requirements were not met.
3. Role updates not reflected:
   - Sign out and sign in again to refresh token claims.
