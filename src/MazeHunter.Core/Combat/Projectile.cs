using System.Numerics;
using MazeHunter.Core.Actors;

namespace MazeHunter.Core.Combat;

public readonly record struct Projectile(
    bool Active,
    int OwnerId,
    Vector2 Position,
    Direction Direction,
    float AgeSeconds);

