using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Combat;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Tests.Combat;

[TestClass]
public sealed class ProjectileSystemTests
{
    private static readonly Maze LongCorridor = Maze.FromAscii(
    [
        "#####################",
        "#...................#",
        "#####################"
    ]);
    private static readonly Maze LifetimeCorridor = Maze.FromAscii(
    [
        new string('#', 52),
        "#" + new string('.', 50) + "#",
        new string('#', 52)
    ]);

    [TestMethod]
    public void TryFire_AllowsOnlyOneActiveProjectilePerOwner()
    {
        var system = new ProjectileSystem();

        Assert.IsTrue(system.TryFire(1, new Vector2(12, 12), Direction.Right));
        Assert.IsFalse(system.TryFire(1, new Vector2(12, 12), Direction.Right));
        Assert.IsTrue(system.TryFire(2, new Vector2(12, 12), Direction.Right));
        Assert.AreEqual(2, system.ActiveCount);
    }

    [TestMethod]
    public void Update_MovesAtConfiguredSpeed()
    {
        var system = new ProjectileSystem();
        system.TryFire(1, new Vector2(12, 12), Direction.Right);

        system.Update(LongCorridor, 0.1f);

        Assert.AreEqual(23.2f, system[0].Position.X, 0.001f);
    }

    [TestMethod]
    public void Update_RemovesProjectileAtWall()
    {
        var system = new ProjectileSystem();
        system.TryFire(1, new Vector2(12, 12), Direction.Left);

        system.Update(LongCorridor, 0.1f);

        Assert.AreEqual(0, system.ActiveCount);
        Assert.IsFalse(system[0].Active);
    }

    [TestMethod]
    public void Update_RemovesProjectileAfterMaximumLifetime()
    {
        var system = new ProjectileSystem();
        system.TryFire(1, new Vector2(12, 12), Direction.Right);

        system.Update(LifetimeCorridor, ProjectileSystem.MaximumLifetimeSeconds);

        Assert.AreEqual(0, system.ActiveCount);
    }

    [TestMethod]
    public void TryHitCircle_ConsumesProjectileAndReportsOwner()
    {
        var system = new ProjectileSystem();
        system.TryFire(7, new Vector2(12, 12), Direction.Right);

        var hit = system.TryHitCircle(new Vector2(14, 12), 2, out var owner);

        Assert.IsTrue(hit);
        Assert.AreEqual(7, owner);
        Assert.AreEqual(0, system.ActiveCount);
    }

    [TestMethod]
    public void Update_UsesSubstepsToPreventWallTunneling()
    {
        var system = new ProjectileSystem();
        system.TryFire(1, new Vector2(12, 12), Direction.Right);

        system.Update(LongCorridor, 2f);

        Assert.AreEqual(0, system.ActiveCount);
    }
}
