using System.Numerics;

namespace MazeHunter.Game.Rendering;

internal enum VisualEffectKind : byte
{
    EnemyBurst,
    PlayerDamage,
    RoundClear
}

internal readonly record struct VisualEffect(
    bool Active,
    VisualEffectKind Kind,
    Vector2 Position,
    float Age,
    float Duration);

/// <summary>Fixed-capacity presentation-only effects with no per-update allocation.</summary>
internal sealed class VisualEffectSystem
{
    private readonly VisualEffect[] _effects = new VisualEffect[32];

    public int Capacity => _effects.Length;

    public VisualEffect this[int index] => _effects[index];

    public void Spawn(VisualEffectKind kind, Vector2 position, bool reducedFlashes)
    {
        for (var i = 0; i < _effects.Length; i++)
        {
            if (_effects[i].Active)
            {
                continue;
            }

            var duration = kind switch
            {
                VisualEffectKind.RoundClear => reducedFlashes ? 0.2f : 0.55f,
                VisualEffectKind.PlayerDamage => reducedFlashes ? 0.12f : 0.35f,
                _ => reducedFlashes ? 0.1f : 0.25f
            };
            _effects[i] = new VisualEffect(true, kind, position, 0, duration);
            return;
        }
    }

    public void Update(float deltaSeconds)
    {
        for (var i = 0; i < _effects.Length; i++)
        {
            var effect = _effects[i];
            if (!effect.Active)
            {
                continue;
            }

            var age = effect.Age + deltaSeconds;
            _effects[i] = age >= effect.Duration
                ? default
                : effect with { Age = age };
        }
    }

    public void Clear() => Array.Clear(_effects);
}
