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
- **Combat:** a fixed-capacity projectile pool enforces per-owner fire limits,
  advances in collision-safe substeps, and provides reusable circle-hit queries.
- **Audio:** original PCM waveforms are synthesized and preloaded at startup;
  Windows asynchronous playback never waits in the simulation loop.
- **Effects:** a fixed-capacity presentation-only pool renders short geometric
  bursts for eliminations, damage, and cycle completion without gameplay state.
  Reduced-flash mode shortens and dims these effects.
- **Enemies:** fixed-capacity entity storage uses seeded gameplay randomness.
  Drifters decide only at tile centers, prefer forward travel, reject avoidable
  reversals, and collide through the same maze queries as runners.
- **Navigation profiles:** stack-backed breadth-first distance searches guide
  direct and predictive hunters without heap allocation. Veil checks clear
  projectile lanes before path selection; Prism maximizes path distance.
- **Rounds:** `RoundDirector` owns quotas, type composition, active caps,
  distance-gated spawn timing, completion delay, and escalating cycle state.
- **Scoring:** profile values feed a 2.5-second chain with a capped 4×
  multiplier; cycle and surviving-life bonuses are separate deterministic rules.
- **Lives/respawn:** `PlayerLife` owns damage eligibility, three lives,
  1.25-second absence, two-second protection, and game over. The spawn planner
  maximizes squared distance from every active enemy before return.
- **Flow state:** `GameFlow` is the authoritative title/instructions/playing/
  paused/game-over state machine. Only `Playing` permits simulation advancement;
  presentation and input adapters cannot invent transitions.
- **Local co-op:** two runner/life/score channels share enemies, rounds, and the
  fixed projectile pool. Owner IDs route points, projectiles never query runner
  hits, hunters choose the nearest live runner, and `LocalTeamRules` defines
  survivor continuation, friendly-fire policy, and cycle recovery.
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

The static maze is pre-rendered into a palette-aware bitmap and reused each
frame. Dynamic actors, projectiles, and bounded effects render over that layer.

## Persistence

Versioned JSON is stored under
`%LOCALAPPDATA%\NeonLabyrinth\profile.json`. `PlayerProfile` owns schema
normalization, callsign sanitation, top-10 ordering, and forward-version
rejection. Writes use a same-directory temporary file and replacement;
malformed data is renamed with a `profile.corrupt-*` timestamp and defaults are
restored. Storage errors fall back safely and are reported to diagnostics.
