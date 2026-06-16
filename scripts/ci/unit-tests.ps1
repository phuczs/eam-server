<#
.SYNOPSIS  Run the EAM Platform UNIT tests and emit a JUnit report for GitLab.
.NOTES     Filters by the EAM.Tests.Unit namespace. The JUnit logger comes from the
           JunitXml.TestLogger package referenced in EAM.Tests.csproj.
#>
[CmdletBinding()]
param(
    [string]$TestProject   = "tests/EAM.Tests/EAM.Tests.csproj",
    [string]$Configuration = "Release",
    [string]$ReportDir     = "reports"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null

# Absolute path: a *relative* LogFilePath is resolved against the results directory,
# which would nest the file as reports/reports/unit-tests.xml and GitLab would find
# nothing at reports/unit-tests.xml. An absolute path lands it exactly here.
$reportPath = Join-Path (Resolve-Path $ReportDir).Path "unit-tests.xml"

Write-Host "==> Running UNIT tests (FullyQualifiedName~EAM.Tests.Unit)"
dotnet test $TestProject -c $Configuration `
    --filter "FullyQualifiedName~EAM.Tests.Unit" `
    --logger "junit;LogFilePath=$reportPath"

if ($LASTEXITCODE -ne 0) { throw "Unit tests failed ($LASTEXITCODE). See $reportPath" }
Write-Host "==> Unit tests passed. Report: $reportPath"
