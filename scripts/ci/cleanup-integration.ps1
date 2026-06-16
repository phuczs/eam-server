<#
.SYNOPSIS  Tear down anything prepare-integration.ps1 started.
.DESCRIPTION
    Runs in after_script so it executes even when the tests fail. Today the
    integration tests leave nothing behind, so this is a no-op. Add teardown
    (drop test DB, flush Redis keys, stop a helper service) here if
    prepare-integration.ps1 starts provisioning real infrastructure.
#>
$ErrorActionPreference = "Continue"   # never let cleanup mask the real test result
Write-Host "==> Integration cleanup: nothing to tear down."
