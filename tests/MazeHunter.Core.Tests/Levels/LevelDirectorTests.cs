using System.Numerics;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Levels;

namespace MazeHunter.Core.Tests.Levels;

[TestClass]
public sealed class LevelDirectorTests
{
    [TestMethod]
    public void Update_SpawnsOnlyWhenPlayerIsSafelyDistant()
    {
        var enemies = new EnemySystem(seed: 1);
        var director = new LevelDirector();
        var entry = new Vector2(155, 15);

        director.Update(enemies, entry, 2f);
        Assert.AreEqual(0, enemies.ActiveCount);

        director.Update(enemies, new Vector2(25, 195), 0.3f);
        Assert.AreEqual(1, enemies.ActiveCount);
    }

    [TestMethod]
    public void CompletingQuota_AdvancesToDifferentLevel()
    {
        var enemies = new EnemySystem(seed: 1);
        var director = new LevelDirector();
        var player = new Vector2(25, 195);
        var firstQuota = director.RequiredDefeats;
        var firstMaze = director.CurrentLevel.Maze;

        for (var defeated = 0; defeated < firstQuota; defeated++)
        {
            director.Update(enemies, player, 2f);
            Assert.AreEqual(1, enemies.ActiveCount);
            enemies.Clear();
            director.NotifyEnemyDefeated();
        }

        director.Update(enemies, player, 1.6f);

        Assert.AreEqual(2, director.LevelNumber);
        Assert.IsGreaterThan(firstQuota, director.RequiredDefeats);
        Assert.AreEqual(0, director.DefeatedThisLevel);
        Assert.AreNotSame(firstMaze, director.CurrentLevel.Maze);
    }
}
