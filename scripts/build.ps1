[CmdletBinding()]
param([ValidateSet("Debug", "Release")][string]$Configuration = "Debug")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
dotnet restore (Join-Path $root "MazeHunter.sln")
dotnet build (Join-Path $root "MazeHunter.sln") -c $Configuration --no-restore

