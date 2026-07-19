# Testing

## Automated coverage

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1 -Configuration Release
```

The 69 automated tests cover:

- fixed-step timing, clamping, and reset;
- maze dimensions, boundary closure, connectivity, and invalid layouts;
- static occupancy collision and out-of-bounds solidity;
- runner speed, wall stops, reversal, buffered junction turns, and respawn reset;
- safe solo, co-op, and teammate-aware spawn selection;
- projectile speed, walls, tunneling, lifetime, limits, ownership, and hits;
- deterministic random sequences and enemy navigation;
- all six enemy profiles, nearest-runner targeting, and lane avoidance;
- enemy pool capacity, contact, and destruction result positions;
- three distinct handcrafted levels, spawn configurations, quotas, transition,
  reset, escalation, and layout rotation;
- configurable tile/collision geometry and larger-body wall safety;
- every enemy entry leaving spawn within one second and continuing to navigate;
- score values, chain growth/cap/expiry, cycle, life, and team bonuses;
- life loss, respawn delay, protection, recovery, reset, and game over;
- solo/co-op flow, pause gating, survivor rules, and no-friendly-fire policy;
- JSON round trips, old/future/corrupt data, callsigns, settings, and leaderboard rules.

## Verified release procedure

1. Run the Release build and test scripts.
2. Run `scripts\package.ps1 -Version 1.0.0`.
3. Confirm the output directory contains only `NeonLabyrinth.exe`.
4. Launch that exact executable.
5. Confirm the title renders, mode and accessibility keys respond, and audio initializes.
6. Start solo, move, fire, pause, resume, restart, and return to the title.
7. Start co-op, hold both players' movement keys simultaneously, and fire both pulses.
8. Resize the window across aspect ratios and confirm crisp letterboxed scaling.
9. Switch focus away and back; confirm input clears and timing does not jump.
10. Close normally and confirm exit code 0, profile JSON, and diagnostic log creation.

For isolated profile smoke tests, set `NEON_LABYRINTH_DATA_DIR` to a temporary
directory before launch.

## Performance evidence

The Release build was measured during a running game with F3 diagnostics:

- presentation: approximately 62 FPS;
- deterministic simulation: 60 updates/second;
- sampled update work: below 0.01 ms at the observed precision;
- allocation rate with diagnostics enabled: approximately 63 KiB/s in solo and
  87 KiB/s during the final active co-op smoke;
- working set of the self-contained build at title: approximately 49 MiB.

The static maze and GDI gameplay resources are cached. Core actor, enemy,
projectile, effect, and pathfinding updates use bounded storage without
steady-frame heap allocation.

## Known non-critical limitations

- Keyboard input only; controllers are deferred.
- Three curated mazes ship in 1.1 and rotate after the third level.
- Windowed play only; the window remains freely resizable.
- Local multiplayer only.
- Rendering and audio require Windows desktop APIs, so automated tests focus on
  the platform-neutral rules while packaged UI behavior is smoke-tested.
