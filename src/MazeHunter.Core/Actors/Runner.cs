using System.Numerics;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Actors;

/// <summary>Deterministic axis-aligned player movement independent of presentation.</summary>
public sealed class Runner
{
    public const float CollisionRadius = 3f;
    public const float MovementSpeed = 48f;
    public const int TileSize = 8;

    private Direction _bufferedDirection;
    private float _animationDistance;

    public Runner(Vector2 spawnPosition)
    {
        Position = spawnPosition;
    }

    public Vector2 Position { get; private set; }

    public Direction Facing { get; private set; } = Direction.Right;

    public Direction MovingDirection { get; private set; }

    public int AnimationFrame => (int)(_animationDistance / 4f) & 1;

    public void Update(Maze maze, Direction requestedDirection, float deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(maze);
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (requestedDirection != Direction.None)
        {
            _bufferedDirection = requestedDirection;
            Facing = requestedDirection;
        }

        var remaining = MovementSpeed * deltaSeconds;
        while (remaining > 0)
        {
            var distance = MathF.Min(remaining, 1f);
            if (TryMoveBufferedDirection(maze, distance))
            {
                MovingDirection = _bufferedDirection;
            }
            else if (TryMove(maze, MovingDirection, distance))
            {
                // Continue along the corridor until the buffered turn opens.
            }
            else
            {
                MovingDirection = Direction.None;
                break;
            }

            _animationDistance += distance;
            remaining -= distance;
        }
    }

    public void Respawn(Vector2 position)
    {
        Position = position;
        MovingDirection = Direction.None;
        _bufferedDirection = Direction.None;
        _animationDistance = 0;
    }

    private bool TryMove(Maze maze, Direction direction, float distance)
    {
        if (direction == Direction.None)
        {
            return false;
        }

        var candidate = Position + (direction.ToVector() * distance);
        if (!maze.CanOccupy(candidate, CollisionRadius, TileSize))
        {
            return false;
        }

        Position = candidate;
        return true;
    }

    private bool TryMoveBufferedDirection(Maze maze, float distance)
    {
        if (!_bufferedDirection.IsPerpendicularTo(MovingDirection))
        {
            return TryMove(maze, _bufferedDirection, distance);
        }

        var centered = Position;
        if (_bufferedDirection.IsVertical())
        {
            var centerX = (MathF.Floor(Position.X / TileSize) * TileSize) + (TileSize / 2f);
            if (MathF.Abs(centerX - Position.X) > distance)
            {
                return false;
            }

            centered.X = centerX;
        }
        else
        {
            var centerY = (MathF.Floor(Position.Y / TileSize) * TileSize) + (TileSize / 2f);
            if (MathF.Abs(centerY - Position.Y) > distance)
            {
                return false;
            }

            centered.Y = centerY;
        }

        var currentTileX = (int)MathF.Floor(centered.X / TileSize);
        var currentTileY = (int)MathF.Floor(centered.Y / TileSize);
        var directionVector = _bufferedDirection.ToVector();
        var destinationTileX = currentTileX + (int)directionVector.X;
        var destinationTileY = currentTileY + (int)directionVector.Y;
        if (!maze.IsWalkable(destinationTileX, destinationTileY))
        {
            return false;
        }

        var candidate = centered + (directionVector * distance);
        if (!maze.CanOccupy(candidate, CollisionRadius, TileSize))
        {
            return false;
        }

        Position = candidate;
        return true;
    }
}
