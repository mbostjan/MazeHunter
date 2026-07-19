using System.Numerics;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Spawning;

namespace MazeHunter.Core.Levels;

/// <summary>Deterministic level composition, fair spawning, and explicit progression.</summary>
public sealed class LevelDirector
{
    private const float MinimumPlayerSpawnDistance = 48f;
    private float _spawnTimer;
    private float _completionTimer;
    private int _spawned;
    private int _defeated;

    public int LevelNumber { get; private set; } = 1;

    public LevelDefinition CurrentLevel => LevelCatalog.Get(LevelNumber);

    public int RequiredDefeats => Math.Min(6 + ((LevelNumber - 1) * 2), 24);

    public int DefeatedThisLevel => _defeated;

    public int RemainingThisLevel => RequiredDefeats - _defeated;

    public bool IsTransitioning { get; private set; }

    public float TransitionProgress => IsTransitioning ? Math.Clamp(_completionTimer / 1.5f, 0, 1) : 0;

    public bool LevelAdvancedThisUpdate { get; private set; }

    public void Update(
        EnemySystem enemies,
        Vector2 playerPosition,
        float deltaSeconds,
        Vector2? secondPlayerPosition = null)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        LevelAdvancedThisUpdate = false;
        if (_spawned >= RequiredDefeats && enemies.ActiveCount == 0)
        {
            IsTransitioning = true;
            _completionTimer += deltaSeconds;
            if (_completionTimer >= 1.5f)
            {
                AdvanceLevel();
            }

            return;
        }

        IsTransitioning = false;
        _spawnTimer -= deltaSeconds;
        var activeLimit = Math.Min(2 + (LevelNumber / 2), 6);
        if (_spawned >= RequiredDefeats || enemies.ActiveCount >= activeLimit || _spawnTimer > 0)
        {
            return;
        }

        var entry = SpawnPlanner.FindEnemyEntry(CurrentLevel, _spawned);
        if (Vector2.DistanceSquared(entry, playerPosition) <
                MinimumPlayerSpawnDistance * MinimumPlayerSpawnDistance ||
            (secondPlayerPosition is { } second &&
             Vector2.DistanceSquared(entry, second) <
             MinimumPlayerSpawnDistance * MinimumPlayerSpawnDistance))
        {
            _spawnTimer = 0.25f;
            return;
        }

        if (enemies.TrySpawn(ChooseKind(LevelNumber, _spawned), entry))
        {
            _spawned++;
            _spawnTimer = MathF.Max(0.35f, 1.2f - (LevelNumber * 0.06f));
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
        LevelNumber = 1;
        _spawned = 0;
        _defeated = 0;
        _spawnTimer = 0;
        _completionTimer = 0;
        IsTransitioning = false;
        LevelAdvancedThisUpdate = false;
    }

    private static EnemyKind ChooseKind(int level, int spawnIndex)
    {
        if ((spawnIndex + 1) % 7 == 0)
        {
            return EnemyKind.Prism;
        }

        var availableProfiles = Math.Min(level + 1, 5);
        return (spawnIndex % availableProfiles) switch
        {
            1 => EnemyKind.Tracer,
            2 => EnemyKind.Vector,
            3 => EnemyKind.Veil,
            4 => EnemyKind.Surge,
            _ => EnemyKind.Drifter
        };
    }

    private void AdvanceLevel()
    {
        LevelAdvancedThisUpdate = true;
        LevelNumber++;
        _spawned = 0;
        _defeated = 0;
        _spawnTimer = 0.75f;
        _completionTimer = 0;
        IsTransitioning = false;
    }
}
