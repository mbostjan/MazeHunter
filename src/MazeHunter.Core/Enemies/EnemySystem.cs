using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Combat;
using MazeHunter.Core.Diagnostics;
using MazeHunter.Core.Geometry;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Enemies;

/// <summary>Fixed-capacity enemy simulation with deterministic maze navigation.</summary>
public sealed class EnemySystem
{
    public const float DrifterSpeed = 32f;
    public const float CollisionRadius = 3f;

    private readonly Enemy[] _enemies;
    private readonly DeterministicRandom _random;
    private int _nextId = 1;

    public EnemySystem(uint seed, int capacity = 32)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _enemies = new Enemy[capacity];
        _random = new DeterministicRandom(seed);
        Seed = seed;
    }

    public uint Seed { get; }

    public int Capacity => _enemies.Length;

    public int ActiveCount { get; private set; }

    public Enemy this[int index] => _enemies[index];

    public bool TrySpawnDrifter(Vector2 position)
    {
        for (var i = 0; i < _enemies.Length; i++)
        {
            if (_enemies[i].Active)
            {
                continue;
            }

            _enemies[i] = new Enemy(
                true,
                _nextId++,
                EnemyKind.Drifter,
                position,
                Direction.None,
                0,
                new GridPoint(-1, -1));
            ActiveCount++;
            return true;
        }

        return false;
    }

    public void Update(Maze maze, float deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(maze);
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        for (var i = 0; i < _enemies.Length; i++)
        {
            var enemy = _enemies[i];
            if (!enemy.Active)
            {
                continue;
            }

            var remaining = DrifterSpeed * deltaSeconds;
            while (remaining > 0)
            {
                var tile = GetCenteredTile(enemy.Position);
                if (tile is { } centeredTile && centeredTile != enemy.LastDecisionTile)
                {
                    var direction = ChooseDrifterDirection(maze, centeredTile, enemy.Direction);
                    enemy = enemy with { Direction = direction, LastDecisionTile = centeredTile };
                }

                if (enemy.Direction == Direction.None)
                {
                    break;
                }

                var distance = MathF.Min(remaining, 1f);
                var candidate = enemy.Position + (enemy.Direction.ToVector() * distance);
                if (!maze.CanOccupy(candidate, CollisionRadius, Runner.TileSize))
                {
                    enemy = enemy with { Direction = Direction.None, LastDecisionTile = new GridPoint(-1, -1) };
                    break;
                }

                enemy = enemy with
                {
                    Position = candidate,
                    DistanceTravelled = enemy.DistanceTravelled + distance
                };
                remaining -= distance;
            }

            _enemies[i] = enemy;
        }
    }

    public bool TryDestroyWithProjectiles(ProjectileSystem projectiles, out int ownerId)
    {
        ArgumentNullException.ThrowIfNull(projectiles);
        for (var i = 0; i < _enemies.Length; i++)
        {
            if (!_enemies[i].Active ||
                !projectiles.TryHitCircle(_enemies[i].Position, CollisionRadius, out ownerId))
            {
                continue;
            }

            _enemies[i] = default;
            ActiveCount--;
            return true;
        }

        ownerId = -1;
        return false;
    }

    public void Clear()
    {
        Array.Clear(_enemies);
        ActiveCount = 0;
    }

    private Direction ChooseDrifterDirection(Maze maze, GridPoint tile, Direction current)
    {
        Span<Direction> choices = stackalloc Direction[4];
        var count = 0;
        if (maze.IsWalkable(tile.X, tile.Y - 1))
        {
            choices[count++] = Direction.Up;
        }

        if (maze.IsWalkable(tile.X, tile.Y + 1))
        {
            choices[count++] = Direction.Down;
        }

        if (maze.IsWalkable(tile.X - 1, tile.Y))
        {
            choices[count++] = Direction.Left;
        }

        if (maze.IsWalkable(tile.X + 1, tile.Y))
        {
            choices[count++] = Direction.Right;
        }

        if (count == 0)
        {
            return Direction.None;
        }

        if (count > 1 && current != Direction.None)
        {
            var reverse = current.Opposite();
            for (var i = 0; i < count; i++)
            {
                if (choices[i] != reverse)
                {
                    continue;
                }

                choices[i] = choices[count - 1];
                count--;
                break;
            }
        }

        if (current != Direction.None && Contains(choices[..count], current) && _random.Next(10) < 6)
        {
            return current;
        }

        return choices[_random.Next(count)];
    }

    private static bool Contains(ReadOnlySpan<Direction> choices, Direction direction)
    {
        foreach (var choice in choices)
        {
            if (choice == direction)
            {
                return true;
            }
        }

        return false;
    }

    private static GridPoint? GetCenteredTile(Vector2 position)
    {
        var tileX = (int)MathF.Floor(position.X / Runner.TileSize);
        var tileY = (int)MathF.Floor(position.Y / Runner.TileSize);
        var centerX = (tileX * Runner.TileSize) + (Runner.TileSize / 2f);
        var centerY = (tileY * Runner.TileSize) + (Runner.TileSize / 2f);
        return MathF.Abs(position.X - centerX) < 0.001f && MathF.Abs(position.Y - centerY) < 0.001f
            ? new GridPoint(tileX, tileY)
            : null;
    }
}
