using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Combat;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Mazes;
using MazeHunter.Core.Players;
using MazeHunter.Core.Rounds;
using MazeHunter.Core.Scoring;
using MazeHunter.Core.Spawning;
using MazeHunter.Core.State;
using MazeHunter.Core.Timing;
using MazeHunter.Game.Audio;
using MazeHunter.Game.Input;

namespace MazeHunter.Game.Application;

internal sealed class GameForm : Form
{
    private const int LogicalWidth = 320;
    private const int LogicalHeight = 240;

    private readonly Bitmap _framebuffer = new(LogicalWidth, LogicalHeight);
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly FixedStepClock _clock = new();
    private readonly System.Windows.Forms.Timer _pump = new() { Interval = 16 };
    private readonly KeyboardState _keyboard = new();
    private readonly Maze _maze = MazeCatalog.CreateSignalCrossing();
    private readonly Runner _runner;
    private readonly ProjectileSystem _projectiles = new();
    private readonly EnemySystem _enemies = new(seed: 0x4E454F4E);
    private readonly RoundDirector _rounds = new();
    private readonly PlayerLife _playerLife = new();
    private readonly ScoreSystem _score = new();
    private readonly GameFlow _flow = new();
    private readonly AudioSystem _audio = new();
    private long _previousTicks;
    private double _presentationTime;

    public GameForm()
    {
        _runner = new Runner(SpawnPlanner.FindPlayerSpawns(_maze).PlayerOne);
        Text = "Neon Labyrinth";
        ClientSize = new Size(960, 720);
        MinimumSize = new Size(640, 520);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(5, 7, 15);
        KeyPreview = true;
        DoubleBuffered = true;
        ResizeRedraw = true;

        _pump.Tick += PumpOnTick;
        Shown += (_, _) =>
        {
            _previousTicks = _stopwatch.ElapsedTicks;
            _pump.Start();
        };
        Activated += (_, _) =>
        {
            _clock.Reset();
            _previousTicks = _stopwatch.ElapsedTicks;
        };
        Deactivate += (_, _) =>
        {
            _clock.Reset();
            _keyboard.Clear();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pump.Stop();
            _pump.Dispose();
            _framebuffer.Dispose();
            _audio.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        RenderLogicalFrame();

        var scale = Math.Min(ClientSize.Width / (float)LogicalWidth, ClientSize.Height / (float)LogicalHeight);
        var width = Math.Max(1, (int)(LogicalWidth * scale));
        var height = Math.Max(1, (int)(LogicalHeight * scale));
        var destination = new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);

        e.Graphics.Clear(BackColor);
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(_framebuffer, destination, 0, 0, LogicalWidth, LogicalHeight, GraphicsUnit.Pixel);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _keyboard.Press(e.KeyCode);
        e.Handled = true;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _keyboard.Release(e.KeyCode);
        e.Handled = true;
        base.OnKeyUp(e);
    }

    private void PumpOnTick(object? sender, EventArgs e)
    {
        var ticks = _stopwatch.ElapsedTicks;
        var elapsed = (ticks - _previousTicks) / (double)Stopwatch.Frequency;
        _previousTicks = ticks;

        if (!Focused && !ContainsFocus)
        {
            return;
        }

        var steps = _clock.AddElapsed(elapsed);
        for (var i = 0; i < steps; i++)
        {
            UpdateSimulation(_clock.StepSeconds);
        }

        Invalidate();
    }

