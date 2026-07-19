# Game Design

## Concept

**Neon Labyrinth** is a compact arcade game set inside a corrupted city-scale
computer. One or two **signal runners** enter single-screen defense grids,
destroy roaming **constructs**, and survive escalating security levels. The
identity is neon circuitry, readable geometry, short tactical levels, and
cooperative positioning.

## Core rules

- Axis-aligned movement through spacious 10-pixel-grid corridors.
- Runners fire one active directional pulse at first; upgrades are temporary.
- A level ends when all required constructs are destroyed.
- Contact or a hostile shot costs a life, followed by a protected respawn.
- Later levels increase decision pressure through compositions and behavior,
  not only raw speed.

## Level progression

The opening sequence is **Signal Crossing**, **Relay Gardens**, and **Prism
Vault**. Each uses a distinct handcrafted 31-by-21 wall pattern, player start
pair, and enemy-entry rotation. Clearing every required construct freezes play
for a 1.5-second animated uplink screen, then loads the next maze and respawns
surviving runners. Layouts rotate after level three while quotas, compositions,
and spawn pressure continue to escalate.

## Enemy roster

- **Drifter:** chooses legal corridors with mild forward preference.
- **Tracer:** pursues the closest runner by shortest-path distance.
- **Vector:** aims for the runner's likely next junction.
- **Veil:** avoids exposed projectile lanes when an alternate route exists.
- **Surge:** fast late-level hunter with clearly telegraphed activation.
- **Prism:** rare evasive bonus construct worth substantial score.

Profiles unlock progressively: Tracer in level 1, Vector in level 2, Veil in
level 3, and Surge in level 4. Prism appears as a rare seventh spawn. Active
enemy count is capped at six, and the entry node pauses whenever a runner is
within 48 logical pixels.

## Two-player design

Two players cooperate to clear the same level while keeping individual scores.
They cannot damage each other. Each has three lives; a defeated player can
return at the next cleared level if the partner survives. Respawns choose the
safest valid runner node and grant brief protection. The level continues while
at least one runner remains. A team score bonus rewards both surviving.

Player 1 uses WASD and Space; Player 2 uses arrow keys and Enter or Right
Control. Pulse ownership awards the individual elimination score. Both players
receive level/life bonuses, plus a shared-survival bonus when neither has been
eliminated. Regular life loss uses delayed protected respawn; a fully eliminated
partner returns with one protected life only after the survivor clears a level.
Respawn selection also treats the active teammate as a hazard to avoid overlap.

## Presentation and accessibility

The limited neon palette uses shape plus color to distinguish entities. F2
switches to a persisted high-contrast maze/player palette. F4 enables persisted
reduced-flash feedback by shortening and dimming combat/level effects. All six
event sounds are original runtime-generated PCM cues and can be muted with M.

## Scoring and progression

Construct values reflect danger and rarity. Consecutive eliminations without
damage build a modest chain multiplier. Level completion awards speed and
survival bonuses. Difficulty uses larger mixed waves, shorter spawn intervals,
new behaviors, and explicit active-enemy caps.

Players begin with three lives. Contact removes the runner for 1.25 seconds,
then selects the maze tile farthest from active constructs and grants two
seconds of visibly blinking protection. The third hit ends the run. Chains
expire after 2.5 seconds or immediately on damage and rise by one multiplier
step every three eliminations, capped at 4×.

## Required for 1.0

Complete solo and local cooperative play, six enemy profiles, curated original
mazes, shooting, lives, score, levels, menus, settings/mute, high scores,
original generated audio/visuals, diagnostics, tests, and x64 packaging.

## Optional later

Controllers, online play, editor/modding, replays, achievements, campaigns,
platform integration, and additional visual themes.
