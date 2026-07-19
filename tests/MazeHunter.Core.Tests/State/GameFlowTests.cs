using MazeHunter.Core.State;

namespace MazeHunter.Core.Tests.State;

[TestClass]
public sealed class GameFlowTests
{
    [TestMethod]
    public void NewFlow_StartsAtTitleWithSimulationStopped()
    {
        var flow = new GameFlow();

        Assert.AreEqual(GameScreen.Title, flow.Screen);
        Assert.IsFalse(flow.SimulationRunning);
    }

    [TestMethod]
    public void Instructions_ReturnsToTitle()
    {
        var flow = new GameFlow();

        flow.ShowInstructions();
        Assert.AreEqual(GameScreen.Instructions, flow.Screen);
        flow.ReturnToTitle();

        Assert.AreEqual(GameScreen.Title, flow.Screen);
    }

    [TestMethod]
    public void Playing_CanPauseAndResumeWithoutChangingRun()
    {
        var flow = new GameFlow();
        flow.StartGame();

        flow.TogglePause();
        Assert.AreEqual(GameScreen.Paused, flow.Screen);
        Assert.IsFalse(flow.SimulationRunning);
        flow.TogglePause();

        Assert.AreEqual(GameScreen.Playing, flow.Screen);
        Assert.IsTrue(flow.SimulationRunning);
    }

    [TestMethod]
    public void GameOver_CanRestartOrReturnToTitle()
    {
        var flow = new GameFlow();
        flow.StartGame();
        flow.EndGame();
        Assert.AreEqual(GameScreen.GameOver, flow.Screen);

        flow.StartGame();
        Assert.AreEqual(GameScreen.Playing, flow.Screen);
        flow.EndGame();
        flow.ReturnToTitle();

        Assert.AreEqual(GameScreen.Title, flow.Screen);
    }

    [TestMethod]
    public void InvalidTransitions_DoNotBypassFlow()
    {
        var flow = new GameFlow();

        flow.TogglePause();
        flow.EndGame();

        Assert.AreEqual(GameScreen.Title, flow.Screen);
    }

    [TestMethod]
    public void StartGame_RecordsSelectedLocalMode()
    {
        var flow = new GameFlow();

        flow.StartGame(GameMode.Cooperative);

        Assert.AreEqual(GameMode.Cooperative, flow.Mode);
    }
}
