using System.Numerics;
using MazeHunter.Core.Geometry;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Spawning;

public static class SpawnPlanner
{
    public static PlayerSpawns FindPlayerSpawns(Maze maze, int tileSize = 8)
    {
        ArgumentNullException.ThrowIfNull(maze);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);

        var lowerRow = maze.Height - 2;
        var playerOneTile = FindClosestWalkable(maze, new GridPoint(2, lowerRow));
        var playerTwoTile = FindClosestWalkable(maze, new GridPoint(maze.Width - 3, lowerRow), playerOneTile);
        return new PlayerSpawns(ToCenter(playerOneTile, tileSize), ToCenter(playerTwoTile, tileSize));
    }

    public static Vector2 FindEnemyEntry(Maze maze, int tileSize = 8)
    {
        ArgumentNullException.ThrowIfNull(maze);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);
        var tile = FindClosestWalkable(maze, new GridPoint(maze.Width / 2, 1));
        return ToCenter(tile, tileSize);
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
