# Testing

## Automated

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1`.
Current tests cover fixed-step accumulation, long-frame clamping, reset
behavior, curated maze validity, connectivity, boundary enforcement, static
collision, runner speed, wall stops, buffered turns, reversal, respawn reset,
safe spawn placement, projectile movement, wall collision, tunneling prevention,
lifetime, owner fire limits, and circle hits. Later milestones add scoring,
enemy navigation, cycles, life rules, and storage.

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
