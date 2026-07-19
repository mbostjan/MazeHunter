using MazeHunter.Core.Diagnostics;

namespace MazeHunter.Core.Tests.Diagnostics;

[TestClass]
public sealed class DeterministicRandomTests
{
    [TestMethod]
    public void SameSeed_ProducesSameSequence()
    {
        var first = new DeterministicRandom(1234);
        var second = new DeterministicRandom(1234);

        for (var i = 0; i < 100; i++)
        {
            Assert.AreEqual(first.NextUInt(), second.NextUInt());
        }
    }
}

