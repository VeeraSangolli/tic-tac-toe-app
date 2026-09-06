$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dotnet = if (Test-Path "$root/.tools/dotnet/dotnet.exe") { "$root/.tools/dotnet/dotnet.exe" } else { (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_HOME = "$root/.tools"
& $dotnet test "$root/backend/TicTacToe.Tests" --verbosity minimal
if ($LASTEXITCODE) { throw 'Tests failed.' }
