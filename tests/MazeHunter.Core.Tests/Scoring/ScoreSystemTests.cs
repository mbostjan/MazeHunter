using MazeHunter.Core.Enemies;
using MazeHunter.Core.Scoring;

namespace MazeHunter.Core.Tests.Scoring;

[TestClass]
public sealed class ScoreSystemTests
{
    [TestMethod]
    public void EnemyValues_ReflectBehaviorAndRarity()
    {
        Assert.AreEqual(100, ScoreSystem.GetBaseValue(EnemyKind.Drifter));
        Assert.AreEqual(300, ScoreSystem.GetBaseValue(EnemyKind.Surge));
        Assert.AreEqual(750, ScoreSystem.GetBaseValue(EnemyKind.Prism));
    }

    [TestMethod]
    public void ConsecutiveHits_BuildCappedMultiplier()
    {
        var scoring = new ScoreSystem();

        for (var i = 0; i < 20; i++)
        {
            scoring.RecordEnemyDestroyed(EnemyKind.Drifter);
        }

        Assert.AreEqual(4, scoring.Multiplier);
        Assert.IsGreaterThan(2000, scoring.Score);
    }

    [TestMethod]
    public void Chain_ExpiresAfterWindow()
    {
        var scoring = new ScoreSystem();
        scoring.RecordEnemyDestroyed(EnemyKind.Tracer);

        scoring.Update(ScoreSystem.ChainWindowSeconds);

        Assert.AreEqual(0, scoring.Chain);
        Assert.AreEqual(1, scoring.Multiplier);
    }

    [TestMethod]
    public void RoundBonus_IncludesRoundAndSurvivingLives()
    {
        var scoring = new ScoreSystem();

        var awarded = scoring.RecordRoundCompleted(3, 2);

        Assert.AreEqual(1700, awarded);
        Assert.AreEqual(1700, scoring.Score);
    }

    [TestMethod]
    public void TeamSurvivalBonus_ScalesByRound()
    {
        var scoring = new ScoreSystem();

        Assert.AreEqual(1000, scoring.RecordTeamSurvivalBonus(4));
        Assert.AreEqual(1000, scoring.Score);
    }
}
