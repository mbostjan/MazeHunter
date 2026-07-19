using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Combat;

namespace MazeHunter.Core.Enemies;

public readonly record struct EnemyContext(
    Vector2 PlayerPosition,
    Direction PlayerFacing,
    ProjectileSystem? Projectiles = null);

