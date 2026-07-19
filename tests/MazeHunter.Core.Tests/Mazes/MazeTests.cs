using System.Numerics;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Tests.Mazes;

[TestClass]
public sealed class MazeTests
{
    [TestMethod]
    public void CuratedMaze_IsClosedConnectedAndExpectedSize()
    {
        var maze = MazeCatalog.CreateSignalCrossing();

        Assert.AreEqual(31, maze.Width);
        Assert.AreEqual(21, maze.Height);
        Assert.IsGreaterThan(300, maze.WalkableTiles().Count());
        Assert.IsTrue(maze.WalkableTiles().All(tile => tile.X > 0 && tile.Y > 0));
    }

    [TestMethod]
    public void FromAscii_RejectsDisconnectedFloorRegions()
    {
        string[] rows =
        [
            "#####",
            "#.#.#",
            "#####"
        ];

        Assert.ThrowsExactly<ArgumentException>(() => Maze.FromAscii(rows));
    }

    [TestMethod]
    public void FromAscii_RejectsOpenBoundary()
    {
        string[] rows =
        [
            "##.##",
            "#...#",
            "#####"
        ];

        Assert.ThrowsExactly<ArgumentException>(() => Maze.FromAscii(rows));
    }

    [TestMethod]
    public void CanOccupy_AllowsCorridorCenter()
    {
        var maze = Maze.FromAscii(
        [
            "#####",
            "#...#",
            "#####"
        ]);

        Assert.IsTrue(maze.CanOccupy(new Vector2(12, 12), 3, 8));
    }

    [TestMethod]
    public void CanOccupy_RejectsWallOverlapAndOutsideMaze()
    {
        var maze = Maze.FromAscii(
        [
            "#####",
            "#...#",
            "#####"
        ]);

        Assert.IsFalse(maze.CanOccupy(new Vector2(8.5f, 12), 3, 8));
        Assert.IsFalse(maze.CanOccupy(new Vector2(-1, 12), 3, 8));
    }

    [TestMethod]
    public void OutsideCoordinates_AreSolid()
    {
        var maze = MazeCatalog.CreateSignalCrossing();

        Assert.AreEqual(MazeTile.Wall, maze[-1, 1]);
        Assert.AreEqual(MazeTile.Wall, maze[maze.Width, 1]);
    }
}
