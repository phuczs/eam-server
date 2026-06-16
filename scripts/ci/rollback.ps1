<#
.SYNOPSIS  Roll the staging deployment back to a previous artifact / backup.
.DESCRIPTION
    The no-Docker rollback: re-expand a known-good .zip over the target. By default
    it restores the most recent backup taken by deploy-staging.ps1; pass -BackupZip
    to restore a specific one (e.g. an older artifact/EAM.Api-<sha>.zip).
.NOTES     Local: pwsh -File scripts/ci/rollback.ps1
           Specific: pwsh -File scripts/ci/rollback.ps1 -BackupZip artifact\EAM.Api-abc1234.zip
#>
[CmdletBinding()]
param(
    [string]$TargetPath  = $env:STAGING_TARGET_PATH,
    [string]$BackupZip,                                   # default = latest in deploy-backup/
    [string]$AppPool     = $env:STAGING_APP_POOL,
    [string]$ServiceName = $env:STAGING_SERVICE_NAME
)

$ErrorActionPreference = "Stop"
if (-not $TargetPath) { $TargetPath = "C:\inetpub\eam-staging" }

if (-not $BackupZip) {
    $latest = Get-ChildItem "deploy-backup\backup-*.zip" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latest) { throw "No backup found in deploy-backup/. Pass -BackupZip explicitly." }
    $BackupZip = $latest.FullName
}
if (-not (Test-Path $BackupZip)) { throw "Backup '$BackupZip' not found." }

Write-Host "==> Rolling back '$TargetPath' <- '$BackupZip'"

if ($AppPool -and (Get-Command Stop-WebAppPool -ErrorAction SilentlyContinue)) {
    Stop-WebAppPool -Name $AppPool -ErrorAction SilentlyContinue
}
if ($ServiceName -and (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 2

if (Test-Path $TargetPath) {
    Get-ChildItem $TargetPath -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Force -Path $TargetPath | Out-Null
Expand-Archive -Path $BackupZip -DestinationPath $TargetPath -Force

if ($ServiceName -and (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
    Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
}
if ($AppPool -and (Get-Command Start-WebAppPool -ErrorAction SilentlyContinue)) {
    Start-WebAppPool -Name $AppPool -ErrorAction SilentlyContinue
}

Write-Host "==> Rollback complete -> $TargetPath"
