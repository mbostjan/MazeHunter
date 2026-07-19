namespace MazeHunter.Game.Input;

/// <summary>Allocation-free held-key state supporting simultaneous key presses.</summary>
internal sealed class KeyboardState
{
    private readonly HashSet<Keys> _held = [];
    private readonly HashSet<Keys> _pressed = [];

    public bool IsDown(Keys key) => _held.Contains(key);

    public bool WasPressed(Keys key) => _pressed.Contains(key);

    public void Press(Keys key)
    {
        if (_held.Add(key))
        {
            _pressed.Add(key);
        }
    }

    public void Release(Keys key) => _held.Remove(key);

    public void EndUpdate() => _pressed.Clear();

    public void Clear()
    {
        _held.Clear();
        _pressed.Clear();
    }
}

