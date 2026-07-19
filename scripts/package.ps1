[CmdletBinding()]
param([string]$Version = "0.1.0")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root "artifacts\NeonLabyrinth-$Version-win-x64"
dotnet publish (Join-Path $root "src\MazeHunter.Game\MazeHunter.Game.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:Version=$Version -o $output
Write-Host "Package created at $output"
