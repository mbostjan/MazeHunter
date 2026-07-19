# Neon Labyrinth

Neon Labyrinth is an original fixed-screen retro maze shooter for one or two local
players. Players are signal runners trapped in a shifting defense grid, clearing
hostile constructs with directional pulse shots. It draws on the broad arcade
maze-shooter genre while using original names, rules, mazes, graphics, and audio.

The project currently contains the Milestone 1 application shell: a responsive
Windows Forms window, logical 320×240 framebuffer, crisp nearest-neighbor scaling,
fixed 60 Hz simulation clock, focus-safe timing, and clean shutdown.

## Requirements

- Windows 10 or later (x64)
- .NET SDK 10.0.300 or a compatible .NET 10 patch SDK

## Commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Direct equivalents are `dotnet build`, `dotnet test`, and
`dotnet run --project src/MazeHunter.Game`.

## Controls

Gameplay controls will be enabled in Milestone 2. Planned defaults are WASD +
Space for Player 1, arrow keys + Enter/Right Control for Player 2, P/Escape to
pause, M to mute, F3 for diagnostics, and Alt+Enter for display mode.

See [GAME_DESIGN.md](docs/GAME_DESIGN.md) and [ROADMAP.md](docs/ROADMAP.md).
