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
