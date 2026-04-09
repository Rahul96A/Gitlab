#!/usr/bin/env bash
set -euo pipefail

# ─── Entra ID (Azure AD) App Registration for GitLabClone ──────────────────
# Creates an app registration with:
#   - SPA redirect URIs (for frontend auth)
#   - API scope
#   - App roles (Admin, Maintainer, Developer, Reporter, Guest)
#
# Prerequisites: az login with Global Admin or App Admin role
#
# Usage:
#   chmod +x infra/setup-entra-id.sh
#   ./infra/setup-entra-id.sh
# ─────────────────────────────────────────────────────────────────────────────

APP_NAME="${APP_NAME:-GitLabClone}"
FRONTEND_URL="${FRONTEND_URL:-https://ca-gitlabclone-web.azurecontainerapps.io}"
API_URL="${API_URL:-https://ca-gitlabclone-api.azurecontainerapps.io}"

echo "==> Creating Entra ID App Registration: $APP_NAME"

# Create the app registration
APP_ID=$(az ad app create \
  --display-name "$APP_NAME" \
  --sign-in-audience "AzureADMyOrg" \
  --web-redirect-uris "$FRONTEND_URL" "$FRONTEND_URL/login" "http://localhost:5173" "http://localhost:5173/login" \
  --query appId -o tsv)

echo "    App (Client) ID: $APP_ID"

# Get the object ID
OBJ_ID=$(az ad app show --id "$APP_ID" --query id -o tsv)

# Create API scope
echo "==> Adding API scope: api://$APP_ID/access_as_user"
az ad app update --id "$APP_ID" \
  --identifier-uris "api://$APP_ID" \
  --set "api={\"oauth2PermissionScopes\":[{\"adminConsentDescription\":\"Access GitLabClone API\",\"adminConsentDisplayName\":\"Access API\",\"id\":\"$(uuidgen)\",\"isEnabled\":true,\"type\":\"User\",\"userConsentDescription\":\"Access GitLabClone API\",\"userConsentDisplayName\":\"Access API\",\"value\":\"access_as_user\"}]}"

# Add App Roles
echo "==> Adding App Roles..."
ROLES_JSON=$(cat <<'ROLES'
[
  {"allowedMemberTypes":["User"],"description":"Full system access","displayName":"Admin","id":"10000000-0000-0000-0000-000000000050","isEnabled":true,"value":"Admin"},
  {"allowedMemberTypes":["User"],"description":"Project management","displayName":"Maintainer","id":"10000000-0000-0000-0000-000000000040","isEnabled":true,"value":"Maintainer"},
  {"allowedMemberTypes":["User"],"description":"Code contribution","displayName":"Developer","id":"10000000-0000-0000-0000-000000000030","isEnabled":true,"value":"Developer"},
  {"allowedMemberTypes":["User"],"description":"Issue reporting","displayName":"Reporter","id":"10000000-0000-0000-0000-000000000020","isEnabled":true,"value":"Reporter"},
  {"allowedMemberTypes":["User"],"description":"Read-only access","displayName":"Guest","id":"10000000-0000-0000-0000-000000000010","isEnabled":true,"value":"Guest"}
]
ROLES
)

az ad app update --id "$APP_ID" --app-roles "$ROLES_JSON"

# Create service principal
echo "==> Creating Service Principal..."
SP_ID=$(az ad sp create --id "$APP_ID" --query id -o tsv 2>/dev/null || echo "already exists")
echo "    Service Principal: $SP_ID"

# Get tenant ID
TENANT_ID=$(az account show --query tenantId -o tsv)

echo ""
echo "==> Entra ID setup complete!"
echo ""
echo "    Tenant ID:     $TENANT_ID"
echo "    Client ID:     $APP_ID"
echo "    API Scope:     api://$APP_ID/access_as_user"
echo ""
echo "    Add to appsettings.json:"
echo "    {"
echo "      \"AzureAd\": {"
echo "        \"TenantId\": \"$TENANT_ID\","
echo "        \"ClientId\": \"$APP_ID\""
echo "      }"
echo "    }"
