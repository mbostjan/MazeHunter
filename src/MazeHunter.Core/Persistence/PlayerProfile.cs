using MazeHunter.Core.State;

namespace MazeHunter.Core.Persistence;

public sealed class PlayerProfile
{
    public const int CurrentVersion = 1;
    public const int MaximumHighScores = 10;

    public int Version { get; set; } = CurrentVersion;

    public ProfileSettings Settings { get; set; } = new();

    public List<HighScoreEntry> HighScores { get; set; } = [];

    public bool QualifiesForHighScore(int score) =>
        score > 0 &&
        (HighScores.Count < MaximumHighScores || score > HighScores[^1].Score);

    public void AddHighScore(
        string callsign,
        int score,
        int round,
        GameMode mode,
        DateTimeOffset achievedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(score);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);
        HighScores.Add(new HighScoreEntry
        {
            Callsign = SanitizeCallsign(callsign),
            Score = score,
            Round = round,
            Mode = mode,
            AchievedAtUtc = achievedAtUtc.ToUniversalTime()
        });
        HighScores.Sort(static (left, right) =>
        {
            var scoreOrder = right.Score.CompareTo(left.Score);
            return scoreOrder != 0
                ? scoreOrder
                : left.AchievedAtUtc.CompareTo(right.AchievedAtUtc);
        });
        if (HighScores.Count > MaximumHighScores)
        {
            HighScores.RemoveRange(MaximumHighScores, HighScores.Count - MaximumHighScores);
        }
    }

    public void Normalize()
    {
        Version = CurrentVersion;
        Settings ??= new ProfileSettings();
        Settings.PlayerOneCallsign = SanitizeCallsign(Settings.PlayerOneCallsign);
        Settings.PlayerTwoCallsign = SanitizeCallsign(Settings.PlayerTwoCallsign);
        HighScores ??= [];
        HighScores.RemoveAll(entry => entry is null || entry.Score < 0 || entry.Round < 1);
        foreach (var entry in HighScores)
        {
            entry.Callsign = SanitizeCallsign(entry.Callsign);
        }

        HighScores.Sort(static (left, right) => right.Score.CompareTo(left.Score));
        if (HighScores.Count > MaximumHighScores)
        {
            HighScores.RemoveRange(MaximumHighScores, HighScores.Count - MaximumHighScores);
        }
    }

    public static string SanitizeCallsign(string? callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return "RUNNER";
        }

        Span<char> sanitized = stackalloc char[8];
        var length = 0;
        foreach (var character in callsign.Trim().ToUpperInvariant())
        {
            if (length == sanitized.Length)
            {
                break;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                sanitized[length++] = character;
            }
        }

        return length == 0 ? "RUNNER" : new string(sanitized[..length]);
    }
}

