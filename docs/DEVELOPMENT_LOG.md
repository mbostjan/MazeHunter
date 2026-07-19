# Development Log

## 2026-07-19 — Milestones 0 and 1

- Confirmed Windows x64, .NET SDK 10.0.300, Windows Desktop 10 runtime, Visual
  Studio 2026, and MSBuild 18.6.
- Named and defined the original game **Neon Labyrinth**.
- Selected WinForms plus a platform-neutral Core assembly.
- Established a 320×240 logical surface and deterministic 60 Hz simulation.
- Added elapsed-time clamping and focus-safe clock reset.
- Added build/run/test/package scripts and initial timing tests.
- Performance target: stable 60 Hz at 1080p with near-zero steady gameplay
  allocations.

No defects remain open for Milestone 1.

## 2026-07-19 — Milestone 2

- Added immutable, ASCII-authored maze representation with closed-boundary and
  full-connectivity validation.
- Added authoritative circle-versus-tile occupancy queries with solid
  out-of-bounds behavior.
- Added the original 31×21 Signal Crossing maze.
- Added simultaneous held/pressed keyboard state and focus-loss clearing.
- Replaced the foundation screen with a crisp maze and live input preview.
- Added six maze validation and collision tests.

## 2026-07-19 — Milestone 3

- Added deterministic signal-runner state with 48 pixel/second movement.
- Added buffered junction turns, immediate reversals, one-pixel collision
  substeps, facing direction, and distance-based two-frame animation.
- Added deterministic safe and separated spawn selection for two players.
- Connected most-recent-held WASD input to the Core runner model.
- Added movement, wall, turn, reversal, respawn, and spawn tests.

## 2026-07-19 — Milestone 4

- Added a fixed-capacity directional pulse pool with one active shot per owner.
- Added 112 pixel/second substep movement, wall removal, 2.5-second lifetime,
  owner-aware circle hit detection, and pool clearing.
- Added vivid direction-shaped pulse rendering and Space firing.
- Synthesized an original descending square-wave firing effect in memory and
  added asynchronous playback plus M mute control.
- Added projectile speed, wall, tunneling, lifetime, fire-limit, and hit tests.

## 2026-07-19 — Milestone 5

- Added fixed-capacity enemy entities and reproducible xorshift gameplay random.
- Added Drifter navigation with tile-center decisions, a 60% forward
  preference, avoidable-reversal rejection, and dead-end recovery.
- Added safe upper-grid enemy entry selection and a distinctive original
  animated construct silhouette.
- Connected projectile hits to enemy removal with owner attribution.
- Added deterministic navigation, corridor safety, capacity, reversal, random
  sequence, and projectile destruction tests.
- Smoke measurement exposed excessive repaint pumping at a 1 ms timer interval;
  changed presentation pumping to 16 ms while retaining independent 60 Hz rules.

## 2026-07-19 — Milestone 6

- Added Tracer shortest-path pursuit, Vector predictive interception, Veil
  projectile-lane avoidance, fast Surge pursuit, and evasive Prism navigation.
- Added stack-backed breadth-first path distances with no navigation heap churn.
- Added per-profile speeds, palette silhouettes, deterministic progressive
  composition, active caps, spawn intervals, and 48-pixel entry safety.
- Added cycle quotas, 1.5-second completion interlude, and automatic escalation.
- Added behavior-specific and round spawn/progression tests.

## 2026-07-19 — Milestone 7

- Added per-profile points, 2.5-second elimination chains, capped 4× multiplier,
  cycle bonuses, and immediate chain loss on damage.
- Added three lives, contact damage, 1.25-second respawn delay, safest-tile
  selection, two seconds of blinking protection, and terminal game over.
- Added live score/chain/lives HUD, respawn countdown, game-over overlay, and
  Enter restart.
- Added scoring, life transition, protection, contact, reset, and safe-respawn
  tests.

## 2026-07-19 — Milestone 8

- Added an explicit title, instructions, playing, paused, and game-over state
  machine; simulation advances only in the playing state.
- Added original animated title presentation, complete solo instructions,
  pause overlay, resume, in-run restart, return-to-title, and game-over restart.
- Consolidated solo HUD messaging for cycles, remaining signals, score, chain,
  lives, respawn state, and audio status.
- Added transition and simulation-gating tests.

## 2026-07-19 — Milestone 9

- Added title-selectable solo and dual-link modes.
- Added an independently controlled blue Player 2 with arrow movement,
  Enter/Right-Control firing, separate projectile color, score, chain, and lives.
- Added owner-routed scoring, disabled friendly fire, nearest-live-runner enemy
  targeting, two-player spawn gating, and teammate-aware safe respawns.
- Added survivor continuation, both-eliminated game over, shared survival
  bonuses, and one protected recovery life at the next cleared cycle.
- Added mode, nearest-target, recovery, team bonus, friendly-fire, and survivor
  rule tests.

## 2026-07-19 — Milestone 10

- Added versioned JSON profile schema for mute state, last mode, both callsigns,
  and top-ten high scores with mode, cycle, and UTC timestamp.
- Added uppercase alphanumeric 1-8 character callsign entry for qualifying solo
  and co-op scores, plus a title-screen top-three display.
- Added `%LOCALAPPDATA%\NeonLabyrinth` storage, temp-file replacement,
  malformed-file quarantine, safe defaults, and close-time persistence.
- Added round-trip, corruption, future/older version, normalization, callsign,
  qualification, ordering, and trimming tests.

## 2026-07-19 — Milestone 11

- Completed original synthesized cues for firing, enemy destruction, player
  damage, cycle completion, menu interaction, and game over.
- Added fixed-pool geometric elimination, damage, and cycle-clear effects.
- Added persisted F2 high-contrast and F4 reduced-flash accessibility options.
- Cached the full static maze into a palette-aware bitmap instead of rebuilding
  hundreds of tile rectangles on every paint.
- Added owner-colored pulses, brighter accessible runner colors, and effect
  position plumbing from authoritative combat results.

## 2026-07-19 — Milestone 12 / 1.0.0

- Added optional F3 diagnostics for FPS, update time, allocation rate, flow,
  mode, cycle, entity counts, player lives/state, effects, and random seed.
- Added a bounded per-user diagnostic log with safe failure behavior.
- Cached steady-path GDI fonts, brushes, pens, and runner geometry.
- Measured approximately 62.6 presentation FPS, less than 0.01 ms displayed
  update work, 63 KiB/s allocation in solo, and 87 KiB/s in active co-op with
  diagnostics enabled.
- Verified 65 Release tests, zero compiler/format warnings, and all documented
  PowerShell scripts.
- Produced and launched the single-file self-contained Windows x64 executable;
  it responded normally, created profile/log data, and exited with code 0.
- Captured the packaged title screen and completed release documentation.
- Final packaged smoke exercised solo and co-op simultaneous movement/fire,
  pause/resume, resize, minimize/restore focus handling, restart, title return,
  F3 diagnostics, versioned profile save, and clean exit code 0.
- Final package: 116,035,144 bytes; SHA-256
  `AE329B3D8628B2C01BFD3058012330982F5A72013FA517B2A400AC9BAC212C21`.
