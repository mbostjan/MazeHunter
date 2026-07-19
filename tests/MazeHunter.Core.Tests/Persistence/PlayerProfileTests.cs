using MazeHunter.Core.Persistence;
using MazeHunter.Core.State;

namespace MazeHunter.Core.Tests.Persistence;

[TestClass]
public sealed class PlayerProfileTests
{
    [TestMethod]
    public void Json_RoundTripsSettingsAndScores()
    {
        var profile = new PlayerProfile();
        profile.Settings.Muted = true;
        profile.Settings.LastMode = GameMode.Cooperative;
        profile.Settings.PlayerOneCallsign = "NOVA";
        profile.Settings.HighContrast = true;
        profile.Settings.ReducedFlashes = true;
        profile.AddHighScore("NOVA", 12345, 7, GameMode.Cooperative, DateTimeOffset.UnixEpoch);

        var json = ProfileJson.Serialize(profile);
        var restored = ProfileJson.DeserializeOrDefault(json, out var recovered);

        Assert.IsFalse(recovered);
        Assert.IsTrue(restored.Settings.Muted);
        Assert.AreEqual(GameMode.Cooperative, restored.Settings.LastMode);
        Assert.AreEqual("NOVA", restored.Settings.PlayerOneCallsign);
        Assert.IsTrue(restored.Settings.HighContrast);
        Assert.IsTrue(restored.Settings.ReducedFlashes);
        Assert.AreEqual(12345, restored.HighScores[0].Score);
        Assert.AreEqual(DateTimeOffset.UnixEpoch, restored.HighScores[0].AchievedAtUtc);
    }

    [TestMethod]
    public void CorruptedJson_ReturnsUsableDefaults()
    {
        var profile = ProfileJson.DeserializeOrDefault("{ definitely broken", out var recovered);

        Assert.IsTrue(recovered);
        Assert.AreEqual(PlayerProfile.CurrentVersion, profile.Version);
        Assert.AreEqual(GameMode.Solo, profile.Settings.LastMode);
        Assert.IsEmpty(profile.HighScores);
    }

    [TestMethod]
    public void FutureVersion_ReturnsDefaultsRatherThanGuessing()
    {
        var profile = ProfileJson.DeserializeOrDefault(
            """{"version":999,"settings":{},"highScores":[]}""",
            out var recovered);

        Assert.IsTrue(recovered);
        Assert.AreEqual(PlayerProfile.CurrentVersion, profile.Version);
    }

    [TestMethod]
    public void OlderVersion_IsNormalizedToCurrentSchema()
    {
        var profile = ProfileJson.DeserializeOrDefault(
            """{"version":0,"settings":{"playerOneCallsign":" old! "},"highScores":[]}""",
            out var recovered);

        Assert.IsFalse(recovered);
        Assert.AreEqual(PlayerProfile.CurrentVersion, profile.Version);
        Assert.AreEqual("OLD", profile.Settings.PlayerOneCallsign);
    }

    [TestMethod]
    public void Leaderboard_SortsAndTrimsToTopTen()
    {
        var profile = new PlayerProfile();
        for (var score = 1; score <= 15; score++)
        {
            profile.AddHighScore("R", score, 1, GameMode.Solo, DateTimeOffset.UnixEpoch);
        }

        Assert.HasCount(PlayerProfile.MaximumHighScores, profile.HighScores);
        Assert.AreEqual(15, profile.HighScores[0].Score);
        Assert.AreEqual(6, profile.HighScores[^1].Score);
        Assert.IsFalse(profile.QualifiesForHighScore(6));
        Assert.IsTrue(profile.QualifiesForHighScore(7));
    }

    [TestMethod]
    public void Callsign_IsUppercaseAlphanumericAndLengthLimited()
    {
        Assert.AreEqual("AB12CD34", PlayerProfile.SanitizeCallsign(" ab-12_cd 345 "));
        Assert.AreEqual("RUNNER", PlayerProfile.SanitizeCallsign("***"));
    }
}
