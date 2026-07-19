using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Geometry;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Tests.Geometry;

[TestClass]
public sealed class GameGeometryTests
{
    [TestMethod]
    public void Runner_UsesInjectedTileAndCollisionDimensions()
    {
        var maze = Maze.FromAscii(["#####", "#...#", "#####"]);
        var geometry = new GameGeometry(12, 5, 1);
        var runner = new Runner(new Vector2(18, 18), geometry);

        runner.Update(maze, Direction.Right, 0.1f);

        Assert.AreEqual(12, runner.TileSize);
        Assert.AreEqual(5, runner.CollisionRadius);
        Assert.IsTrue(maze.CanOccupy(runner.Position, runner.CollisionRadius, runner.TileSize));
    }
}
