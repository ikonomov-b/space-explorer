# Exports the Godot project with the Windows Desktop preset and runs the exported build's smoke check.
# Usage: powershell -ExecutionPolicy Bypass -File tests\SpaceExplorer.Game.Smoke\smoke.ps1
# Requires: Godot 4.7.2 .NET editor (`godot` on PATH or $env:GODOT), matching export templates, .NET SDK.
$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$godot = if ($env:GODOT) { $env:GODOT } else { "godot" }
$project = Join-Path $root "src\SpaceExplorer.Game"
$out = Join-Path $root "build\smoke\windows"
$bin = Join-Path $out "SpaceExplorer.Game.exe"
$consoleBin = Join-Path $out "SpaceExplorer.Game.console.exe"

if (Test-Path $out) { Remove-Item -Recurse -Force $out }
New-Item -ItemType Directory -Path $out | Out-Null

& $godot --headless --path $project --import
if ($LASTEXITCODE -ne 0) { throw "smoke: godot --import exited with $LASTEXITCODE" }

& $godot --headless --path $project --export-release "Windows Desktop" $bin
if ($LASTEXITCODE -ne 0) { throw "smoke: godot --export-release exited with $LASTEXITCODE" }

# The console wrapper attaches a console so the build's output and exit code reach this script.
$output = & $consoleBin --headless -- --smoke 2>&1 | Out-String
Write-Host $output
if ($LASTEXITCODE -ne 0) { throw "smoke: exported build exited with $LASTEXITCODE" }
if ($output -notmatch "SMOKE OK") { throw "smoke: 'SMOKE OK' marker not found in output" }
