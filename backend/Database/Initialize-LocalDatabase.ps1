[CmdletBinding()]
param(
    [string]$Server = '(localdb)\MSSQLLocalDB'
)

$ErrorActionPreference = 'Stop'
$sqlcmd = Get-Command sqlcmd -ErrorAction Stop
$createScript = Join-Path $PSScriptRoot 'HospitalSmsDemo.sql'
$verifyScript = Join-Path $PSScriptRoot 'Verify-DemoDatabase.sql'

Write-Host "Creating HospitalSms on $Server..."
& $sqlcmd.Source -S $Server -E -b -i $createScript
if ($LASTEXITCODE -ne 0) {
    throw "Database creation failed with sqlcmd exit code $LASTEXITCODE."
}

Write-Host 'Verifying schema and demo seed data...'
& $sqlcmd.Source -S $Server -E -b -i $verifyScript
if ($LASTEXITCODE -ne 0) {
    throw "Database verification failed with sqlcmd exit code $LASTEXITCODE."
}

Write-Host 'HospitalSms demo database is ready.'
