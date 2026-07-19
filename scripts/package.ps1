[CmdletBinding()]
param([string]$Version = "0.1.0")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifacts = [IO.Path]::GetFullPath((Join-Path $root "artifacts"))
$output = [IO.Path]::GetFullPath((Join-Path $artifacts "NeonLabyrinth-$Version-win-x64"))
if (-not $output.StartsWith($artifacts + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Package output resolved outside the artifacts directory."
}
if (Test-Path -LiteralPath $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}
dotnet publish (Join-Path $root "src\MazeHunter.Game\MazeHunter.Game.csproj") `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
    -p:Version=$Version -o $output
Write-Host "Package created at $output"
