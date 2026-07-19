using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Mazes;
using MazeHunter.Core.Spawning;

namespace MazeHunter.Core.Tests.Spawning;

[TestClass]
public sealed class SafeRespawnTests
{
    [TestMethod]
    public void SafestSpawn_IsValidAndFarFromEnemyCluster()
    {
        var maze = MazeCatalog.CreateSignalCrossing();
        var enemies = new EnemySystem(seed: 1);
        enemies.TrySpawn(EnemyKind.Drifter, new Vector2(12, 12));
        enemies.TrySpawn(EnemyKind.Tracer, new Vector2(20, 12));

        var spawn = SpawnPlanner.FindSafestPlayerSpawn(maze, enemies);

        Assert.IsTrue(maze.CanOccupy(spawn, Runner.CollisionRadius, Runner.TileSize));
        Assert.IsGreaterThan(10000f, Vector2.DistanceSquared(spawn, new Vector2(16, 12)));
    }

    [TestMethod]
    public void SafestSpawn_AvoidsActiveTeammateWhenNoEnemiesRemain()
    {
        var maze = MazeCatalog.CreateSignalCrossing();
        var enemies = new EnemySystem(seed: 1);
        var teammate = new Vector2(12, 12);

        var spawn = SpawnPlanner.FindSafestPlayerSpawn(
            maze,
            enemies,
            teammatePosition: teammate);

        Assert.IsGreaterThan(20000f, Vector2.DistanceSquared(spawn, teammate));
    }
}
