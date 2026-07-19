# Testing

## Automated

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1`.
Current tests cover fixed-step accumulation, long-frame clamping, reset
behavior, curated maze validity, connectivity, boundary enforcement, static
collision, runner speed, wall stops, buffered turns, reversal, respawn reset,
safe spawn placement, projectile movement, wall collision, tunneling prevention,
lifetime, owner fire limits, and circle hits. Later milestones add scoring,
advanced navigation, cycles, life rules, and storage. Drifter coverage includes
seed reproducibility, long-run corridor safety, reversal rules, pool capacity,
and projectile removal.

Advanced behavior tests prove shortest-route pursuit, predictive targeting,
projectile-lane avoidance, evasive movement, and Surge speed. Round tests cover
safe-distance spawn gating, quotas, reset, and escalation.

Survival coverage verifies three-hit game over, respawn delay, protection,
reset, enemy contact, and farthest-valid-tile selection. Scoring coverage
verifies profile values, multiplier growth/cap, timeout, and cycle bonuses.

Flow tests cover initial title state, instructions, pause/resume simulation
gating, game-over restart, return to title, and rejected invalid transitions.

Co-op tests cover mode selection, nearest-runner hunting, independent projectile
ownership through existing owner-hit tests, friendly-fire policy, continuation
with one survivor, both-player game over, recovery life, and team bonuses.

Persistence tests cover JSON round trips, settings and timestamp preservation,
malformed JSON, unsupported future versions, older-version normalization,
callsign sanitation, leaderboard qualification, sorting, and top-ten trimming.
File smoke tests may set `NEON_LABYRINTH_DATA_DIR` to isolate profile writes
from the normal per-user directory.

Audio smoke verification constructs and preloads all six synthesized WAV
streams, invokes each asynchronous cue, and disposes every player. Accessibility
settings are covered by profile round-trip tests; effect hit positions are
covered by combat result tests.

## Milestone 1 manual smoke procedure

1. Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run.ps1`.
2. Confirm the Neon Labyrinth status screen animates.
3. Resize to several aspect ratios; pixels remain crisp and content letterboxes.
4. Switch focus away for several seconds and return; animation resumes without
   a timing jump.
5. Close the window; the process exits normally.

## Known limitations

Milestone 1 is an application foundation, not playable gameplay. Automated UI
tests are intentionally deferred; platform-neutral rules receive priority.
