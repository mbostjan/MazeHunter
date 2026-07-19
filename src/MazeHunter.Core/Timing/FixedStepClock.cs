namespace MazeHunter.Core.Timing;

/// <summary>Converts variable wall-clock samples into deterministic simulation steps.</summary>
public sealed class FixedStepClock
{
    private readonly double _stepSeconds;
    private readonly double _maximumFrameSeconds;
    private double _accumulator;

    public FixedStepClock(int updatesPerSecond = 60, double maximumFrameSeconds = 0.25)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(updatesPerSecond, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumFrameSeconds, 0);
        _stepSeconds = 1.0 / updatesPerSecond;
        _maximumFrameSeconds = maximumFrameSeconds;
    }

    public float StepSeconds => (float)_stepSeconds;

    public double InterpolationAlpha => _accumulator / _stepSeconds;

    public int AddElapsed(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        }

        _accumulator += Math.Min(elapsedSeconds, _maximumFrameSeconds);
        var steps = (int)(_accumulator / _stepSeconds);
        _accumulator -= steps * _stepSeconds;
        return steps;
    }

    public void Reset() => _accumulator = 0;
}

