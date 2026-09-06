param([int]$Port = 5080)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dotnet = if (Test-Path "$root/.tools/dotnet/dotnet.exe") { "$root/.tools/dotnet/dotnet.exe" } else { (Get-Command dotnet -ErrorAction Stop).Source }
if (!(Test-Path "$root/backend/TicTacToe.Api/wwwroot/index.html")) { throw 'Build the application first: ./scripts/build.ps1' }
Set-Location "$root/backend/TicTacToe.Api"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_HOME = "$root/.tools"
Write-Host "Tic Tac Toe: http://127.0.0.1:$Port (Ctrl+C to stop)"
& $dotnet run --no-build -c Release --urls "http://127.0.0.1:$Port"
