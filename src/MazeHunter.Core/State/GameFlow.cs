namespace MazeHunter.Core.State;

/// <summary>Authoritative high-level screen and simulation-running state.</summary>
public sealed class GameFlow
{
    public GameScreen Screen { get; private set; } = GameScreen.Title;

    public bool SimulationRunning => Screen == GameScreen.Playing;

    public void ShowInstructions()
    {
        if (Screen == GameScreen.Title)
        {
            Screen = GameScreen.Instructions;
        }
    }

    public void StartGame()
    {
        if (Screen is GameScreen.Title or GameScreen.GameOver)
        {
            Screen = GameScreen.Playing;
        }
    }

    public void TogglePause()
    {
        Screen = Screen switch
        {
            GameScreen.Playing => GameScreen.Paused,
            GameScreen.Paused => GameScreen.Playing,
            _ => Screen
        };
    }

    public void EndGame()
    {
        if (Screen == GameScreen.Playing)
        {
            Screen = GameScreen.GameOver;
        }
    }

    public void ReturnToTitle()
    {
        if (Screen is GameScreen.Instructions or GameScreen.Paused or GameScreen.GameOver)
        {
            Screen = GameScreen.Title;
        }
    }
}

