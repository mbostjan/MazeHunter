using MazeHunter.Core.Timing;

namespace MazeHunter.Core.Tests.Timing;

[TestClass]
public sealed class FixedStepClockTests
{
    [TestMethod]
    public void AddElapsed_AccumulatesPartialSteps()
    {
        var clock = new FixedStepClock(60);

        Assert.AreEqual(0, clock.AddElapsed(1.0 / 120));
        Assert.AreEqual(1, clock.AddElapsed(1.0 / 120));
    }

    [TestMethod]
    public void AddElapsed_ClampsLongFrames()
    {
        var clock = new FixedStepClock(60, 0.25);

        Assert.AreEqual(15, clock.AddElapsed(10));
    }

    [TestMethod]
    public void Reset_DiscardsRemainder()
    {
        var clock = new FixedStepClock(60);
        clock.AddElapsed(1.0 / 120);

        clock.Reset();

        Assert.AreEqual(0, clock.AddElapsed(1.0 / 120));
    }
}

