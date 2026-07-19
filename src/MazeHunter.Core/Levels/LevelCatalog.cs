using MazeHunter.Core.Geometry;
using MazeHunter.Core.Mazes;

namespace MazeHunter.Core.Levels;

public static class LevelCatalog
{
    private static readonly LevelDefinition[] Levels =
    [
        CreateSignalCrossing(),
        CreateRelayGardens(),
        CreatePrismVault()
    ];

    public static int HandcraftedLevelCount => Levels.Length;

    public static LevelDefinition Get(int levelNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(levelNumber, 1);
        return Levels[(levelNumber - 1) % Levels.Length];
    }

    private static LevelDefinition CreateSignalCrossing() => new(
        "Signal Crossing",
        MazeCatalog.CreateSignalCrossing(),
        new GridPoint(2, 19),
        new GridPoint(28, 19),
        [new GridPoint(15, 1), new GridPoint(1, 3), new GridPoint(29, 3)]);

    private static LevelDefinition CreateRelayGardens() => new(
        "Relay Gardens",
        Maze.FromAscii(
        [
            "###############################",
            "#.............................#",
            "#.###.#######.###.#######.###.#",
            "#.............................#",
            "#.####.###.###.###.###.####...#",
            "#.............................#",
            "#...####.#######.#######.####.#",
            "#.............................#",
            "#.###.###.###.###.###.###.###.#",
            "#.............................#",
            "#.#######.###.....###.#######.#",
            "#.............................#",
            "#.###.###.###.###.###.###.###.#",
            "#.............................#",
            "#.####.#######.#######.####...#",
            "#.............................#",
            "#...####.###.###.###.####.###.#",
            "#.............................#",
            "#.###.#######.###.#######.###.#",
            "#.............................#",
            "###############################"
        ]),
        new GridPoint(15, 19),
        new GridPoint(2, 19),
        [new GridPoint(1, 1), new GridPoint(29, 1), new GridPoint(15, 3)]);

    private static LevelDefinition CreatePrismVault() => new(
        "Prism Vault",
        Maze.FromAscii(
        [
            "###############################",
            "#.............................#",
            "#.#####.###.#######.###.#####.#",
            "#.............................#",
            "#.###.#####.###.###.#####.###.#",
            "#.............................#",
            "#.#######.###.###.###.#######.#",
            "#.............................#",
            "#.###.###.#########.###.###...#",
            "#.............................#",
            "#.#####.#####...#####.#####...#",
            "#.............................#",
            "#...###.###.#########.###.###.#",
            "#.............................#",
            "#.#######.###.###.###.#######.#",
            "#.............................#",
            "#.###.#####.###.###.#####.###.#",
            "#.............................#",
            "#.#####.###.#######.###.#####.#",
            "#.............................#",
            "###############################"
        ]),
        new GridPoint(28, 19),
        new GridPoint(15, 19),
        [new GridPoint(1, 5), new GridPoint(29, 5), new GridPoint(1, 1), new GridPoint(29, 1)]);
}
