<#
.SYNOPSIS
    Restore, compile, and publish the EAM Platform API (Release).
.DESCRIPTION
    Stage 1 of the pipeline. Runs on a Windows GitLab Runner (shell = pwsh) — no
    Docker required. Output is a framework-dependent publish folder that the
    `package` stage zips into the deployable artifact.
.NOTES
    Equivalent local command:  pwsh -File scripts/ci/build.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Solution      = "EAM.Platform.sln",
    [string]$ApiProject    = "src/EAM.Api/EAM.Api.csproj",
    [string]$PublishDir    = "publish/EAM.Api"
)

$ErrorActionPreference = "Stop"

Write-Host "==> dotnet SDK: $(dotnet --version)"

Write-Host "==> Restoring $Solution"
dotnet restore $Solution
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed ($LASTEXITCODE)." }

Write-Host "==> Building $Solution ($Configuration)"
dotnet build $Solution -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)." }

Write-Host "==> Publishing $ApiProject -> $PublishDir"
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
dotnet publish $ApiProject -c $Configuration -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }

Write-Host "==> Build OK. Published to $PublishDir"
