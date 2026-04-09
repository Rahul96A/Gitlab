#!/usr/bin/env bash
set -euo pipefail

# ─── GitLabClone Deployment Script ──────────────────────────────────────────
# Prerequisites:
#   - Azure CLI (az) installed and logged in
#   - Docker installed (for image builds)
#   - Bicep CLI (bundled with az cli >= 2.20)
#
# Usage:
#   chmod +x infra/deploy.sh
#   ./infra/deploy.sh
# ─────────────────────────────────────────────────────────────────────────────

APP_NAME="${APP_NAME:-gitlabclone}"
LOCATION="${LOCATION:-eastus2}"
RG_NAME="rg-${APP_NAME}"
ACR_NAME=$(echo "acr${APP_NAME}" | tr -d '-')
API_IMAGE="gitlabclone-api"
WEB_IMAGE="gitlabclone-web"
TAG="${IMAGE_TAG:-$(git rev-parse --short HEAD 2>/dev/null || echo latest)}"

echo "==> Deploying GitLabClone to Azure"
echo "    App Name:  $APP_NAME"
echo "    Location:  $LOCATION"
echo "    Image Tag: $TAG"
echo ""

# ─── Step 1: Deploy infrastructure ──────────────────────────────────────────
echo "==> Step 1: Deploying Azure infrastructure via Bicep..."
az deployment sub create \
  --location "$LOCATION" \
  --template-file infra/main.bicep \
  --parameters appName="$APP_NAME" location="$LOCATION" \
    apiImageTag="$TAG" frontendImageTag="$TAG" \
  --query "properties.outputs" -o json

ACR_LOGIN_SERVER=$(az acr show -n "$ACR_NAME" --query loginServer -o tsv)
echo "    ACR: $ACR_LOGIN_SERVER"

# ─── Step 2: Build and push Docker images ───────────────────────────────────
echo "==> Step 2: Building and pushing Docker images..."
az acr login -n "$ACR_NAME"

echo "    Building API image..."
docker build -t "$ACR_LOGIN_SERVER/$API_IMAGE:$TAG" -f src/backend/Dockerfile src/backend/
docker push "$ACR_LOGIN_SERVER/$API_IMAGE:$TAG"

echo "    Building Frontend image..."
docker build -t "$ACR_LOGIN_SERVER/$WEB_IMAGE:$TAG" -f src/frontend/Dockerfile src/frontend/
docker push "$ACR_LOGIN_SERVER/$WEB_IMAGE:$TAG"

# ─── Step 3: Update Container Apps with new images ──────────────────────────
echo "==> Step 3: Updating Container Apps..."
az containerapp update \
  -n "ca-${APP_NAME}-api" -g "$RG_NAME" \
  --image "$ACR_LOGIN_SERVER/$API_IMAGE:$TAG"

az containerapp update \
  -n "ca-${APP_NAME}-web" -g "$RG_NAME" \
  --image "$ACR_LOGIN_SERVER/$WEB_IMAGE:$TAG"

# ─── Step 4: Run EF Core migrations ─────────────────────────────────────────
echo "==> Step 4: Running database migrations..."
# Exec into the running container to apply migrations
az containerapp exec \
  -n "ca-${APP_NAME}-api" -g "$RG_NAME" \
  --command "dotnet GitLabClone.Api.dll --migrate" 2>/dev/null || \
echo "    (Migration via exec not available — apply manually or add auto-migrate on startup)"

# ─── Done ────────────────────────────────────────────────────────────────────
API_FQDN=$(az containerapp show -n "ca-${APP_NAME}-api" -g "$RG_NAME" --query "properties.configuration.ingress.fqdn" -o tsv)
WEB_FQDN=$(az containerapp show -n "ca-${APP_NAME}-web" -g "$RG_NAME" --query "properties.configuration.ingress.fqdn" -o tsv)

echo ""
echo "==> Deployment complete!"
echo "    API:      https://$API_FQDN"
echo "    Frontend: https://$WEB_FQDN"
echo "    Swagger:  https://$API_FQDN/swagger"
echo "    Health:   https://$API_FQDN/health"
