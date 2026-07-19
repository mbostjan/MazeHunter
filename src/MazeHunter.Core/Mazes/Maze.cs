using System.Numerics;
using MazeHunter.Core.Geometry;

namespace MazeHunter.Core.Mazes;

/// <summary>Immutable tile maze and authoritative static collision queries.</summary>
public sealed class Maze
{
    private readonly MazeTile[] _tiles;

    private Maze(int width, int height, MazeTile[] tiles)
    {
        Width = width;
        Height = height;
        _tiles = tiles;
    }

    public int Width { get; }

    public int Height { get; }

    public MazeTile this[int x, int y] =>
        IsInside(x, y) ? _tiles[(y * Width) + x] : MazeTile.Wall;

    public bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public bool IsWalkable(int x, int y) => this[x, y] == MazeTile.Floor;

    public bool CanOccupy(Vector2 center, float radius, int tileSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        const float inset = 0.001f;
        var left = (int)MathF.Floor((center.X - radius + inset) / tileSize);
        var right = (int)MathF.Floor((center.X + radius - inset) / tileSize);
        var top = (int)MathF.Floor((center.Y - radius + inset) / tileSize);
        var bottom = (int)MathF.Floor((center.Y + radius - inset) / tileSize);

        return IsWalkable(left, top) &&
               IsWalkable(right, top) &&
               IsWalkable(left, bottom) &&
               IsWalkable(right, bottom);
    }

    public IEnumerable<GridPoint> WalkableTiles()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (IsWalkable(x, y))
                {
                    yield return new GridPoint(x, y);
                }
            }
        }
    }

    public static Maze FromAscii(IReadOnlyList<string> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count < 3)
        {
            throw new ArgumentException("A maze requires at least three rows.", nameof(rows));
        }

        var width = rows[0].Length;
        if (width < 3 || rows.Any(row => row.Length != width))
        {
            throw new ArgumentException("Maze rows must have an equal width of at least three.", nameof(rows));
        }

        var tiles = new MazeTile[width * rows.Count];
        for (var y = 0; y < rows.Count; y++)
        {
            for (var x = 0; x < width; x++)
            {
                tiles[(y * width) + x] = rows[y][x] switch
                {
                    '#' => MazeTile.Wall,
                    '.' => MazeTile.Floor,
                    _ => throw new ArgumentException($"Unsupported maze symbol at ({x}, {y}).", nameof(rows))
                };
            }
        }

        var maze = new Maze(width, rows.Count, tiles);
        maze.Validate();
        return maze;
    }

    private void Validate()
    {
        for (var x = 0; x < Width; x++)
        {
            if (IsWalkable(x, 0) || IsWalkable(x, Height - 1))
            {
                throw new ArgumentException("Maze boundary must be closed.");
            }
        }

        for (var y = 0; y < Height; y++)
        {
            if (IsWalkable(0, y) || IsWalkable(Width - 1, y))
            {
                throw new ArgumentException("Maze boundary must be closed.");
            }
        }

        var first = WalkableTiles().FirstOrDefault();
        if (!IsWalkable(first.X, first.Y))
        {
            throw new ArgumentException("Maze must contain walkable tiles.");
        }

        var visited = new bool[Width * Height];
        var pending = new Queue<GridPoint>();
        pending.Enqueue(first);
        visited[(first.Y * Width) + first.X] = true;
        var reached = 0;

        ReadOnlySpan<GridPoint> offsets =
        [
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1)
        ];

        while (pending.TryDequeue(out var current))
        {
            reached++;
            foreach (var offset in offsets)
            {
                var next = new GridPoint(current.X + offset.X, current.Y + offset.Y);
                var index = (next.Y * Width) + next.X;
                if (IsWalkable(next.X, next.Y) && !visited[index])
                {
                    visited[index] = true;
                    pending.Enqueue(next);
                }
            }
        }

        if (reached != WalkableTiles().Count())
        {
            throw new ArgumentException("All walkable maze tiles must be connected.");
        }
    }
}

