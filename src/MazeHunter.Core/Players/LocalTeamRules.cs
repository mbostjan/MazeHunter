using MazeHunter.Core.State;

namespace MazeHunter.Core.Players;

public static class LocalTeamRules
{
    public static bool FriendlyFireEnabled => false;

    public static bool IsRunOver(GameMode mode, PlayerLife playerOne, PlayerLife playerTwo)
    {
        ArgumentNullException.ThrowIfNull(playerOne);
        ArgumentNullException.ThrowIfNull(playerTwo);
        return playerOne.IsGameOver && (mode == GameMode.Solo || playerTwo.IsGameOver);
    }

    public static bool ShouldRecoverAtNextRound(
        GameMode mode,
        PlayerLife eliminated,
        PlayerLife partner)
    {
        ArgumentNullException.ThrowIfNull(eliminated);
        ArgumentNullException.ThrowIfNull(partner);
        return mode == GameMode.Cooperative && eliminated.IsGameOver && !partner.IsGameOver;
    }
}
