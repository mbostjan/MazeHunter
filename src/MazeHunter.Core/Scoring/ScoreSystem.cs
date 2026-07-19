using MazeHunter.Core.Enemies;

namespace MazeHunter.Core.Scoring;

public sealed class ScoreSystem
{
    public const float ChainWindowSeconds = 2.5f;
    private float _chainTimer;

    public int Score { get; private set; }

    public int Chain { get; private set; }

    public int Multiplier => Math.Min(1 + (Chain / 3), 4);

    public void Update(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (Chain == 0)
        {
            return;
        }

        _chainTimer -= deltaSeconds;
        if (_chainTimer <= 0)
        {
            ResetChain();
        }
    }

    public int RecordEnemyDestroyed(EnemyKind kind)
    {
        var awarded = GetBaseValue(kind) * Multiplier;
        Score += awarded;
        Chain++;
        _chainTimer = ChainWindowSeconds;
        return awarded;
    }

    public int RecordRoundCompleted(int roundNumber, int lives)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundNumber, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(lives);
        var awarded = (roundNumber * 500) + (lives * 100);
        Score += awarded;
        return awarded;
    }

    public int RecordTeamSurvivalBonus(int roundNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundNumber, 1);
        var awarded = roundNumber * 250;
        Score += awarded;
        return awarded;
    }

    public void ResetChain()
    {
        Chain = 0;
        _chainTimer = 0;
    }

    public void Reset()
    {
        Score = 0;
        ResetChain();
    }

    public static int GetBaseValue(EnemyKind kind) => kind switch
    {
        EnemyKind.Tracer => 150,
        EnemyKind.Vector => 200,
        EnemyKind.Veil => 250,
        EnemyKind.Surge => 300,
        EnemyKind.Prism => 750,
        _ => 100
    };
}
