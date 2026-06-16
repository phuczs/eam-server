<#
.SYNOPSIS  Deploy a packaged artifact to the staging host (IIS site or folder).
.DESCRIPTION
    No-Docker deploy = stop the app, back up the current copy (for rollback),
    expand the new .zip over the target, start the app again. Works for an IIS app
    pool, a Windows Service, or a plain folder — whichever is configured via env.

    Configure on the runner (CI/CD Variables or machine env):
      STAGING_TARGET_PATH   e.g. C:\inetpub\eam-staging   (default below)
      STAGING_APP_POOL      e.g. EamStaging                (optional — IIS)
      STAGING_SERVICE_NAME  e.g. EamApi                    (optional — Windows Service)
.NOTES     Local: pwsh -File scripts/ci/deploy-staging.ps1 -PackagePath artifact\EAM.Api-<sha>.zip
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [string]$TargetPath  = $env:STAGING_TARGET_PATH,
    [string]$AppPool     = $env:STAGING_APP_POOL,
    [string]$ServiceName = $env:STAGING_SERVICE_NAME
)

$ErrorActionPreference = "Stop"
if (-not $TargetPath) { $TargetPath = "C:\inetpub\eam-staging" }
if (-not (Test-Path $PackagePath)) { throw "Package '$PackagePath' not found." }

Write-Host "==> Deploying '$PackagePath' -> '$TargetPath'"

# 1) Stop the app so its files aren't locked.
if ($AppPool -and (Get-Command Stop-WebAppPool -ErrorAction SilentlyContinue)) {
    Write-Host "    stopping IIS app pool '$AppPool'"
    Stop-WebAppPool -Name $AppPool -ErrorAction SilentlyContinue
}
if ($ServiceName -and (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
    Write-Host "    stopping service '$ServiceName'"
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 2

# 2) Back up the current deployment so rollback.ps1 can restore it.
$backupRoot = "deploy-backup"
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
if ((Test-Path $TargetPath) -and (Get-ChildItem $TargetPath -Force -ErrorAction SilentlyContinue)) {
    $backup = Join-Path $backupRoot ("backup-{0}.zip" -f (Get-Date -Format "yyyyMMddHHmmss"))
    Write-Host "    backing up current deployment -> $backup"
    Compress-Archive -Path (Join-Path $TargetPath '*') -DestinationPath $backup -Force
}

# 3) Expand the new package over the target.
New-Item -ItemType Directory -Force -Path $TargetPath | Out-Null
Write-Host "    expanding package over target"
Expand-Archive -Path $PackagePath -DestinationPath $TargetPath -Force

# 4) Start the app again.
if ($ServiceName -and (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
    Write-Host "    starting service '$ServiceName'"
    Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
}
if ($AppPool -and (Get-Command Start-WebAppPool -ErrorAction SilentlyContinue)) {
    Write-Host "    starting IIS app pool '$AppPool'"
    Start-WebAppPool -Name $AppPool -ErrorAction SilentlyContinue
}

Write-Host "==> Deploy complete -> $TargetPath"
