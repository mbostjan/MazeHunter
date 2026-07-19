using System.Numerics;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Mazes;
using MazeHunter.Core.Spawning;

namespace MazeHunter.Core.Rounds;

/// <summary>Deterministic round composition, fair spawning, and progression.</summary>
public sealed class RoundDirector
{
    private const float MinimumPlayerSpawnDistance = 48f;
    private float _spawnTimer;
    private float _completionTimer;
    private int _spawned;
    private int _defeated;

    public int RoundNumber { get; private set; } = 1;

    public int RequiredDefeats => Math.Min(6 + ((RoundNumber - 1) * 2), 24);

    public int DefeatedThisRound => _defeated;

    public int RemainingThisRound => RequiredDefeats - _defeated;

    public bool IsCompleting { get; private set; }

    public bool RoundAdvancedThisUpdate { get; private set; }

    public void Update(
        Maze maze,
        EnemySystem enemies,
        Vector2 playerPosition,
        float deltaSeconds,
        Vector2? secondPlayerPosition = null)
    {
        ArgumentNullException.ThrowIfNull(maze);
        ArgumentNullException.ThrowIfNull(enemies);
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        RoundAdvancedThisUpdate = false;
        if (_spawned >= RequiredDefeats && enemies.ActiveCount == 0)
        {
            IsCompleting = true;
            _completionTimer += deltaSeconds;
            if (_completionTimer >= 1.5f)
            {
                AdvanceRound();
            }

            return;
        }

        IsCompleting = false;
        _spawnTimer -= deltaSeconds;
        var activeLimit = Math.Min(2 + (RoundNumber / 2), 6);
        if (_spawned >= RequiredDefeats || enemies.ActiveCount >= activeLimit || _spawnTimer > 0)
        {
            return;
        }

        var entry = SpawnPlanner.FindEnemyEntry(maze);
        if (Vector2.DistanceSquared(entry, playerPosition) <
                MinimumPlayerSpawnDistance * MinimumPlayerSpawnDistance ||
            (secondPlayerPosition is { } second &&
             Vector2.DistanceSquared(entry, second) <
             MinimumPlayerSpawnDistance * MinimumPlayerSpawnDistance))
        {
            _spawnTimer = 0.25f;
            return;
        }

        if (enemies.TrySpawn(ChooseKind(RoundNumber, _spawned), entry))
        {
            _spawned++;
            _spawnTimer = MathF.Max(0.35f, 1.2f - (RoundNumber * 0.06f));
        }
    }

    public void NotifyEnemyDefeated()
    {
        if (_defeated < _spawned)
        {
            _defeated++;
        }
    }

    public void Reset()
    {
        RoundNumber = 1;
        _spawned = 0;
        _defeated = 0;
        _spawnTimer = 0;
        _completionTimer = 0;
        IsCompleting = false;
        RoundAdvancedThisUpdate = false;
    }

    private static EnemyKind ChooseKind(int round, int spawnIndex)
    {
        if ((spawnIndex + 1) % 7 == 0)
        {
            return EnemyKind.Prism;
        }

        var availableProfiles = Math.Min(round + 1, 5);
        return (spawnIndex % availableProfiles) switch
        {
            1 => EnemyKind.Tracer,
            2 => EnemyKind.Vector,
            3 => EnemyKind.Veil,
            4 => EnemyKind.Surge,
            _ => EnemyKind.Drifter
        };
    }

    private void AdvanceRound()
    {
        RoundAdvancedThisUpdate = true;
        RoundNumber++;
        _spawned = 0;
        _defeated = 0;
        _spawnTimer = 0.75f;
        _completionTimer = 0;
        IsCompleting = false;
    }
}
