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
        var runner = new Runner(new Vector2(28, 28));

        runner.Update(OpenCross, Direction.Right, 0.05f);

        Assert.AreEqual(30.4f, runner.Position.X, 0.001f);
        Assert.AreEqual(28f, runner.Position.Y);
        Assert.AreEqual(Direction.Right, runner.MovingDirection);
    }

    [TestMethod]
    public void Update_StopsBeforeWall()
    {
        var runner = new Runner(new Vector2(28, 28));

        runner.Update(OpenCross, Direction.Right, 1f);

        Assert.IsTrue(runner.Position.X <= 45.001f);
        Assert.IsTrue(OpenCross.CanOccupy(runner.Position, Runner.CollisionRadius, Runner.TileSize));
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
        var runner = new Runner(new Vector2(12, 28));
        runner.Update(maze, Direction.Right, 0.15f);

        runner.Update(maze, Direction.Up, 0.6f);

        Assert.IsTrue(runner.Position.X > 40, $"Position was {runner.Position}.");
        Assert.IsTrue(runner.Position.Y < 28, $"Position was {runner.Position}.");
        Assert.AreEqual(Direction.Up, runner.MovingDirection);
    }

    [TestMethod]
    public void Update_AllowsImmediateReversal()
    {
        var runner = new Runner(new Vector2(28, 28));
        runner.Update(OpenCross, Direction.Right, 0.1f);
        var rightmost = runner.Position.X;

        runner.Update(OpenCross, Direction.Left, 0.05f);

        Assert.IsLessThan(rightmost, runner.Position.X);
        Assert.AreEqual(Direction.Left, runner.Facing);
    }

    [TestMethod]
    public void Respawn_ResetsMotion()
    {
        var runner = new Runner(new Vector2(28, 28));
        runner.Update(OpenCross, Direction.Right, 0.05f);

        runner.Respawn(new Vector2(28, 28));

        Assert.AreEqual(new Vector2(28, 28), runner.Position);
        Assert.AreEqual(Direction.None, runner.MovingDirection);
        Assert.AreEqual(0, runner.AnimationFrame);
    }
}