    private void UpdateSimulation(float deltaSeconds)
    {
        _presentationTime += deltaSeconds;
        ProcessSystemInput();
        if (!_flow.SimulationRunning)
        {
            _keyboard.EndUpdate();
            return;
        }

        _score.Update(deltaSeconds);
        if (_playerLife.Update(deltaSeconds))
        {
            _runner.Respawn(SpawnPlanner.FindSafestPlayerSpawn(_maze, _enemies));
            _playerLife.CompleteRespawn();
        }

        if (_playerLife.IsAlive)
        {
            _runner.Update(_maze, GetRequestedDirection(), deltaSeconds);
            if (_keyboard.WasPressed(Keys.Space))
            {
                var origin = _runner.Position + (_runner.Facing.ToVector() * 5f);
                if (_projectiles.TryFire(1, origin, _runner.Facing))
                {
                    _audio.PlayFire();
                }
            }
        }

        _projectiles.Update(_maze, deltaSeconds);
        _rounds.Update(_maze, _enemies, _runner.Position, deltaSeconds);
        if (_rounds.RoundAdvancedThisUpdate)
        {
            _score.RecordRoundCompleted(_rounds.RoundNumber - 1, _playerLife.Lives);
        }

        _enemies.Update(
            _maze,
            deltaSeconds,
            new EnemyContext(_runner.Position, _runner.Facing, _projectiles));
        while (_enemies.TryDestroyWithProjectiles(_projectiles, out _, out var destroyedKind))
        {
            _rounds.NotifyEnemyDefeated();
            _score.RecordEnemyDestroyed(destroyedKind);
        }

        if (_playerLife.IsAlive &&
            _enemies.HasContact(_runner.Position, Runner.CollisionRadius) &&
            _playerLife.TryDamage())
        {
            _score.ResetChain();
            _projectiles.Clear();
            if (_playerLife.IsGameOver)
            {
                _enemies.Clear();
                _flow.EndGame();
            }
        }

        _keyboard.EndUpdate();
    }

