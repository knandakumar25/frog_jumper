<#
.SYNOPSIS
    Save a compressed copy of the running Axiom Force PostgreSQL database.
.DESCRIPTION
    Runs pg_dump inside the Compose `db` container, then copies the dump to the
    git-ignored backups/ folder. The dump stays in the container until the copy
    finishes, which avoids PowerShell altering binary output in a pipeline.
    Manual only: nothing schedules this script. Requires the `db` service to run.
.PARAMETER OutputDirectory
    Destination folder, relative to the project root. Defaults to backups.
.OUTPUTS
    Prints the path of the finished dump.
#>
param([string]$OutputDirectory = 'backups')
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..\..')
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$name = "axiom-force-$(Get-Date -Format 'yyyyMMdd-HHmmss').dump"
docker compose exec -T db sh -c "pg_dump -U `"`$POSTGRES_USER`" -d `"`$POSTGRES_DB`" -Fc -f /tmp/$name"
if ($LASTEXITCODE -ne 0) { throw 'pg_dump failed; no backup was written.' }
docker compose cp "db:/tmp/$name" (Join-Path $OutputDirectory $name)
$copied = $LASTEXITCODE
docker compose exec -T db rm -f "/tmp/$name"
if ($copied -ne 0) { throw 'Copying the dump out of the container failed.' }
Write-Output (Join-Path $OutputDirectory $name)
