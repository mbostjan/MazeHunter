using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Tests.Actors;

[TestClass]
public sealed class RunnerTests
{
    private static readonly Maze OpenCross = Maze.FromAscii(
    [
        "#######",
        "###.###",
        "###.###",
        "#.....#",
        "###.###",
        "###.###",
        "#######"
    ]);

    [TestMethod]
    public void Update_MovesAtConfiguredSpeed()
    {
        var runner = new Runner(new Vector2(35, 35));

        runner.Update(OpenCross, Direction.Right, 0.05f);

        Assert.AreEqual(37.4f, runner.Position.X, 0.001f);
        Assert.AreEqual(35f, runner.Position.Y);
        Assert.AreEqual(Direction.Right, runner.MovingDirection);
    }

    [TestMethod]
    public void Update_StopsBeforeWall()
    {
        var runner = new Runner(new Vector2(35, 35));

        runner.Update(OpenCross, Direction.Right, 1f);

        Assert.IsTrue(runner.Position.X <= 56.001f);
        Assert.IsTrue(OpenCross.CanOccupy(runner.Position, runner.CollisionRadius, runner.TileSize));
        Assert.AreEqual(Direction.None, runner.MovingDirection);
    }

    [TestMethod]
    public void Update_BuffersTurnUntilJunction()
    {
        var maze = Maze.FromAscii(
        [
            "#######",
            "#####.#",
            "#####.#",
            "#.....#",
            "#######"
        ]);
        var runner = new Runner(new Vector2(15, 35));
        runner.Update(maze, Direction.Right, 0.15f);

        runner.Update(maze, Direction.Up, 0.8f);

        Assert.IsTrue(runner.Position.X > 50, $"Position was {runner.Position}.");
        Assert.IsTrue(runner.Position.Y < 35, $"Position was {runner.Position}.");
        Assert.AreEqual(Direction.Up, runner.MovingDirection);
    }

    [TestMethod]
    public void Update_AllowsImmediateReversal()
    {
        var runner = new Runner(new Vector2(35, 35));
        runner.Update(OpenCross, Direction.Right, 0.1f);
        var rightmost = runner.Position.X;

        runner.Update(OpenCross, Direction.Left, 0.05f);

        Assert.IsLessThan(rightmost, runner.Position.X);
        Assert.AreEqual(Direction.Left, runner.Facing);
    }

    [TestMethod]
    public void Respawn_ResetsMotion()
    {
        var runner = new Runner(new Vector2(35, 35));
        runner.Update(OpenCross, Direction.Right, 0.05f);

        runner.Respawn(new Vector2(35, 35));

        Assert.AreEqual(new Vector2(35, 35), runner.Position);
        Assert.AreEqual(Direction.None, runner.MovingDirection);
        Assert.AreEqual(0, runner.AnimationFrame);
    }
}
