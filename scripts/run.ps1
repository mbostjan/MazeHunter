$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
dotnet run --project (Join-Path $root "src\MazeHunter.Game\MazeHunter.Game.csproj")

