# Architecture

## Decision summary

Neon Labyrinth targets `net10.0-windows` and Windows Forms. WinForms is built
into the Windows Desktop runtime, provides reliable keyboard/window lifecycle
handling, and allows direct control over a small software framebuffer without
an engine or external runtime.

The solution separates deterministic rules (`MazeHunter.Core`) from Windows
presentation (`MazeHunter.Game`). Tests depend only on Core, so gameplay rules
can be verified without opening a window.

Milestone 13 expands the logical surface to 400-by-300. `GameGeometry` injects
the default 10-pixel tile size, 4-pixel actor radius, and 1.25-pixel projectile
radius into runners, enemies, projectiles, spawning, collision, and rendering.

## Modules

- **Bootstrap:** `Program` configures WinForms and top-level error handling.
- **Loop/timing:** `FixedStepClock` accumulates wall time and emits 60 Hz steps.
- **Presentation:** `GameForm` owns a 400-by-300 bitmap and scales it with nearest
  neighbor interpolation into a letterboxed resizable window.
- **Maze/collision:** immutable validated ASCII-authored tile grids own static
  collision queries; out-of-bounds coordinates are always solid.
- **Levels:** `LevelCatalog` owns named handcrafted maze, player-spawn, and
  enemy-entry definitions. `LevelDirector` owns quotas, composition, spawn
  timing, active caps, the 1.5-second transition, and increasing level identity.
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
  bursts for eliminations, damage, and level completion without gameplay state.
  Reduced-flash mode shortens and dims these effects.
- **Enemies:** fixed-capacity entity storage uses seeded gameplay randomness.
  Enemies move exactly center-to-center, choose again on arrival, reject
  avoidable reversals, and recover invalid positions to the nearest walkable
  center. They collide through the same configurable queries as runners.
- **Navigation profiles:** stack-backed breadth-first distance searches guide
  direct and predictive hunters without heap allocation. Veil checks clear
  projectile lanes before path selection; Prism maximizes path distance.
- **Scoring:** profile values feed a 2.5-second chain with a capped 4×
  multiplier; level and surviving-life bonuses are separate deterministic rules.
- **Lives/respawn:** `PlayerLife` owns damage eligibility, three lives,
  1.25-second absence, two-second protection, and game over. The spawn planner
  maximizes squared distance from every active enemy before return.
- **Flow state:** `GameFlow` is the authoritative title/instructions/playing/
  paused/game-over state machine. Only `Playing` permits simulation advancement;
  presentation and input adapters cannot invent transitions.
- **Local co-op:** two runner/life/score channels share enemies, levels, and the
  fixed projectile pool. Owner IDs route points, projectiles never query runner
  hits, hunters choose the nearest live runner, and `LocalTeamRules` defines
  survivor continuation, friendly-fire policy, and level recovery.
- **Persistence/configuration:** `PlayerProfile` and `ProfileJson` own versioned
  settings, callsigns, and leaderboard rules; `ProfileStore` owns per-user I/O.
- **Diagnostics/logging:** F3 samples presentation FPS, update work, allocation
  rate, entities, player state, level, seed, and flow state. A bounded per-user
  log records lifecycle events and recoverable storage failures.

## Ownership and timing

Core game state is authoritative. Rendering reads that state but never decides
rules. The 8 ms UI timer merely pumps elapsed time; simulation
advances only in fixed 1/60-second increments. Wall-clock gaps are capped at
250 ms to avoid a spiral after debugging or window stalls. Focus loss suspends
simulation and clears accumulated time.

Logical coordinates use 400-by-300 pixels. Maze geometry defaults to 10-pixel
tiles with actor positions represented in logical pixels. Physical window size
never changes gameplay coordinates.

## Performance target

The measured Release target is 60 simulation updates per second and smooth
presentation on an ordinary desktop. Final sampling produced approximately
62.6 presentation FPS, less than 0.01 ms displayed update time, and 63 KiB/s
allocation with the diagnostics overlay enabled.

The static maze is pre-rendered into a palette-aware bitmap and reused each
frame. Dynamic actors, projectiles, and bounded effects render over that layer.

## Persistence

Versioned JSON is stored under
`%LOCALAPPDATA%\NeonLabyrinth\profile.json`. `PlayerProfile` owns schema
normalization, callsign sanitation, top-10 ordering, and forward-version
rejection. Writes use a same-directory temporary file and replacement;
malformed data is renamed with a `profile.corrupt-*` timestamp and defaults are
restored. Storage errors fall back safely and are reported to diagnostics.
