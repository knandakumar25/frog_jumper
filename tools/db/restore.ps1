<#
.SYNOPSIS
    Restore a dump made by backup.ps1 into the Compose PostgreSQL database.
.DESCRIPTION
    Replaces the contents of the target database, so existing rows are lost.
    It refuses to run unless -File names an existing dump and -ConfirmOverwrite
    equals the target database name, which forces the caller to name what will
    be replaced. The restore is one transaction, so a failure leaves the target
    unchanged. Stop the game before restoring.
.PARAMETER File
    Path to a .dump file.
.PARAMETER ConfirmOverwrite
    Must exactly match the target database name.
.PARAMETER TargetDatabase
    Database to restore into. Defaults to the service's POSTGRES_DB. It must
    already exist; passing a scratch database allows a safe trial restore.
#>
param(
    [Parameter(Mandatory)][string]$File,
    [Parameter(Mandatory)][string]$ConfirmOverwrite,
    [string]$TargetDatabase = ''
)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..\..')
if (-not (Test-Path -PathType Leaf $File)) { throw "Dump not found: $File" }
if (-not $TargetDatabase) { $TargetDatabase = (docker compose exec -T db printenv POSTGRES_DB).Trim() }
if ($ConfirmOverwrite -cne $TargetDatabase) { throw "Refusing to restore: -ConfirmOverwrite must be '$TargetDatabase'." }
$remote = '/tmp/axiom-restore.dump'
docker compose cp $File "db:$remote"
if ($LASTEXITCODE -ne 0) { throw 'Copying the dump into the container failed.' }
docker compose exec -T db sh -c "pg_restore -U `"`$POSTGRES_USER`" -d $TargetDatabase --clean --if-exists --no-owner --exit-on-error --single-transaction $remote"
$restored = $LASTEXITCODE
docker compose exec -T db rm -f $remote
if ($restored -ne 0) { throw 'pg_restore failed; the transaction was rolled back and the target is unchanged.' }
Write-Output "Restored $File into $TargetDatabase."
