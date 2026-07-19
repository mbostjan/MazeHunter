# Changelog

## 1.1.0 - 2026-07-19

### Added

- Explicit named level progression with animated 1.5-second transition screens.
- Relay Gardens and Prism Vault, joining Signal Crossing as three distinct
  handcrafted 31-by-21 mazes with per-level player and enemy spawn layouts.
- Configurable shared tile, actor-radius, and projectile-radius geometry.
- Automated level catalog, custom geometry, and all-entry navigation tests.

### Changed

- Increased the logical framebuffer to 400-by-300, tiles to 10 pixels, collision
  bodies to 4 pixels, and actor silhouettes to 9-10 pixels.
- Replaced cycle terminology and `RoundDirector` with explicit levels and
  `LevelDirector`.
- Enemy travel now advances center-to-center and recovers safely from an
  invalid position, preventing off-center corner lockups.

## 1.0.0 - 2026-07-19

### Added

- Initial Neon Labyrinth architecture and game design.
- Windows Forms application shell with fixed-step timing.
- 320×240 nearest-neighbor-scaled presentation.
- Core timing tests and Windows build automation.
- Validated original maze format, static collision queries, simultaneous
  keyboard state, and Signal Crossing maze preview.
- Responsive runner movement with buffered turns, animation, and deterministic
  safe player spawns.
- Fixed-pool directional pulses, collision/hit rules, and original synthesized
  asynchronous firing audio with mute control.
- Seeded fixed-pool Drifter enemies with fair tile-center navigation and
  projectile destruction.
- Five advanced enemy profiles plus safe spawn direction, escalating quotas,
  progressive compositions, and automatic cycle advancement.
- Profile scoring, chain multipliers, lives, contact damage, safe protected
  respawning, cycle bonuses, game over, and restart.
- Original title/instructions presentation, authoritative flow state, complete
  solo HUD, pause/resume, in-run restart, and return-to-title controls.
- Complete local co-op with independent runners, controls, scores, lives,
  owner-colored shots, survivor rules, cycle recovery, and no friendly fire.
- Versioned per-user profile, callsign entry, top-ten leaderboard, last mode and
  mute persistence, atomic replacement, and corruption recovery.
- Complete six-cue synthesized audio set, fixed-pool feedback effects, cached
  maze rendering, high-contrast palette, and reduced-flash mode.
- Optional diagnostics overlay and bounded lifecycle/error logging.
- Self-contained single-file Windows x64 packaging and complete release docs.
