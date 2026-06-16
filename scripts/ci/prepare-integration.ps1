<#
.SYNOPSIS  Provision anything the integration tests need before they run.
.DESCRIPTION
    EAM Platform integration tests boot the API via WebApplicationFactory under the
    "Testing" environment with an in-memory configuration — there is genuinely
    nothing to provision here today.

    This placeholder is kept so the pipeline shape (prepare -> test -> cleanup)
    stays ready: if you later add an integration test that needs a real SQL Server
    or Redis on the Windows VM, start/seed it here.
#>
$ErrorActionPreference = "Stop"
Write-Host "==> Integration prep: nothing to provision (in-memory, Testing env)."
