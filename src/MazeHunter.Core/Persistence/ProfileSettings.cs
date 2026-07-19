using MazeHunter.Core.State;

namespace MazeHunter.Core.Persistence;

public sealed class ProfileSettings
{
    public bool Muted { get; set; }

    public GameMode LastMode { get; set; } = GameMode.Solo;

    public string PlayerOneCallsign { get; set; } = "P1";

    public string PlayerTwoCallsign { get; set; } = "P2";

    public bool HighContrast { get; set; }

    public bool ReducedFlashes { get; set; }
}

