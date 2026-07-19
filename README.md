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
Progressive cycles add shortest-path Tracers, predictive Vectors, lane-aware
Veils, fast Surges, and rare evasive Prisms.
The playable survival loop now includes profile scoring, chain multipliers,
three lives, delayed safe respawning, spawn protection, and game over/restart.

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

From the title screen, Enter starts a solo run and I opens instructions. During
play P or Escape pauses. From pause, R restarts and Q returns to the title.
Press 2 on the title screen for local dual-link co-op. Player 2 moves with the
arrow keys and fires with Enter or Right Control. Players score independently,
cannot harm each other, and keep the run alive while either has remaining lives.

Qualifying game-over scores prompt for a 1-8 character callsign. The top ten,
mute state, callsigns, and last-selected mode persist in versioned JSON at
`%LOCALAPPDATA%\NeonLabyrinth\profile.json`; the title shows the top three.

Accessibility controls: F2 toggles the persisted high-contrast palette, F4
toggles persisted reduced-flash effects, and M mutes all six original
programmatically synthesized sound cues.

See [GAME_DESIGN.md](docs/GAME_DESIGN.md) and [ROADMAP.md](docs/ROADMAP.md).
