using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Combat;

/// <summary>Fixed-capacity, allocation-free directional pulse simulation.</summary>
public sealed class ProjectileSystem
{
    public const float Speed = 112f;
    public const float Radius = 1f;
    public const float MaximumLifetimeSeconds = 2.5f;

    private readonly Projectile[] _projectiles;

    public ProjectileSystem(int capacity = 8)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _projectiles = new Projectile[capacity];
    }

    public int Capacity => _projectiles.Length;

    public int ActiveCount { get; private set; }

    public Projectile this[int index] => _projectiles[index];

    public bool TryFire(int ownerId, Vector2 origin, Direction direction)
    {
        if (direction == Direction.None || HasActiveProjectile(ownerId))
        {
            return false;
        }

        for (var i = 0; i < _projectiles.Length; i++)
        {
            if (_projectiles[i].Active)
            {
                continue;
            }

            _projectiles[i] = new Projectile(true, ownerId, origin, direction, 0);
            ActiveCount++;
            return true;
        }

        return false;
    }

    public void Update(Maze maze, float deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(maze);
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        for (var i = 0; i < _projectiles.Length; i++)
        {
            var projectile = _projectiles[i];
            if (!projectile.Active)
            {
                continue;
            }

            var age = projectile.AgeSeconds + deltaSeconds;
            if (age >= MaximumLifetimeSeconds)
            {
                Deactivate(i);
                continue;
            }

            var position = projectile.Position;
            var remaining = Speed * deltaSeconds;
            var collided = false;
            while (remaining > 0)
            {
                var distance = MathF.Min(remaining, 1f);
                var candidate = position + (projectile.Direction.ToVector() * distance);
                if (!maze.CanOccupy(candidate, Radius, Runner.TileSize))
                {
                    collided = true;
                    break;
                }

                position = candidate;
                remaining -= distance;
            }

            if (collided)
            {
                Deactivate(i);
            }
            else
            {
                _projectiles[i] = projectile with { Position = position, AgeSeconds = age };
            }
        }
    }

    public bool TryHitCircle(Vector2 center, float radius, out int ownerId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
        var hitDistanceSquared = (radius + Radius) * (radius + Radius);
        for (var i = 0; i < _projectiles.Length; i++)
        {
            var projectile = _projectiles[i];
            if (!projectile.Active || Vector2.DistanceSquared(projectile.Position, center) > hitDistanceSquared)
            {
                continue;
            }

            ownerId = projectile.OwnerId;
            Deactivate(i);
            return true;
        }

        ownerId = -1;
        return false;
    }

    public void Clear()
    {
        Array.Clear(_projectiles);
        ActiveCount = 0;
    }

    private bool HasActiveProjectile(int ownerId)
    {
        for (var i = 0; i < _projectiles.Length; i++)
        {
            if (_projectiles[i].Active && _projectiles[i].OwnerId == ownerId)
            {
                return true;
            }
        }

        return false;
    }

    private void Deactivate(int index)
    {
        _projectiles[index] = default;
        ActiveCount--;
    }
}

