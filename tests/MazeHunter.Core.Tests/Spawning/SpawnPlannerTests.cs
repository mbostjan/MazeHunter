using MazeHunter.Core.Actors;
using MazeHunter.Core.Mazes;
using MazeHunter.Core.Spawning;

namespace MazeHunter.Core.Tests.Spawning;

[TestClass]
public sealed class SpawnPlannerTests
{
    [TestMethod]
    public void FindPlayerSpawns_ReturnsDistinctSafeLowerMazePositions()
    {
        var maze = MazeCatalog.CreateSignalCrossing();

        var spawns = SpawnPlanner.FindPlayerSpawns(maze);

        Assert.AreNotEqual(spawns.PlayerOne, spawns.PlayerTwo);
        Assert.IsTrue(maze.CanOccupy(spawns.PlayerOne, Runner.CollisionRadius, Runner.TileSize));
        Assert.IsTrue(maze.CanOccupy(spawns.PlayerTwo, Runner.CollisionRadius, Runner.TileSize));
        Assert.IsGreaterThan(100f, spawns.PlayerTwo.X - spawns.PlayerOne.X);
    }
}

