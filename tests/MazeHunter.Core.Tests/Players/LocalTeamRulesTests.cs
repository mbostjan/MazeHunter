using MazeHunter.Core.Players;
using MazeHunter.Core.State;

namespace MazeHunter.Core.Tests.Players;

[TestClass]
public sealed class LocalTeamRulesTests
{
    [TestMethod]
    public void CooperativeRun_ContinuesWhileEitherPlayerHasLives()
    {
        var playerOne = Eliminate(new PlayerLife());
        var playerTwo = new PlayerLife();

        Assert.IsFalse(LocalTeamRules.IsRunOver(GameMode.Cooperative, playerOne, playerTwo));
        Assert.IsTrue(LocalTeamRules.ShouldRecoverAtNextRound(
            GameMode.Cooperative,
            playerOne,
            playerTwo));
    }

    [TestMethod]
    public void CooperativeRun_EndsOnlyWhenBothPlayersAreEliminated()
    {
        var playerOne = Eliminate(new PlayerLife());
        var playerTwo = Eliminate(new PlayerLife());

        Assert.IsTrue(LocalTeamRules.IsRunOver(GameMode.Cooperative, playerOne, playerTwo));
    }

    [TestMethod]
    public void FriendlyFire_IsDisabled()
    {
        Assert.IsFalse(LocalTeamRules.FriendlyFireEnabled);
    }

    private static PlayerLife Eliminate(PlayerLife life)
    {
        for (var hit = 0; hit < PlayerLife.StartingLives; hit++)
        {
            life.TryDamage();
            if (!life.IsGameOver)
            {
                life.Update(PlayerLife.RespawnDelaySeconds);
                life.CompleteRespawn();
                life.Update(PlayerLife.ProtectionSeconds);
            }
        }

        return life;
    }
}

