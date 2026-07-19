using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Combat;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Tests.Enemies;

[TestClass]
public sealed class AdvancedEnemyBehaviorTests
{
    private static readonly Maze CrossMaze = Maze.FromAscii(
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
    public void Tracer_UsesShortestRouteTowardPlayer()
    {
        var enemies = new EnemySystem(seed: 1);
        enemies.TrySpawn(EnemyKind.Tracer, new Vector2(15, 35));

        enemies.Update(
            CrossMaze,
            0.25f,
            new EnemyContext(new Vector2(55, 35), Direction.Left));

        Assert.AreEqual(Direction.Right, enemies[0].Direction);
        Assert.IsGreaterThan(15f, enemies[0].Position.X);
    }

    [TestMethod]
    public void Vector_TargetsTilesAheadOfPlayer()
    {
        var enemies = new EnemySystem(seed: 1);
        enemies.TrySpawn(EnemyKind.Vector, new Vector2(35, 15));

        enemies.Update(
            CrossMaze,
            0.1f,
            new EnemyContext(new Vector2(35, 35), Direction.Down));

        Assert.AreEqual(Direction.Down, enemies[0].Direction);
    }

    [TestMethod]
    public void Veil_AvoidsExposedProjectileLane()
    {
        var projectiles = new ProjectileSystem();
        projectiles.TryFire(1, new Vector2(15, 35), Direction.Right);
        var enemies = new EnemySystem(seed: 1);
        enemies.TrySpawn(EnemyKind.Veil, new Vector2(35, 35));

        enemies.Update(
            CrossMaze,
            0.05f,
            new EnemyContext(new Vector2(55, 35), Direction.Left, projectiles));

        Assert.AreNotEqual(Direction.Right, enemies[0].Direction);
    }

    [TestMethod]
    public void Prism_IncreasesDistanceFromPlayer()
    {
        var enemies = new EnemySystem(seed: 1);
        enemies.TrySpawn(EnemyKind.Prism, new Vector2(35, 35));

        enemies.Update(
            CrossMaze,
            0.1f,
            new EnemyContext(new Vector2(15, 35), Direction.Right));

        Assert.AreNotEqual(Direction.Left, enemies[0].Direction);
        Assert.IsGreaterThan(
            400f,
            Vector2.DistanceSquared(enemies[0].Position, new Vector2(15, 35)));
    }

    [TestMethod]
    public void Surge_IsFasterThanTracer()
    {
        var tracer = new EnemySystem(seed: 1);
        var surge = new EnemySystem(seed: 1);
        tracer.TrySpawn(EnemyKind.Tracer, new Vector2(15, 35));
        surge.TrySpawn(EnemyKind.Surge, new Vector2(15, 35));
        var context = new EnemyContext(new Vector2(55, 35), Direction.Left);

        tracer.Update(CrossMaze, 0.25f, context);
        surge.Update(CrossMaze, 0.25f, context);

        Assert.IsGreaterThan(tracer[0].Position.X, surge[0].Position.X);
    }

    [TestMethod]
    public void Hunter_TargetsNearestLiveRunnerInCooperativeContext()
    {
        var enemies = new EnemySystem(seed: 1);
        enemies.TrySpawn(EnemyKind.Tracer, new Vector2(35, 35));

        enemies.Update(
            CrossMaze,
            0.1f,
            new EnemyContext(
                new Vector2(15, 35),
                Direction.Right,
                SecondPlayerPosition: new Vector2(45, 35),
                SecondPlayerFacing: Direction.Left));

        Assert.AreEqual(Direction.Right, enemies[0].Direction);
    }
}
