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

    private readonly Enemy[] _enemies;
    private readonly DeterministicRandom _random;
    private readonly GameGeometry _geometry;
    private int _nextId = 1;

    public EnemySystem(uint seed, int capacity = 32, GameGeometry? geometry = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _enemies = new Enemy[capacity];
        _random = new DeterministicRandom(seed);
        _geometry = geometry ?? GameGeometry.Default;
        Seed = seed;
    }

    public uint Seed { get; }

    public int Capacity => _enemies.Length;

    public int ActiveCount { get; private set; }

    public float CollisionRadius => _geometry.ActorRadius;

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

                var distanceToCenter = DistanceToNextCenter(enemy.Position, enemy.Direction);
                var distance = MathF.Min(remaining, MathF.Min(1f, distanceToCenter));
                var candidate = enemy.Position + (enemy.Direction.ToVector() * distance);
                if (!maze.CanOccupy(candidate, CollisionRadius, _geometry.TileSize))
                {
                    var recovered = SnapToNearestWalkableCenter(maze, enemy.Position);
                    enemy = enemy with
                    {
                        Position = recovered,
                        Direction = Direction.None,
                        LastDecisionTile = new GridPoint(-1, -1)
                    };
                    continue;
                }

                if (MathF.Abs(distance - distanceToCenter) < 0.001f)
                {
                    candidate = SnapToNearestCenter(candidate);
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
        TryDestroyWithProjectiles(projectiles, out ownerId, out _, out _);

    public bool TryDestroyWithProjectiles(
        ProjectileSystem projectiles,
        out int ownerId,
        out EnemyKind destroyedKind) =>
        TryDestroyWithProjectiles(projectiles, out ownerId, out destroyedKind, out _);

    public bool TryDestroyWithProjectiles(
        ProjectileSystem projectiles,
        out int ownerId,
        out EnemyKind destroyedKind,
        out Vector2 destroyedPosition)
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
            destroyedPosition = _enemies[i].Position;
            _enemies[i] = default;
            ActiveCount--;
            return true;
        }

        ownerId = -1;
        destroyedKind = default;
        destroyedPosition = default;
        return false;
    }

    public void Clear()
    {
        Array.Clear(_enemies);
        ActiveCount = 0;
    }

    public bool HasContact(Vector2 center, float radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
        var contactDistance = radius + CollisionRadius;
        var contactDistanceSquared = contactDistance * contactDistance;
        for (var i = 0; i < _enemies.Length; i++)
        {
            if (_enemies[i].Active &&
                Vector2.DistanceSquared(_enemies[i].Position, center) <= contactDistanceSquared)
            {
                return true;
            }
        }

        return false;
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
        EnemyContext context)
    {
        var target = GetClosestTarget(enemy.Position, context);
        return enemy.Kind switch
        {
            EnemyKind.Tracer => ChooseToward(maze, tile, ToTile(target.Position), enemy.Direction),
            EnemyKind.Vector => ChooseToward(
                maze,
                tile,
                GetPredictionTile(maze, target.Position, target.Facing),
                enemy.Direction),
            EnemyKind.Veil => ChooseVeilDirection(maze, tile, enemy.Direction, context, target.Position),
            EnemyKind.Surge => ChooseToward(maze, tile, ToTile(target.Position), enemy.Direction),
            EnemyKind.Prism => ChooseAway(maze, tile, ToTile(target.Position), enemy.Direction),
            _ => ChooseDrifterDirection(maze, tile, enemy.Direction)
        };
    }

    private Direction ChooseVeilDirection(
        Maze maze,
        GridPoint tile,
        Direction current,
        EnemyContext context,
        Vector2 targetPosition)
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
                    ToTile(targetPosition));
            }
        }

        return ChooseBestToward(maze, legal[..count], tile, ToTile(targetPosition));
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

    private bool IsInProjectileLane(Maze maze, GridPoint tile, ProjectileSystem projectiles)
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

    private GridPoint GetPredictionTile(Maze maze, Vector2 playerPosition, Direction facing)
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

    private GridPoint ToTile(Vector2 position) =>
        new((int)MathF.Floor(position.X / _geometry.TileSize), (int)MathF.Floor(position.Y / _geometry.TileSize));

    private static (Vector2 Position, Direction Facing) GetClosestTarget(
        Vector2 enemyPosition,
        EnemyContext context)
    {
        if (context.SecondPlayerPosition is not { } second ||
            Vector2.DistanceSquared(enemyPosition, context.PlayerPosition) <=
            Vector2.DistanceSquared(enemyPosition, second))
        {
            return (context.PlayerPosition, context.PlayerFacing);
        }

        return (second, context.SecondPlayerFacing);
    }

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

    private GridPoint? GetCenteredTile(Vector2 position)
    {
        var tileX = (int)MathF.Floor(position.X / _geometry.TileSize);
        var tileY = (int)MathF.Floor(position.Y / _geometry.TileSize);
        var centerX = (tileX * _geometry.TileSize) + _geometry.TileCenterOffset;
        var centerY = (tileY * _geometry.TileSize) + _geometry.TileCenterOffset;
        return MathF.Abs(position.X - centerX) < 0.001f && MathF.Abs(position.Y - centerY) < 0.001f
            ? new GridPoint(tileX, tileY)
            : null;
    }

    private float DistanceToNextCenter(Vector2 position, Direction direction)
    {
        var coordinate = direction.IsVertical() ? position.Y : position.X;
        var tileSize = _geometry.TileSize;
        var center = (MathF.Floor(coordinate / tileSize) * tileSize) + _geometry.TileCenterOffset;
        if (direction is Direction.Right or Direction.Down)
        {
            return center > coordinate + 0.001f ? center - coordinate : center + tileSize - coordinate;
        }

        return center < coordinate - 0.001f ? coordinate - center : coordinate - (center - tileSize);
    }

    private Vector2 SnapToNearestCenter(Vector2 position)
    {
        var tileSize = _geometry.TileSize;
        return new Vector2(
            (MathF.Floor(position.X / tileSize) * tileSize) + _geometry.TileCenterOffset,
            (MathF.Floor(position.Y / tileSize) * tileSize) + _geometry.TileCenterOffset);
    }

    private Vector2 SnapToNearestWalkableCenter(Maze maze, Vector2 position)
    {
        var snapped = SnapToNearestCenter(position);
        if (maze.CanOccupy(snapped, CollisionRadius, _geometry.TileSize))
        {
            return snapped;
        }

        var tile = maze.WalkableTiles().MinBy(candidate =>
        {
            var center = new Vector2(
                (candidate.X * _geometry.TileSize) + _geometry.TileCenterOffset,
                (candidate.Y * _geometry.TileSize) + _geometry.TileCenterOffset);
            return Vector2.DistanceSquared(position, center);
        });
        return new Vector2(
            (tile.X * _geometry.TileSize) + _geometry.TileCenterOffset,
            (tile.Y * _geometry.TileSize) + _geometry.TileCenterOffset);
    }
}
