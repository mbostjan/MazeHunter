using MazeHunter.Core.Players;

namespace MazeHunter.Core.Tests.Players;

[TestClass]
public sealed class PlayerLifeTests
{
    [TestMethod]
    public void ThreeHits_ReachGameOver()
    {
        var life = new PlayerLife();

        for (var hit = 0; hit < 3; hit++)
        {
            Assert.IsTrue(life.TryDamage());
            if (hit < 2)
            {
                life.Update(PlayerLife.RespawnDelaySeconds);
                life.CompleteRespawn();
                life.Update(PlayerLife.ProtectionSeconds);
            }
        }

        Assert.AreEqual(0, life.Lives);
        Assert.IsTrue(life.IsGameOver);
        Assert.IsFalse(life.IsAlive);
    }

    [TestMethod]
    public void Damage_RequiresRespawnDelay()
    {
        var life = new PlayerLife();
        life.TryDamage();

        Assert.IsFalse(life.Update(PlayerLife.RespawnDelaySeconds - 0.01f));
        Assert.IsTrue(life.Update(0.02f));
        life.CompleteRespawn();

        Assert.IsTrue(life.IsAlive);
        Assert.IsTrue(life.IsProtected);
    }

    [TestMethod]
    public void Protection_PreventsImmediateRepeatedDamage()
    {
        var life = new PlayerLife();
        life.TryDamage();
        life.Update(PlayerLife.RespawnDelaySeconds);
        life.CompleteRespawn();

        Assert.IsFalse(life.TryDamage());
        life.Update(PlayerLife.ProtectionSeconds);
        Assert.IsTrue(life.TryDamage());
    }

    [TestMethod]
    public void Reset_RestoresInitialState()
    {
        var life = new PlayerLife();
        life.TryDamage();

        life.Reset();

        Assert.AreEqual(PlayerLife.StartingLives, life.Lives);
        Assert.IsTrue(life.IsAlive);
        Assert.IsFalse(life.IsProtected);
    }

    [TestMethod]
    public void CycleRecovery_ReturnsEliminatedPlayerWithOneProtectedLife()
    {
        var life = new PlayerLife();
        for (var hit = 0; hit < 3; hit++)
        {
            life.TryDamage();
            if (hit < 2)
            {
                life.Update(PlayerLife.RespawnDelaySeconds);
                life.CompleteRespawn();
                life.Update(PlayerLife.ProtectionSeconds);
            }
        }

        life.ReviveForNextRound();

        Assert.AreEqual(1, life.Lives);
        Assert.IsTrue(life.IsAlive);
        Assert.IsTrue(life.IsProtected);
    }
}
