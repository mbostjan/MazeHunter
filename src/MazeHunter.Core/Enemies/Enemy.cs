using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Geometry;

namespace MazeHunter.Core.Enemies;

public readonly record struct Enemy(
    bool Active,
    int Id,
    EnemyKind Kind,
    Vector2 Position,
    Direction Direction,
    float DistanceTravelled,
    GridPoint LastDecisionTile)
{
    public int AnimationFrame => (int)(DistanceTravelled / 4f) & 1;
}

