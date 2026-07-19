namespace MazeHunter.Core.Diagnostics;

/// <summary>Small reproducible PRNG for gameplay decisions and diagnostic seeds.</summary>
public sealed class DeterministicRandom
{
    private uint _state;

    public DeterministicRandom(uint seed)
    {
        _state = seed == 0 ? 0x9E3779B9u : seed;
        Seed = seed;
    }

    public uint Seed { get; }

    public uint NextUInt()
    {
        var value = _state;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        _state = value;
        return value;
    }

    public int Next(int exclusiveMaximum)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exclusiveMaximum, 1);
        return (int)(NextUInt() % (uint)exclusiveMaximum);
    }
}

