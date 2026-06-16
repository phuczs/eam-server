<#
.SYNOPSIS  Zip the published API into a versioned, deployable artifact.
.DESCRIPTION
    Replaces the Docker image with a traditional .zip artifact — the enterprise,
    no-Docker equivalent. The name carries the commit SHA so every build is
    traceable and any previous artifact can be re-deployed for rollback.
.NOTES     Local: pwsh -File scripts/ci/package.ps1
#>
[CmdletBinding()]
param(
    [string]$PublishDir  = "publish/EAM.Api",
    [string]$ArtifactDir = "artifact"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $PublishDir)) {
    throw "Publish output '$PublishDir' not found. Run scripts/ci/build.ps1 first."
}

# Version = commit SHA in CI, timestamp when run locally.
$version = if ($env:CI_COMMIT_SHORT_SHA) { $env:CI_COMMIT_SHORT_SHA } else { Get-Date -Format "yyyyMMddHHmmss" }

New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null
$zip = Join-Path $ArtifactDir "EAM.Api-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }

Write-Host "==> Packaging $PublishDir -> $zip"
Compress-Archive -Path (Join-Path $PublishDir '*') -DestinationPath $zip -Force

$sizeMb = [math]::Round((Get-Item $zip).Length / 1MB, 2)
Write-Host "==> Artifact ready: $zip ($sizeMb MB)"
