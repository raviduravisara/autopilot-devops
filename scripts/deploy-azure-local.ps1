# Local Azure deployment - mirrors .github/workflows/deploy-azure.yml.
# Used because the university AAD tenant blocks service principal creation,
# so GitHub Actions cannot log in to Azure; we deploy with the developer's
# own az login session instead.
#
# Usage (from repo root):
#   .\scripts\deploy-azure-local.ps1 -GhcrToken "ghp_xxx" -JwtSigningKey "long-random-key"

param(
    [string]$ImageTag = "latest",
    [Parameter(Mandatory = $true)][string]$GhcrToken,
    [Parameter(Mandatory = $true)][string]$JwtSigningKey
)

$ErrorActionPreference = "Stop"

$ResourceGroup = "autopilot-rg"
$Location      = "southeastasia"
$AcaEnv        = "autopilot-rv-env"
$BackendApp    = "autopilot-backend"
$FrontendApp   = "autopilot-frontend"
$AiApp         = "autopilot-ai"
$GhcrUser      = "raviduravisara"
$JwtIssuer     = "autopilot-prod"
$JwtAudience   = "autopilot-clients"

$BackendImage  = "ghcr.io/$GhcrUser/autopilot-backend:$ImageTag"
$FrontendImage = "ghcr.io/$GhcrUser/autopilot-frontend:$ImageTag"
$AiImage       = "ghcr.io/$GhcrUser/autopilot-ai-service:$ImageTag"

az config set extension.use_dynamic_install=yes_without_prompt | Out-Null

Write-Host "Reading Postgres connection string from Terraform output..."
$PostgresConn = terraform -chdir=infra/terraform/azure output -raw postgres_connection_string
if (-not $PostgresConn) { throw "Could not read postgres_connection_string from terraform output." }

Write-Host "Deploying AI service..."
az containerapp up `
    --name $AiApp `
    --resource-group $ResourceGroup `
    --environment $AcaEnv `
    --image $AiImage `
    --ingress external `
    --target-port 8000 `
    --registry-server ghcr.io `
    --registry-username $GhcrUser `
    --registry-password $GhcrToken

$AiFqdn = az containerapp show --name $AiApp --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv

Write-Host "Deploying backend..."
az containerapp up `
    --name $BackendApp `
    --resource-group $ResourceGroup `
    --environment $AcaEnv `
    --image $BackendImage `
    --ingress external `
    --target-port 8080 `
    --registry-server ghcr.io `
    --registry-username $GhcrUser `
    --registry-password $GhcrToken

Write-Host "Configuring backend secrets and environment..."
az containerapp secret set `
    --name $BackendApp `
    --resource-group $ResourceGroup `
    --secrets postgres-conn="$PostgresConn" jwt-signing-key="$JwtSigningKey" | Out-Null

az containerapp update `
    --name $BackendApp `
    --resource-group $ResourceGroup `
    --set-env-vars `
        ASPNETCORE_ENVIRONMENT=Production `
        ASPNETCORE_URLS=http://+:8080 `
        ConnectionStrings__DefaultConnection=secretref:postgres-conn `
        Jwt__Issuer=$JwtIssuer `
        Jwt__Audience=$JwtAudience `
        Jwt__SigningKey=secretref:jwt-signing-key `
        Jwt__ExpiresMinutes=60 | Out-Null

$BackendFqdn = az containerapp show --name $BackendApp --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv

Write-Host "Deploying frontend..."
az containerapp up `
    --name $FrontendApp `
    --resource-group $ResourceGroup `
    --environment $AcaEnv `
    --image $FrontendImage `
    --ingress external `
    --target-port 80 `
    --registry-server ghcr.io `
    --registry-username $GhcrUser `
    --registry-password $GhcrToken

Write-Host "Injecting runtime config into frontend..."
az containerapp update `
    --name $FrontendApp `
    --resource-group $ResourceGroup `
    --set-env-vars API_BASE_URL="https://$BackendFqdn" AI_BASE_URL="https://$AiFqdn" | Out-Null

$FrontendFqdn = az containerapp show --name $FrontendApp --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv

Write-Host "Allowing frontend origin in backend CORS..."
az containerapp update `
    --name $BackendApp `
    --resource-group $ResourceGroup `
    --set-env-vars "Cors__AllowedOrigins__0=https://$FrontendFqdn" | Out-Null

Write-Host ""
Write-Host "=================================================="
Write-Host "Frontend: https://$FrontendFqdn"
Write-Host "Backend:  https://$BackendFqdn"
Write-Host "AI:       https://$AiFqdn"
Write-Host "=================================================="
