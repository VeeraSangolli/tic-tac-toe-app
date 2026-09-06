param([switch]$SkipInstall)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$dotnet = if (Test-Path "$root/.tools/dotnet/dotnet.exe") { "$root/.tools/dotnet/dotnet.exe" } else { (Get-Command dotnet -ErrorAction Stop).Source }
$pnpmCommand = Get-Command pnpm -ErrorAction SilentlyContinue
$bundledPnpm = "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/bin/fallback/pnpm.cmd"
$pnpm = if ($pnpmCommand) { $pnpmCommand.Source } elseif (Test-Path $bundledPnpm) { $bundledPnpm } else { throw 'Install Node.js 22.12+ and pnpm 11, then retry.' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_HOME = "$root/.tools"
Push-Location "$root/frontend"
try {
    if (!$SkipInstall) { & $pnpm install --frozen-lockfile; if ($LASTEXITCODE) { throw 'Dependency installation failed.' } }
    & $pnpm build
    if ($LASTEXITCODE) { throw 'Angular build failed.' }
} finally { Pop-Location }
New-Item -ItemType Directory -Path "$root/backend/TicTacToe.Api/wwwroot" -Force | Out-Null
Copy-Item -Path "$root/frontend/dist/browser/*" -Destination "$root/backend/TicTacToe.Api/wwwroot" -Recurse -Force
& $dotnet build "$root/backend/TicTacToe.Api" -c Release
if ($LASTEXITCODE) { throw 'Backend build failed.' }
Write-Host 'Build complete. Run ./scripts/start-local.ps1'
