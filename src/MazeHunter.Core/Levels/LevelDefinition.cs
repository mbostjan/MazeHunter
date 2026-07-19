using MazeHunter.Core.Geometry;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Levels;

/// <summary>Handcrafted maze and spawn intent for one explicit game level.</summary>
public sealed record LevelDefinition(
    string Name,
    Maze Maze,
    GridPoint PlayerOneSpawn,
    GridPoint PlayerTwoSpawn,
    IReadOnlyList<GridPoint> EnemyEntries);
