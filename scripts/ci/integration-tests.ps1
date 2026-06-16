<#
.SYNOPSIS  Run the EAM Platform INTEGRATION tests and emit a JUnit report.
.NOTES     Filters by the EAM.Tests.Integration namespace. The tests boot the real
           Program.cs via WebApplicationFactory under the "Testing" environment;
           the factory supplies its own in-memory Jwt config, so no external
           services are required.
#>
[CmdletBinding()]
param(
    [string]$TestProject   = "tests/EAM.Tests/EAM.Tests.csproj",
    [string]$Configuration = "Release",
    [string]$ReportDir     = "reports"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null

# Absolute path — see the note in unit-tests.ps1 (avoids reports/reports/ nesting).
$reportPath = Join-Path (Resolve-Path $ReportDir).Path "integration-tests.xml"

# TEST-ONLY config fallback. The integration host boots Program.cs which requires
# Jwt:SigningKey. EamWebAppFactory already injects these, but we also export them as
# environment variables so the host has them even if a future test boots Program
# without the factory. (Not production secrets; CI-only.)
$env:Jwt__Issuer             = "eam"
$env:Jwt__Audience           = "eam-clients"
$env:Jwt__AccessTokenMinutes = "15"
$env:Jwt__SigningKey         = "ci-test-signing-key-0123456789abcdef0123456789abcdef0123456789ab"

Write-Host "==> Running INTEGRATION tests (FullyQualifiedName~EAM.Tests.Integration)"
dotnet test $TestProject -c $Configuration `
    --filter "FullyQualifiedName~EAM.Tests.Integration" `
    --logger "junit;LogFilePath=$reportPath"

if ($LASTEXITCODE -ne 0) { throw "Integration tests failed ($LASTEXITCODE). See $reportPath" }
Write-Host "==> Integration tests passed. Report: $reportPath"
