using MazeHunter.Core.State;

namespace MazeHunter.Core.Persistence;

public sealed class HighScoreEntry
{
    public string Callsign { get; set; } = "RUNNER";

    public int Score { get; set; }

    public int Round { get; set; } = 1;

    public GameMode Mode { get; set; }

    public DateTimeOffset AchievedAtUtc { get; set; }
}

