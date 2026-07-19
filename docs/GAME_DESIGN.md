# Game Design

## Concept

**Neon Labyrinth** is a compact arcade game set inside a corrupted city-scale
computer. One or two **signal runners** enter single-screen defense grids,
destroy roaming **constructs**, and survive escalating security cycles. The
identity is neon circuitry, readable geometry, short tactical rounds, and
cooperative positioning.

## Core rules

- Axis-aligned movement through 8-pixel-grid corridors.
- Runners fire one active directional pulse at first; upgrades are temporary.
- A cycle ends when all required constructs are destroyed.
- Contact or a hostile shot costs a life, followed by a protected respawn.
- Later cycles increase decision pressure through compositions and behavior,
  not only raw speed.

## Enemy roster

- **Drifter:** chooses legal corridors with mild forward preference.
- **Tracer:** pursues the closest runner by shortest-path distance.
- **Vector:** aims for the runner's likely next junction.
- **Veil:** avoids exposed projectile lanes when an alternate route exists.
- **Surge:** fast late-cycle hunter with clearly telegraphed activation.
- **Prism:** rare evasive bonus construct worth substantial score.

Profiles unlock progressively: Tracer in cycle 1, Vector in cycle 2, Veil in
cycle 3, and Surge in cycle 4. Prism appears as a rare seventh spawn. Active
enemy count is capped at six, and the entry node pauses whenever a runner is
within 48 logical pixels.

## Two-player design

Two players cooperate to clear the same cycle while keeping individual scores.
They cannot damage each other. Each has three lives; a defeated player can
return at the next cleared cycle if the partner survives. Respawns choose the
safest valid runner node and grant brief protection. The cycle continues while
at least one runner remains. A team score bonus rewards both surviving.

## Scoring and progression

Construct values reflect danger and rarity. Consecutive eliminations without
damage build a modest chain multiplier. Cycle completion awards speed and
survival bonuses. Difficulty uses curated maze rotation, larger mixed waves,
shorter spawn intervals, and new behaviors with explicit caps.

## Required for 1.0

Complete solo and local cooperative play, six enemy profiles, curated original
mazes, shooting, lives, score, rounds, menus, settings/mute, high scores,
original generated audio/visuals, diagnostics, tests, and x64 packaging.

## Optional later

Controllers, online play, editor/modding, replays, achievements, campaigns,
platform integration, and additional visual themes.
