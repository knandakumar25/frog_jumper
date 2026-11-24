<#
.SYNOPSIS
Runs the shared website's public smoke test from this game checkout.
.PARAMETER WebsiteRoot
Path to the shared website checkout.
#>
param([string]$WebsiteRoot = (Join-Path (Split-Path (Split-Path $PSScriptRoot)) 'axiom_force_website'))

$ErrorActionPreference = 'Stop'
$runner = Join-Path $WebsiteRoot 'smoke.ps1'
if (!(Test-Path -LiteralPath $runner -PathType Leaf)) { throw "Shared website smoke runner missing at $runner" }
& $runner
exit $LASTEXITCODE
