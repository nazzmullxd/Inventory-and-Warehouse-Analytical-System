param([switch]$AddDemoData)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$dotnetPath = if (Test-Path '.tools/dotnet/dotnet.exe') { (Resolve-Path '.tools/dotnet/dotnet.exe').Path } else { 'dotnet' }
$seedArguments = if ($AddDemoData) { @('--add-demo-data') } else { @() }
& $dotnetPath run --project database/Iwas.DatabaseSetup.csproj -- @seedArguments
if ($LASTEXITCODE -ne 0) { throw 'Database setup failed. Check that XAMPP MySQL is running and the setup credentials are correct.' }
