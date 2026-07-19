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
        Assert.IsTrue(maze.CanOccupy(spawns.PlayerOne, 4, 10));
        Assert.IsTrue(maze.CanOccupy(spawns.PlayerTwo, 4, 10));
        Assert.IsGreaterThan(100f, spawns.PlayerTwo.X - spawns.PlayerOne.X);
    }
}
