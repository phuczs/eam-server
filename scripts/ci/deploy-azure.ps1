<#
.SYNOPSIS  Deploy the packaged API .zip to an Azure App Service (zip deploy, no Docker).
.DESCRIPTION
    Stage `deploy` — Azure target. Logs in with a Service Principal, then pushes the
    artifact via `az webapp deploy --type zip` (Kudu OneDeploy). No container image.

    Required CI/CD Variables (Settings -> CI/CD -> Variables):
      AZ_CLIENT_ID, AZ_CLIENT_SECRET (masked), AZ_TENANT_ID  -> Service Principal
      AZ_RG        -> resource group of the App Service
      AZ_APP_NAME  -> App Service name
    Prereq on the runner: Azure CLI (`az`).  winget install Microsoft.AzureCLI

    IMPORTANT: set runtime config (Jwt / ConnectionStrings / AzureAd / Singpass /
    Storage / Cors) as App Service "Application settings" BEFORE deploying, or the
    app fails to start (e.g. "Jwt:SigningKey is required").
.NOTES  Local: pwsh -File scripts/ci/deploy-azure.ps1 -PackagePath artifact\EAM.Api-<sha>.zip
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [string]$ClientId      = $env:AZ_CLIENT_ID,
    [string]$ClientSecret  = $env:AZ_CLIENT_SECRET,
    [string]$TenantId      = $env:AZ_TENANT_ID,
    [string]$ResourceGroup = $env:AZ_RG,
    [string]$AppName       = $env:AZ_APP_NAME
)

$ErrorActionPreference = "Stop"

if (-not $ClientId)      { throw "AZ_CLIENT_ID is required." }
if (-not $ClientSecret)  { throw "AZ_CLIENT_SECRET is required." }
if (-not $TenantId)      { throw "AZ_TENANT_ID is required." }
if (-not $ResourceGroup) { throw "AZ_RG is required." }
if (-not $AppName)       { throw "AZ_APP_NAME is required." }
if (-not (Test-Path $PackagePath)) { throw "Package '$PackagePath' not found. Run scripts/ci/package.ps1 first." }
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI ('az') not found on the runner. Install it: winget install Microsoft.AzureCLI"
}

Write-Host "==> Azure CLI: $((az version --query '\"azure-cli\"' -o tsv))"
Write-Host "==> az login (service principal, tenant $TenantId)"
az login --service-principal -u $ClientId -p $ClientSecret --tenant $TenantId | Out-Null
if ($LASTEXITCODE -ne 0) { throw "az login failed ($LASTEXITCODE)." }

try {
    Write-Host "==> Deploying '$PackagePath' -> App Service '$AppName' (rg: $ResourceGroup)"
    az webapp deploy --resource-group $ResourceGroup --name $AppName --type zip --src-path $PackagePath
    if ($LASTEXITCODE -ne 0) { throw "az webapp deploy failed ($LASTEXITCODE)." }
    Write-Host "==> Deploy complete -> https://$AppName.azurewebsites.net"
}
finally {
    az logout
}