    private void RenderLogicalFrame()
    {
        using var graphics = Graphics.FromImage(_framebuffer);
        graphics.Clear(Color.FromArgb(5, 7, 15));
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        var pulse = (int)(20 + (Math.Sin(_presentationTime * 3) + 1) * 25);
        using var glowBrush = new SolidBrush(Color.FromArgb(255, 55 + pulse, 218, 190));
        using var titleFont = new Font(FontFamily.GenericMonospace, 13, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textFont = new Font(FontFamily.GenericMonospace, 9, FontStyle.Bold, GraphicsUnit.Pixel);

        if (_flow.Screen == GameScreen.Title)
        {
            RenderTitle(graphics, glowBrush, textFont);
            return;
        }

        if (_flow.Screen == GameScreen.Instructions)
        {
            RenderInstructions(graphics, glowBrush, textFont);
            return;
        }

        var header = _rounds.IsCompleting
            ? $"CYCLE {_rounds.RoundNumber} CLEARED"
            : $"CYCLE {_rounds.RoundNumber:00} // SIGNALS {_rounds.RemainingThisRound:00}";
        DrawCentered(graphics, header, titleFont, glowBrush, 8);
        RenderMaze(graphics);
        RenderProjectiles(graphics);
        RenderEnemies(graphics);
        RenderRunner(graphics);

        using var accentBrush = new SolidBrush(Color.FromArgb(255, 240, 85, 150));
        var status = _playerLife.IsAlive
            ? $"SCORE {_score.Score:000000}  CHAIN x{_score.Multiplier}  LIVES {_playerLife.Lives}"
            : $"SIGNAL LOST // RETURN IN {_playerLife.RespawnSecondsRemaining:0.0}";
        DrawCentered(graphics, status, textFont, accentBrush, 222);
        using var dimBrush = new SolidBrush(Color.FromArgb(255, 150, 164, 190));
        var footer = _audio.Muted ? "SPACE FIRE // M AUDIO ON" : "SPACE FIRE // M MUTE";
        DrawCentered(graphics, footer, textFont, dimBrush, 232);

        if (_flow.Screen == GameScreen.Paused)
        {
            RenderPaused(graphics);
        }
        else if (_flow.Screen == GameScreen.GameOver)
        {
            RenderGameOver(graphics);
        }
    }

    private void RenderTitle(Graphics graphics, Brush glowBrush, Font textFont)
    {
        using var largeFont = new Font(FontFamily.GenericMonospace, 28, FontStyle.Bold, GraphicsUnit.Pixel);
        using var subtitleBrush = new SolidBrush(Color.FromArgb(255, 255, 82, 164));
        using var textBrush = new SolidBrush(Color.FromArgb(255, 180, 200, 224));
        using var dimBrush = new SolidBrush(Color.FromArgb(255, 90, 112, 145));
        using var gridPen = new Pen(Color.FromArgb(255, 16, 49, 68));

        for (var x = 24; x < LogicalWidth; x += 24)
        {
            graphics.DrawLine(gridPen, x, 38, x, 204);
        }

        for (var y = 48; y < 204; y += 24)
        {
            graphics.DrawLine(gridPen, 16, y, LogicalWidth - 16, y);
        }

        DrawCentered(graphics, "NEON", largeFont, glowBrush, 50);
        DrawCentered(graphics, "LABYRINTH", largeFont, subtitleBrush, 80);
        DrawCentered(graphics, "A SIGNAL-RUNNER ARCADE", textFont, textBrush, 122);
        DrawCentered(graphics, "ENTER  SOLO LINK", textFont, glowBrush, 158);
        DrawCentered(graphics, "I      HOW TO PLAY", textFont, textBrush, 176);
        DrawCentered(graphics, "M      TOGGLE AUDIO", textFont, textBrush, 190);
        DrawCentered(graphics, "ORIGINAL CODE // ART // AUDIO", textFont, dimBrush, 222);
    }

    private static void RenderInstructions(Graphics graphics, Brush glowBrush, Font textFont)
    {
        using var headingFont = new Font(FontFamily.GenericMonospace, 18, FontStyle.Bold, GraphicsUnit.Pixel);
        using var accentBrush = new SolidBrush(Color.FromArgb(255, 255, 82, 164));
        using var textBrush = new SolidBrush(Color.FromArgb(255, 190, 210, 232));
        DrawCentered(graphics, "SIGNAL PROTOCOL", headingFont, glowBrush, 20);
        DrawCentered(graphics, "W A S D     MOVE", textFont, textBrush, 64);
        DrawCentered(graphics, "SPACE       FIRE PULSE", textFont, textBrush, 82);
        DrawCentered(graphics, "P / ESC     PAUSE", textFont, textBrush, 100);
        DrawCentered(graphics, "M           MUTE", textFont, textBrush, 118);
        DrawCentered(graphics, "CLEAR EVERY HOSTILE SIGNAL", textFont, accentBrush, 148);
        DrawCentered(graphics, "CHAIN HITS FOR UP TO 4x SCORE", textFont, textBrush, 164);
        DrawCentered(graphics, "THREE LINKS // SURVIVE THE GRID", textFont, textBrush, 180);
        DrawCentered(graphics, "ENTER OR ESC TO RETURN", textFont, glowBrush, 214);
    }

    private void RenderPaused(Graphics graphics)
    {
        using var overlayBrush = new SolidBrush(Color.FromArgb(210, 5, 7, 15));
        graphics.FillRectangle(overlayBrush, 48, 84, 224, 74);
        using var titleFont = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textFont = new Font(FontFamily.GenericMonospace, 9, FontStyle.Bold, GraphicsUnit.Pixel);
        using var titleBrush = new SolidBrush(Color.FromArgb(255, 55, 238, 210));
        using var textBrush = new SolidBrush(Color.White);
        DrawCentered(graphics, "LINK SUSPENDED", titleFont, titleBrush, 94);
        DrawCentered(graphics, "P / ESC RESUME   R RESTART", textFont, textBrush, 128);
        DrawCentered(graphics, "Q RETURN TO TITLE", textFont, textBrush, 144);
    }

    private void RenderEnemies(Graphics graphics)
    {
        const int tileSize = Runner.TileSize;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = 32;
        using var shellBrush = new SolidBrush(Color.FromArgb(255, 178, 70, 255));
        using var eyeBrush = new SolidBrush(Color.FromArgb(255, 110, 245, 255));
        using var backgroundBrush = new SolidBrush(Color.FromArgb(255, 8, 13, 27));

        for (var i = 0; i < _enemies.Capacity; i++)
        {
            var enemy = _enemies[i];
            if (!enemy.Active)
            {
                continue;
            }

            var x = offsetX + (int)MathF.Round(enemy.Position.X);
            var y = offsetY + (int)MathF.Round(enemy.Position.Y);
            shellBrush.Color = enemy.Kind switch
            {
                EnemyKind.Tracer => Color.FromArgb(255, 255, 80, 105),
                EnemyKind.Vector => Color.FromArgb(255, 255, 148, 48),
                EnemyKind.Veil => Color.FromArgb(255, 82, 160, 255),
                EnemyKind.Surge => Color.FromArgb(255, 255, 235, 72),
                EnemyKind.Prism => Color.FromArgb(255, 110, 255, 150),
                _ => Color.FromArgb(255, 178, 70, 255)
            };

            if (enemy.Kind == EnemyKind.Prism)
            {
                graphics.FillEllipse(shellBrush, x - 4, y - 4, 8, 8);
            }
            else
            {
                graphics.FillRectangle(shellBrush, x - 3, y - 3, 7, 7);
            }

            graphics.FillRectangle(backgroundBrush, x - 1, y - 1, 3, 3);
            var eyeOffset = enemy.AnimationFrame == 0 ? -2 : 2;
            graphics.FillRectangle(eyeBrush, x + eyeOffset, y, 1, 1);
        }
    }

    private void RenderProjectiles(Graphics graphics)
    {
        const int tileSize = Runner.TileSize;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = 32;
        using var pulseBrush = new SolidBrush(Color.FromArgb(255, 255, 82, 164));
        using var coreBrush = new SolidBrush(Color.White);

        for (var i = 0; i < _projectiles.Capacity; i++)
        {
            var projectile = _projectiles[i];
            if (!projectile.Active)
            {
                continue;
            }

            var x = offsetX + (int)MathF.Round(projectile.Position.X);
            var y = offsetY + (int)MathF.Round(projectile.Position.Y);
            if (projectile.Direction.IsVertical())
            {
                graphics.FillRectangle(pulseBrush, x - 1, y - 3, 3, 7);
            }
            else
            {
                graphics.FillRectangle(pulseBrush, x - 3, y - 1, 7, 3);
            }

            graphics.FillRectangle(coreBrush, x, y, 1, 1);
        }
    }

    private void RenderRunner(Graphics graphics)
    {
        if (!_playerLife.IsAlive ||
            (_playerLife.IsProtected && ((int)(_presentationTime * 10) & 1) == 0))
        {
            return;
        }

        const int tileSize = Runner.TileSize;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = 32;
        var x = offsetX + (int)MathF.Round(_runner.Position.X);
        var y = offsetY + (int)MathF.Round(_runner.Position.Y);

        using var shadowBrush = new SolidBrush(Color.FromArgb(255, 30, 52, 80));
        using var runnerBrush = new SolidBrush(Color.FromArgb(255, 255, 206, 72));
        graphics.FillRectangle(shadowBrush, x - 4, y - 4, 8, 8);

        Point[] arrow = _runner.Facing switch
        {
            Direction.Up => [new(x, y - 4), new(x - 3, y + 3), new(x + 3, y + 3)],
            Direction.Down => [new(x, y + 4), new(x - 3, y - 3), new(x + 3, y - 3)],
            Direction.Left => [new(x - 4, y), new(x + 3, y - 3), new(x + 3, y + 3)],
            _ => [new(x + 4, y), new(x - 3, y - 3), new(x - 3, y + 3)]
        };
        graphics.FillPolygon(runnerBrush, arrow);

        if (_runner.AnimationFrame == 1)
        {
            using var coreBrush = new SolidBrush(Color.White);
            graphics.FillRectangle(coreBrush, x - 1, y - 1, 2, 2);
        }
    }

    private void RenderGameOver(Graphics graphics)
    {
        using var overlayBrush = new SolidBrush(Color.FromArgb(220, 5, 7, 15));
        graphics.FillRectangle(overlayBrush, 44, 78, 232, 86);
        using var borderPen = new Pen(Color.FromArgb(255, 255, 82, 164), 2);
        graphics.DrawRectangle(borderPen, 44, 78, 231, 85);
        using var titleFont = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textFont = new Font(FontFamily.GenericMonospace, 9, FontStyle.Bold, GraphicsUnit.Pixel);
        using var titleBrush = new SolidBrush(Color.FromArgb(255, 255, 82, 164));
        using var textBrush = new SolidBrush(Color.White);
        DrawCentered(graphics, "LINK TERMINATED", titleFont, titleBrush, 94);
        DrawCentered(graphics, $"FINAL SCORE {_score.Score:000000}", textFont, textBrush, 126);
        DrawCentered(graphics, "ENTER TO RECONNECT", textFont, textBrush, 145);
    }

    private void RenderMaze(Graphics graphics)
    {
        const int tileSize = 8;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = 32;
        using var wallBrush = new SolidBrush(Color.FromArgb(255, 29, 75, 112));
        using var wallCoreBrush = new SolidBrush(Color.FromArgb(255, 44, 210, 190));
        using var floorBrush = new SolidBrush(Color.FromArgb(255, 8, 13, 27));

        for (var y = 0; y < _maze.Height; y++)
        {
            for (var x = 0; x < _maze.Width; x++)
            {
                var pixelX = offsetX + (x * tileSize);
                var pixelY = offsetY + (y * tileSize);
                if (_maze[x, y] == MazeTile.Wall)
                {
                    graphics.FillRectangle(wallBrush, pixelX, pixelY, tileSize, tileSize);
                    graphics.FillRectangle(wallCoreBrush, pixelX + 2, pixelY + 2, 4, 4);
                }
                else
                {
                    graphics.FillRectangle(floorBrush, pixelX, pixelY, tileSize, tileSize);
                }
            }
        }
    }

    private Direction GetRequestedDirection()
    {
        var direction = Direction.None;
        var newest = -1L;
        Select(Keys.W, Direction.Up);
        Select(Keys.S, Direction.Down);
        Select(Keys.A, Direction.Left);
        Select(Keys.D, Direction.Right);
        return direction;

        void Select(Keys key, Direction candidate)
        {
            var order = _keyboard.PressOrder(key);
            if (order > newest)
            {
                newest = order;
                direction = candidate;
            }
        }
    }

    private void ProcessSystemInput()
    {
        if (_keyboard.WasPressed(Keys.M))
        {
            _audio.ToggleMute();
        }

        switch (_flow.Screen)
        {
            case GameScreen.Title:
                if (_keyboard.WasPressed(Keys.Enter))
                {
                    ResetGame();
                    _flow.StartGame();
                }
                else if (_keyboard.WasPressed(Keys.I))
                {
                    _flow.ShowInstructions();
                }

                break;
            case GameScreen.Instructions:
                if (_keyboard.WasPressed(Keys.Enter) || _keyboard.WasPressed(Keys.Escape))
                {
                    _flow.ReturnToTitle();
                }

                break;
            case GameScreen.Playing:
                if (_keyboard.WasPressed(Keys.P) || _keyboard.WasPressed(Keys.Escape))
                {
                    _flow.TogglePause();
                }

                break;
            case GameScreen.Paused:
                if (_keyboard.WasPressed(Keys.P) || _keyboard.WasPressed(Keys.Escape))
                {
                    _flow.TogglePause();
                }
                else if (_keyboard.WasPressed(Keys.R))
                {
                    ResetGame();
                    _flow.TogglePause();
                }
                else if (_keyboard.WasPressed(Keys.Q))
                {
                    _flow.ReturnToTitle();
                }

                break;
            case GameScreen.GameOver:
                if (_keyboard.WasPressed(Keys.Enter))
                {
                    ResetGame();
                    _flow.StartGame();
                }
                else if (_keyboard.WasPressed(Keys.Escape))
                {
                    _flow.ReturnToTitle();
                }

                break;
        }
    }

    private void ResetGame()
    {
        _enemies.Clear();
        _projectiles.Clear();
        _rounds.Reset();
        _score.Reset();
        _playerLife.Reset();
        _runner.Respawn(SpawnPlanner.FindPlayerSpawns(_maze).PlayerOne);
        _clock.Reset();
    }

    private static void DrawCentered(Graphics graphics, string text, Font font, Brush brush, float y)
    {
        var size = graphics.MeasureString(text, font);
        graphics.DrawString(text, font, brush, (LogicalWidth - size.Width) / 2, y);
    }
}
