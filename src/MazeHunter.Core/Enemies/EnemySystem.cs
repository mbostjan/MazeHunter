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
    public const float TracerSpeed = 31f;
    public const float VectorSpeed = 34f;
    public const float VeilSpeed = 30f;
    public const float SurgeSpeed = 46f;
    public const float PrismSpeed = 38f;
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

    public bool TrySpawnDrifter(Vector2 position) => TrySpawn(EnemyKind.Drifter, position);

    public bool TrySpawn(EnemyKind kind, Vector2 position)
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
                kind,
                position,
                Direction.None,
                0,
                new GridPoint(-1, -1));
            ActiveCount++;
            return true;
        }

        return false;
    }

    public void Update(Maze maze, float deltaSeconds) =>
        Update(maze, deltaSeconds, new EnemyContext(Vector2.Zero, Direction.None));

    public void Update(Maze maze, float deltaSeconds, EnemyContext context)
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

            var remaining = GetSpeed(enemy.Kind) * deltaSeconds;
            while (remaining > 0)
            {
                var tile = GetCenteredTile(enemy.Position);
                if (tile is { } centeredTile && centeredTile != enemy.LastDecisionTile)
                {
                    var direction = ChooseDirection(maze, centeredTile, enemy, context);
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

    public bool TryDestroyWithProjectiles(ProjectileSystem projectiles, out int ownerId) =>
        TryDestroyWithProjectiles(projectiles, out ownerId, out _);

    public bool TryDestroyWithProjectiles(
        ProjectileSystem projectiles,
        out int ownerId,
        out EnemyKind destroyedKind)
    {
        ArgumentNullException.ThrowIfNull(projectiles);
        for (var i = 0; i < _enemies.Length; i++)
        {
            if (!_enemies[i].Active ||
                !projectiles.TryHitCircle(_enemies[i].Position, CollisionRadius, out ownerId))
            {
                continue;
            }

            destroyedKind = _enemies[i].Kind;
            _enemies[i] = default;
            ActiveCount--;
            return true;
        }

        ownerId = -1;
        destroyedKind = default;
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

    private Direction ChooseDirection(
        Maze maze,
        GridPoint tile,
        Enemy enemy,
        EnemyContext context) =>
        enemy.Kind switch
        {
            EnemyKind.Tracer => ChooseToward(maze, tile, ToTile(context.PlayerPosition), enemy.Direction),
            EnemyKind.Vector => ChooseToward(
                maze,
                tile,
                GetPredictionTile(maze, context.PlayerPosition, context.PlayerFacing),
                enemy.Direction),
            EnemyKind.Veil => ChooseVeilDirection(maze, tile, enemy.Direction, context),
            EnemyKind.Surge => ChooseToward(maze, tile, ToTile(context.PlayerPosition), enemy.Direction),
            EnemyKind.Prism => ChooseAway(maze, tile, ToTile(context.PlayerPosition), enemy.Direction),
            _ => ChooseDrifterDirection(maze, tile, enemy.Direction)
        };

    private Direction ChooseVeilDirection(
        Maze maze,
        GridPoint tile,
        Direction current,
        EnemyContext context)
    {
        Span<Direction> legal = stackalloc Direction[4];
        var count = GetLegalDirections(maze, tile, current, legal);
        if (count == 0)
        {
            return Direction.None;
        }

        if (context.Projectiles is not null)
        {
            Span<Direction> safe = stackalloc Direction[4];
            var safeCount = 0;
            for (var i = 0; i < count; i++)
            {
                var offset = legal[i].ToVector();
                var next = new GridPoint(tile.X + (int)offset.X, tile.Y + (int)offset.Y);
                if (!IsInProjectileLane(maze, next, context.Projectiles))
                {
                    safe[safeCount++] = legal[i];
                }
            }

            if (safeCount > 0)
            {
                return ChooseBestToward(
                    maze,
                    safe[..safeCount],
                    tile,
                    ToTile(context.PlayerPosition));
            }
        }

        return ChooseBestToward(maze, legal[..count], tile, ToTile(context.PlayerPosition));
    }

    private Direction ChooseToward(Maze maze, GridPoint from, GridPoint target, Direction current)
    {
        Span<Direction> legal = stackalloc Direction[4];
        var count = GetLegalDirections(maze, from, current, legal);
        return count == 0 ? Direction.None : ChooseBestToward(maze, legal[..count], from, target);
    }

    private static Direction ChooseBestToward(
        Maze maze,
        ReadOnlySpan<Direction> legal,
        GridPoint from,
        GridPoint target)
    {
        var best = legal[0];
        var bestDistance = int.MaxValue;
        foreach (var direction in legal)
        {
            var offset = direction.ToVector();
            var next = new GridPoint(from.X + (int)offset.X, from.Y + (int)offset.Y);
            var distance = ShortestDistance(maze, next, target);
            if (distance < bestDistance)
            {
                best = direction;
                bestDistance = distance;
            }
        }

        return best;
    }

    private Direction ChooseAway(Maze maze, GridPoint from, GridPoint target, Direction current)
    {
        Span<Direction> legal = stackalloc Direction[4];
        var count = GetLegalDirections(maze, from, current, legal);
        if (count == 0)
        {
            return Direction.None;
        }

        var best = legal[0];
        var bestDistance = -1;
        for (var i = 0; i < count; i++)
        {
            var offset = legal[i].ToVector();
            var next = new GridPoint(from.X + (int)offset.X, from.Y + (int)offset.Y);
            var distance = ShortestDistance(maze, next, target);
            if (distance > bestDistance)
            {
                best = legal[i];
                bestDistance = distance;
            }
        }

        return best;
    }

    private static int GetLegalDirections(
        Maze maze,
        GridPoint tile,
        Direction current,
        Span<Direction> choices)
    {
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

        if (count > 1 && current != Direction.None)
        {
            var reverse = current.Opposite();
            for (var i = 0; i < count; i++)
            {
                if (choices[i] == reverse)
                {
                    choices[i] = choices[--count];
                    break;
                }
            }
        }

        return count;
    }

    private static int ShortestDistance(Maze maze, GridPoint start, GridPoint target)
    {
        if (!maze.IsWalkable(target.X, target.Y))
        {
            return int.MaxValue;
        }

        var area = maze.Width * maze.Height;
        Span<int> distances = stackalloc int[area];
        Span<int> queue = stackalloc int[area];
        distances.Fill(-1);
        var startIndex = (start.Y * maze.Width) + start.X;
        var targetIndex = (target.Y * maze.Width) + target.X;
        distances[startIndex] = 0;
        queue[0] = startIndex;
        var read = 0;
        var write = 1;
        ReadOnlySpan<GridPoint> offsets =
        [
            new(0, -1),
            new(0, 1),
            new(-1, 0),
            new(1, 0)
        ];

        while (read < write)
        {
            var index = queue[read++];
            if (index == targetIndex)
            {
                return distances[index];
            }

            var x = index % maze.Width;
            var y = index / maze.Width;
            foreach (var offset in offsets)
            {
                var nextX = x + offset.X;
                var nextY = y + offset.Y;
                var nextIndex = (nextY * maze.Width) + nextX;
                if (maze.IsWalkable(nextX, nextY) && distances[nextIndex] < 0)
                {
                    distances[nextIndex] = distances[index] + 1;
                    queue[write++] = nextIndex;
                }
            }
        }

        return int.MaxValue;
    }

    private static bool IsInProjectileLane(Maze maze, GridPoint tile, ProjectileSystem projectiles)
    {
        for (var i = 0; i < projectiles.Capacity; i++)
        {
            var projectile = projectiles[i];
            if (!projectile.Active)
            {
                continue;
            }

            var projectileTile = ToTile(projectile.Position);
            var aligned = projectile.Direction.IsVertical()
                ? projectileTile.X == tile.X
                : projectileTile.Y == tile.Y;
            if (!aligned || !IsAhead(projectileTile, tile, projectile.Direction))
            {
                continue;
            }

            var step = projectile.Direction.ToVector();
            var cursor = projectileTile;
            while (cursor != tile)
            {
                cursor = new GridPoint(cursor.X + (int)step.X, cursor.Y + (int)step.Y);
                if (!maze.IsWalkable(cursor.X, cursor.Y))
                {
                    break;
                }
            }

            if (cursor == tile)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAhead(GridPoint origin, GridPoint target, Direction direction) => direction switch
    {
        Direction.Up => target.Y < origin.Y,
        Direction.Down => target.Y > origin.Y,
        Direction.Left => target.X < origin.X,
        Direction.Right => target.X > origin.X,
        _ => false
    };

    private static GridPoint GetPredictionTile(Maze maze, Vector2 playerPosition, Direction facing)
    {
        var playerTile = ToTile(playerPosition);
        var offset = facing.ToVector();
        for (var distance = 3; distance > 0; distance--)
        {
            var candidate = new GridPoint(
                playerTile.X + ((int)offset.X * distance),
                playerTile.Y + ((int)offset.Y * distance));
            if (maze.IsWalkable(candidate.X, candidate.Y))
            {
                return candidate;
            }
        }

        return playerTile;
    }

    private static GridPoint ToTile(Vector2 position) =>
        new((int)MathF.Floor(position.X / Runner.TileSize), (int)MathF.Floor(position.Y / Runner.TileSize));

    private static float GetSpeed(EnemyKind kind) => kind switch
    {
        EnemyKind.Tracer => TracerSpeed,
        EnemyKind.Vector => VectorSpeed,
        EnemyKind.Veil => VeilSpeed,
        EnemyKind.Surge => SurgeSpeed,
        EnemyKind.Prism => PrismSpeed,
        _ => DrifterSpeed
    };

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
