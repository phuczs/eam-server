<#
.SYNOPSIS  Verify the freshly deployed app is alive (GET /health).
.DESCRIPTION
    Polls the health probe a few times to allow for app warm-up, and fails the
    pipeline (non-zero exit) if it never returns "healthy". This is the gate that
    turns "files copied" into "deploy actually worked".

    Configure on the runner:
      STAGING_BASE_URL   e.g. https://staging.eam.local   (default below)
.NOTES     Local: pwsh -File scripts/ci/smoke-test.ps1 -BaseUrl http://localhost:5000
#>
[CmdletBinding()]
param(
    [string]$BaseUrl      = $env:STAGING_BASE_URL,
    [int]   $Retries      = 10,
    [int]   $DelaySeconds = 3
)

$ErrorActionPreference = "Stop"
if (-not $BaseUrl) { $BaseUrl = "http://localhost:8080" }
$health = "{0}/health" -f $BaseUrl.TrimEnd('/')

Write-Host "==> Smoke test: GET $health (up to $Retries attempts)"

for ($i = 1; $i -le $Retries; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri $health -UseBasicParsing -TimeoutSec 10
        if ($resp.StatusCode -eq 200 -and $resp.Content -match "healthy") {
            Write-Host "==> Healthy on attempt ${i}: $($resp.Content)"
            exit 0
        }
        Write-Host "    attempt ${i}: HTTP $($resp.StatusCode) body=$($resp.Content)"
    }
    catch {
        Write-Host "    attempt ${i}: $($_.Exception.Message)"
    }
    Start-Sleep -Seconds $DelaySeconds
}

throw "Smoke test FAILED: $health did not return 'healthy' after $Retries attempts."
