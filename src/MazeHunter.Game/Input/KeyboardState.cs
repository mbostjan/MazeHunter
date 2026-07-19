namespace MazeHunter.Game.Input;

/// <summary>Allocation-free held-key state supporting simultaneous key presses.</summary>
internal sealed class KeyboardState
{
    private readonly HashSet<Keys> _held = [];
    private readonly HashSet<Keys> _pressed = [];
    private readonly Dictionary<Keys, long> _pressOrder = [];
    private long _sequence;

    public bool IsDown(Keys key) => _held.Contains(key);

    public bool WasPressed(Keys key) => _pressed.Contains(key);

    public long PressOrder(Keys key) => _held.Contains(key) && _pressOrder.TryGetValue(key, out var order) ? order : -1;

    public void Press(Keys key)
    {
        if (_held.Add(key))
        {
            _pressed.Add(key);
            _pressOrder[key] = ++_sequence;
        }
    }

    public void Release(Keys key) => _held.Remove(key);

    public void EndUpdate() => _pressed.Clear();

    public void Clear()
    {
        _held.Clear();
        _pressed.Clear();
        _pressOrder.Clear();
    }
}
