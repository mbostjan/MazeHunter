namespace MazeHunter.Core.Geometry;

/// <summary>Shared logical dimensions for maze tiles and circular collision bodies.</summary>
public sealed record GameGeometry
{
    public GameGeometry(int tileSize, float actorRadius, float projectileRadius)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tileSize, 4);
        if (!float.IsFinite(actorRadius) || actorRadius <= 0 || actorRadius >= tileSize / 2f)
        {
            throw new ArgumentOutOfRangeException(nameof(actorRadius));
        }

        if (!float.IsFinite(projectileRadius) || projectileRadius <= 0 ||
            projectileRadius >= tileSize / 2f)
        {
            throw new ArgumentOutOfRangeException(nameof(projectileRadius));
        }

        TileSize = tileSize;
        ActorRadius = actorRadius;
        ProjectileRadius = projectileRadius;
    }

    public static GameGeometry Default { get; } = new(10, 4f, 1.25f);

    public int TileSize { get; }

    public float ActorRadius { get; }

    public float ProjectileRadius { get; }

    public float TileCenterOffset => TileSize / 2f;
}
