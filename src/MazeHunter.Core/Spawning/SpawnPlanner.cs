using System.Numerics;
using MazeHunter.Core.Geometry;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Levels;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Spawning;

public static class SpawnPlanner
{
    public static PlayerSpawns FindPlayerSpawns(Maze maze, int tileSize = 10)
    {
        ArgumentNullException.ThrowIfNull(maze);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);

        var lowerRow = maze.Height - 2;
        var playerOneTile = FindClosestWalkable(maze, new GridPoint(2, lowerRow));
        var playerTwoTile = FindClosestWalkable(maze, new GridPoint(maze.Width - 3, lowerRow), playerOneTile);
        return new PlayerSpawns(ToCenter(playerOneTile, tileSize), ToCenter(playerTwoTile, tileSize));
    }

    public static PlayerSpawns FindPlayerSpawns(LevelDefinition level, int tileSize = 10)
    {
        ArgumentNullException.ThrowIfNull(level);
        return new PlayerSpawns(
            ToCenter(level.PlayerOneSpawn, tileSize),
            ToCenter(level.PlayerTwoSpawn, tileSize));
    }

    public static Vector2 FindEnemyEntry(Maze maze, int tileSize = 10)
    {
        ArgumentNullException.ThrowIfNull(maze);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);
        var tile = FindClosestWalkable(maze, new GridPoint(maze.Width / 2, 1));
        return ToCenter(tile, tileSize);
    }

    public static Vector2 FindEnemyEntry(LevelDefinition level, int spawnIndex, int tileSize = 10)
    {
        ArgumentNullException.ThrowIfNull(level);
        if (level.EnemyEntries.Count == 0)
        {
            throw new ArgumentException("A level requires at least one enemy entry.", nameof(level));
        }

        var tile = level.EnemyEntries[Math.Abs(spawnIndex) % level.EnemyEntries.Count];
        if (!level.Maze.IsWalkable(tile.X, tile.Y))
        {
            throw new ArgumentException("Enemy entries must be walkable.", nameof(level));
        }

        return ToCenter(tile, tileSize);
    }

    public static Vector2 FindSafestPlayerSpawn(
        Maze maze,
        EnemySystem enemies,
        int tileSize = 10,
        Vector2? teammatePosition = null)
    {
        ArgumentNullException.ThrowIfNull(maze);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);

        var bestTile = default(GridPoint);
        var bestSafety = float.NegativeInfinity;
        foreach (var tile in maze.WalkableTiles())
        {
            var center = ToCenter(tile, tileSize);
            var closestEnemyDistance = float.PositiveInfinity;
            if (teammatePosition is { } teammate)
            {
                closestEnemyDistance = Vector2.DistanceSquared(center, teammate);
            }

            for (var i = 0; i < enemies.Capacity; i++)
            {
                if (enemies[i].Active)
                {
                    closestEnemyDistance = MathF.Min(
                        closestEnemyDistance,
                        Vector2.DistanceSquared(center, enemies[i].Position));
                }
            }

            if (closestEnemyDistance > bestSafety)
            {
                bestTile = tile;
                bestSafety = closestEnemyDistance;
            }
        }

        if (float.IsNegativeInfinity(bestSafety))
        {
            throw new InvalidOperationException("Maze has no valid player spawn.");
        }

        return ToCenter(bestTile, tileSize);
    }

    private static GridPoint FindClosestWalkable(Maze maze, GridPoint target, GridPoint? excluded = null)
    {
        var best = default(GridPoint);
        var bestDistance = int.MaxValue;
        foreach (var tile in maze.WalkableTiles())
        {
            if (tile == excluded)
            {
                continue;
            }

            var distance = Math.Abs(tile.X - target.X) + Math.Abs(tile.Y - target.Y);
            if (distance < bestDistance)
            {
                best = tile;
                bestDistance = distance;
            }
        }

        if (bestDistance == int.MaxValue)
        {
            throw new InvalidOperationException("Maze has no available player spawn.");
        }

        return best;
    }

    private static Vector2 ToCenter(GridPoint tile, int tileSize) =>
        new((tile.X * tileSize) + (tileSize / 2f), (tile.Y * tileSize) + (tileSize / 2f));
}
