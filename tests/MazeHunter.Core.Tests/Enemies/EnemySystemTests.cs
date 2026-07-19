using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Combat;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Geometry;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Tests.Enemies;

[TestClass]
public sealed class EnemySystemTests
{
    private static readonly Maze LoopMaze = Maze.FromAscii(
    [
        "#######",
        "#.....#",
        "#.###.#",
        "#.....#",
        "#######"
    ]);

    [TestMethod]
    public void Update_KeepsDrifterInsideWalkableCorridors()
    {
        var enemies = new EnemySystem(seed: 42);
        enemies.TrySpawnDrifter(new Vector2(15, 15));

        for (var i = 0; i < 600; i++)
        {
            enemies.Update(LoopMaze, 1f / 60);
            Assert.IsTrue(LoopMaze.CanOccupy(
                enemies[0].Position,
                enemies.CollisionRadius,
                GameGeometry.Default.TileSize));
        }
    }

    [TestMethod]
    public void SameSeed_ProducesIdenticalNavigation()
    {
        var first = new EnemySystem(seed: 77);
        var second = new EnemySystem(seed: 77);
        first.TrySpawnDrifter(new Vector2(15, 15));
        second.TrySpawnDrifter(new Vector2(15, 15));

        for (var i = 0; i < 300; i++)
        {
            first.Update(LoopMaze, 1f / 60);
            second.Update(LoopMaze, 1f / 60);
        }

        Assert.AreEqual(first[0].Position, second[0].Position);
        Assert.AreEqual(first[0].Direction, second[0].Direction);
    }

    [TestMethod]
    public void Drifter_DoesNotReverseWhenForwardRouteExists()
    {
        var enemies = new EnemySystem(seed: 3);
        enemies.TrySpawnDrifter(new Vector2(15, 15));
        enemies.Update(LoopMaze, 0.01f);
        var initialDirection = enemies[0].Direction;

        for (var i = 0; i < 10; i++)
        {
            enemies.Update(LoopMaze, 0.1f);
            Assert.AreNotEqual(initialDirection.Opposite(), enemies[0].Direction);
        }
    }

    [TestMethod]
    public void TryDestroyWithProjectiles_ConsumesBothEntities()
    {
        var enemies = new EnemySystem(seed: 1);
        var projectiles = new ProjectileSystem();
        enemies.TrySpawnDrifter(new Vector2(20, 12));
        projectiles.TryFire(9, new Vector2(20, 12), Direction.Right);

        var destroyed = enemies.TryDestroyWithProjectiles(projectiles, out var owner);

        Assert.IsTrue(destroyed);
        Assert.AreEqual(9, owner);
        Assert.AreEqual(0, enemies.ActiveCount);
        Assert.AreEqual(0, projectiles.ActiveCount);
    }

    [TestMethod]
    public void DestroyResult_ReportsEffectPosition()
    {
        var enemies = new EnemySystem(seed: 1);
        var projectiles = new ProjectileSystem();
        var position = new Vector2(20, 12);
        enemies.TrySpawnDrifter(position);
        projectiles.TryFire(1, position, Direction.Right);

        enemies.TryDestroyWithProjectiles(projectiles, out _, out _, out var destroyedPosition);

        Assert.AreEqual(position, destroyedPosition);
    }

    [TestMethod]
    public void Capacity_PreventsUnboundedSpawning()
    {
        var enemies = new EnemySystem(seed: 1, capacity: 1);

        Assert.IsTrue(enemies.TrySpawnDrifter(new Vector2(12, 12)));
        Assert.IsFalse(enemies.TrySpawnDrifter(new Vector2(20, 12)));
        Assert.AreEqual(1, enemies.ActiveCount);
    }

    [TestMethod]
    public void HasContact_DetectsTouchingButNotDistantEnemy()
    {
        var enemies = new EnemySystem(seed: 1);
        enemies.TrySpawnDrifter(new Vector2(20, 12));

        Assert.IsTrue(enemies.HasContact(new Vector2(14, 12), 3));
        Assert.IsFalse(enemies.HasContact(new Vector2(40, 12), 3));
    }

    [TestMethod]
    public void EveryLevelEntry_LeavesSpawnWithinOneSecondAndKeepsMoving()
    {
        for (var levelNumber = 1; levelNumber <= 3; levelNumber++)
        {
            var level = MazeHunter.Core.Levels.LevelCatalog.Get(levelNumber);
            for (var entryIndex = 0; entryIndex < level.EnemyEntries.Count; entryIndex++)
            {
                foreach (var kind in Enum.GetValues<EnemyKind>())
                {
                    var enemies = new EnemySystem(seed: (uint)(100 + levelNumber + entryIndex));
                    var spawn = MazeHunter.Core.Spawning.SpawnPlanner.FindEnemyEntry(level, entryIndex);
                    enemies.TrySpawn(kind, spawn);

                    for (var update = 0; update < 60; update++)
                    {
                        enemies.Update(level.Maze, 1f / 60f);
                    }

                    var afterOneSecond = enemies[0].Position;
                    Assert.IsGreaterThan(
                        4f,
                        Vector2.DistanceSquared(spawn, afterOneSecond),
                        $"Level {levelNumber}, entry {entryIndex}, {kind} did not leave spawn.");

                    for (var update = 0; update < 120; update++)
                    {
                        enemies.Update(level.Maze, 1f / 60f);
                    }

                    Assert.AreNotEqual(
                        afterOneSecond,
                        enemies[0].Position,
                        $"Level {levelNumber}, entry {entryIndex}, {kind} stopped navigating.");
                }
            }
        }
    }
}
