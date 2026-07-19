namespace MazeHunter.Core.Players;

/// <summary>Player survival state with delayed, protected respawning.</summary>
public sealed class PlayerLife
{
    public const int StartingLives = 3;
    public const float RespawnDelaySeconds = 1.25f;
    public const float ProtectionSeconds = 2f;

    private float _respawnTimer;
    private float _protectionTimer;

    public int Lives { get; private set; } = StartingLives;

    public bool IsAlive { get; private set; } = true;

    public bool IsGameOver => Lives == 0;

    public bool IsProtected => IsAlive && _protectionTimer > 0;

    public float RespawnSecondsRemaining => IsAlive ? 0 : MathF.Max(0, _respawnTimer);

    public bool TryDamage()
    {
        if (!IsAlive || IsProtected || IsGameOver)
        {
            return false;
        }

        Lives--;
        IsAlive = false;
        _protectionTimer = 0;
        _respawnTimer = IsGameOver ? 0 : RespawnDelaySeconds;
        return true;
    }

    public bool Update(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (IsAlive)
        {
            _protectionTimer = MathF.Max(0, _protectionTimer - deltaSeconds);
            return false;
        }

        if (IsGameOver)
        {
            return false;
        }

        _respawnTimer -= deltaSeconds;
        return _respawnTimer <= 0;
    }

    public void CompleteRespawn()
    {
        if (IsAlive || IsGameOver || _respawnTimer > 0)
        {
            throw new InvalidOperationException("Player is not ready to respawn.");
        }

        IsAlive = true;
        _respawnTimer = 0;
        _protectionTimer = ProtectionSeconds;
    }

    public void Reset()
    {
        Lives = StartingLives;
        IsAlive = true;
        _respawnTimer = 0;
        _protectionTimer = 0;
    }
}

