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
