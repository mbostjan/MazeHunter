# Architecture

## Decision summary

Neon Labyrinth targets `net10.0-windows` and Windows Forms. WinForms is built
into the Windows Desktop runtime, provides reliable keyboard/window lifecycle
handling, and allows direct control over a small software framebuffer without
an engine or external runtime.

The solution separates deterministic rules (`MazeHunter.Core`) from Windows
presentation (`MazeHunter.Game`). Tests depend only on Core, so gameplay rules
can be verified without opening a window.

## Modules

- **Bootstrap:** `Program` configures WinForms and top-level error handling.
- **Loop/timing:** `FixedStepClock` accumulates wall time and emits 60 Hz steps.
- **Presentation:** `GameForm` owns a 320×240 bitmap and scales it with nearest
  neighbor interpolation into a letterboxed resizable window.
- **Maze/collision:** immutable validated ASCII-authored tile grids own static
  collision queries; out-of-bounds coordinates are always solid.
- **Input:** the WinForms adapter records held and newly pressed keys separately,
  supports simultaneous presses, and clears state whenever focus is lost.
- **Actors:** `Runner` owns deterministic axis-aligned movement, direction
  buffering, collision-safe substeps, facing, and animation phase. Platform
  input is reduced to a direction before entering Core.
- **Spawning:** deterministic target-based selection returns distinct valid
  tile-center positions for both local players.
- **Future Core systems:** state machine, input commands,
  projectiles, enemy strategies/pathfinding, spawning, rounds, score, settings,
  persistence, and diagnostic snapshots.
- **Future platform systems:** keyboard adapter, renderer, asynchronous audio,
  file store, and logging.

## Ownership and timing

Core game state will be authoritative. Rendering consumes immutable snapshots
and never decides rules. The UI timer merely pumps elapsed time; simulation
advances only in fixed 1/60-second increments. Wall-clock gaps are capped at
250 ms to avoid a spiral after debugging or window stalls. Focus loss suspends
simulation and clears accumulated time.

Logical coordinates use 320×240 pixels. Maze geometry will use 8-pixel tiles,
with actor positions represented in logical pixels. Physical window size never
changes gameplay coordinates.

## Performance target

Maintain 60 simulation updates per second and smooth presentation at 1920×1080
on an ordinary desktop, with steady-state gameplay allocations near zero.

## Persistence

Versioned JSON will be stored under
`%LOCALAPPDATA%\NeonLabyrinth\profile.json`. Writes will use a temporary file
and replacement; malformed data will be quarantined and defaults restored.
