using System.Numerics;

namespace MazeHunter.Core.Actors;

public enum Direction : byte
{
    None,
    Up,
    Down,
    Left,
    Right
}

public static class DirectionExtensions
{
    public static Vector2 ToVector(this Direction direction) => direction switch
    {
        Direction.Up => -Vector2.UnitY,
        Direction.Down => Vector2.UnitY,
        Direction.Left => -Vector2.UnitX,
        Direction.Right => Vector2.UnitX,
        _ => Vector2.Zero
    };

    public static bool IsVertical(this Direction direction) =>
        direction is Direction.Up or Direction.Down;

    public static bool IsPerpendicularTo(this Direction direction, Direction other) =>
        direction != Direction.None &&
        other != Direction.None &&
        direction.IsVertical() != other.IsVertical();

    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => Direction.None
    };
}
