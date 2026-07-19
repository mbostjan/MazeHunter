using System.Numerics;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Mazes;
using MazeHunter.Core.Rounds;

namespace MazeHunter.Core.Tests.Rounds;

[TestClass]
public sealed class RoundDirectorTests
{
    [TestMethod]
    public void Update_SpawnsOnlyWhenPlayerIsSafelyDistant()
    {
        var maze = MazeCatalog.CreateSignalCrossing();
        var enemies = new EnemySystem(seed: 1);
        var director = new RoundDirector();
        var entry = new Vector2((maze.Width / 2 * 8) + 4, 12);

        director.Update(maze, enemies, entry, 2f);
        Assert.AreEqual(0, enemies.ActiveCount);

        director.Update(maze, enemies, new Vector2(12, 156), 0.3f);
        Assert.AreEqual(1, enemies.ActiveCount);
    }

    [TestMethod]
    public void CompletingQuota_AdvancesAndExpandsRound()
    {
        var maze = MazeCatalog.CreateSignalCrossing();
        var enemies = new EnemySystem(seed: 1);
        var director = new RoundDirector();
        var player = new Vector2(12, 156);
        var firstQuota = director.RequiredDefeats;

        for (var defeated = 0; defeated < firstQuota; defeated++)
        {
            director.Update(maze, enemies, player, 2f);
            Assert.AreEqual(1, enemies.ActiveCount);
            enemies.Clear();
            director.NotifyEnemyDefeated();
        }

        director.Update(maze, enemies, player, 1.6f);

        Assert.AreEqual(2, director.RoundNumber);
        Assert.IsGreaterThan(firstQuota, director.RequiredDefeats);
        Assert.AreEqual(0, director.DefeatedThisRound);
    }
}

