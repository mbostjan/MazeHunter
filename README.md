# Neon Labyrinth

Neon Labyrinth is an original fixed-screen retro maze shooter for one or two local
players. Players are signal runners trapped in a shifting defense grid, clearing
hostile constructs with directional pulse shots. It draws on the broad arcade
maze-shooter genre while using original names, rules, mazes, graphics, and audio.

The current build contains the Milestone 2 foundation: a responsive Windows
Forms shell, logical 320×240 framebuffer, fixed 60 Hz simulation, simultaneous
keyboard tracking, a validated original tile maze, static collision rules, and
a controllable animated signal runner with buffered corridor turns and
directional pulses with original synthesized firing audio.
The first enemy, the seeded maze-wandering Drifter construct, is active and can
be destroyed by pulse fire.

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

Current controls are WASD to move, Space to fire, and M to mute. Planned
Player 2 defaults are arrow keys plus Enter/Right Control; P/Escape will pause,
F3 will toggle diagnostics, and Alt+Enter will toggle display mode.

See [GAME_DESIGN.md](docs/GAME_DESIGN.md) and [ROADMAP.md](docs/ROADMAP.md).
