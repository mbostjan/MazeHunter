using System.Numerics;
using MazeHunter.Core.Geometry;
using MazeHunter.Core.Levels;
using MazeHunter.Core.Spawning;

namespace MazeHunter.Core.Tests.Levels;

[TestClass]
public sealed class LevelCatalogTests
{
    [TestMethod]
    public void FirstThreeLevels_HaveDistinctValidMazesAndSpawns()
    {
        var signatures = new HashSet<string>();
        var enemyEntries = new HashSet<Vector2>();

        for (var levelNumber = 1; levelNumber <= 3; levelNumber++)
        {
            var level = LevelCatalog.Get(levelNumber);
            var signature = string.Join(
                "",
                Enumerable.Range(0, level.Maze.Height).SelectMany(y =>
                    Enumerable.Range(0, level.Maze.Width).Select(x => (int)level.Maze[x, y])));
            signatures.Add(signature);

            var players = SpawnPlanner.FindPlayerSpawns(level);
            Assert.IsTrue(level.Maze.CanOccupy(
                players.PlayerOne,
                GameGeometry.Default.ActorRadius,
                GameGeometry.Default.TileSize));
            Assert.IsTrue(level.Maze.CanOccupy(
                players.PlayerTwo,
                GameGeometry.Default.ActorRadius,
                GameGeometry.Default.TileSize));
            enemyEntries.Add(SpawnPlanner.FindEnemyEntry(level, 0));
        }

        Assert.HasCount(3, signatures);
        Assert.HasCount(3, enemyEntries);
    }

    [TestMethod]
    public void Catalog_RepeatsLayoutsWithoutRepeatingLevelIdentity()
    {
        Assert.AreSame(LevelCatalog.Get(1).Maze, LevelCatalog.Get(4).Maze);
        Assert.AreEqual("Signal Crossing", LevelCatalog.Get(4).Name);
    }
}
