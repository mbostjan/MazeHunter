using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Combat;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Mazes;
using MazeHunter.Core.Persistence;
using MazeHunter.Core.Players;
using MazeHunter.Core.Rounds;
using MazeHunter.Core.Scoring;
using MazeHunter.Core.Spawning;
using MazeHunter.Core.State;
using MazeHunter.Core.Timing;
using MazeHunter.Game.Audio;
using MazeHunter.Game.Input;
using MazeHunter.Game.Persistence;

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
    private readonly Runner _runnerTwo;
    private readonly ProjectileSystem _projectiles = new();
    private readonly EnemySystem _enemies = new(seed: 0x4E454F4E);
    private readonly RoundDirector _rounds = new();
    private readonly PlayerLife _playerLife = new();
    private readonly PlayerLife _playerTwoLife = new();
    private readonly ScoreSystem _score = new();
    private readonly ScoreSystem _scoreTwo = new();
    private readonly GameFlow _flow = new();
    private readonly AudioSystem _audio = new();
    private readonly ProfileStore _profileStore = new();
    private readonly PlayerProfile _profile;
    private readonly List<PendingScore> _pendingScores = [];
    private int _pendingScoreIndex;
    private string _callsignBuffer = string.Empty;
    private long _previousTicks;
    private double _presentationTime;

    public GameForm()
    {
        var spawns = SpawnPlanner.FindPlayerSpawns(_maze);
        _runner = new Runner(spawns.PlayerOne);
        _runnerTwo = new Runner(spawns.PlayerTwo);
        _profile = _profileStore.Load();
        _flow.SelectMode(_profile.Settings.LastMode);
        _audio.SetMuted(_profile.Settings.Muted);
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
        FormClosing += (_, _) => SaveProfileSafely();
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
        if (_flow.Screen == GameScreen.GameOver && IsEnteringCallsign)
        {
            HandleCallsignKey(e);
            return;
        }

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
        if (IsCooperative)
        {
            _scoreTwo.Update(deltaSeconds);
        }

        if (_playerLife.Update(deltaSeconds))
        {
            _runner.Respawn(SpawnPlanner.FindSafestPlayerSpawn(
                _maze,
                _enemies,
                teammatePosition: IsCooperative && _playerTwoLife.IsAlive ? _runnerTwo.Position : null));
            _playerLife.CompleteRespawn();
        }

        if (IsCooperative && _playerTwoLife.Update(deltaSeconds))
        {
            _runnerTwo.Respawn(SpawnPlanner.FindSafestPlayerSpawn(
                _maze,
                _enemies,
                teammatePosition: _playerLife.IsAlive ? _runner.Position : null));
            _playerTwoLife.CompleteRespawn();
        }

        if (_playerLife.IsAlive)
        {
            _runner.Update(_maze, GetPlayerOneDirection(), deltaSeconds);
            if (_keyboard.WasPressed(Keys.Space))
            {
                FirePulse(1, _runner);
            }
        }

        if (IsCooperative && _playerTwoLife.IsAlive)
        {
            _runnerTwo.Update(_maze, GetPlayerTwoDirection(), deltaSeconds);
            if (_keyboard.WasPressed(Keys.Enter) || _keyboard.WasPressed(Keys.RControlKey))
            {
                FirePulse(2, _runnerTwo);
            }
        }

        _projectiles.Update(_maze, deltaSeconds);
        var primaryRunner = IsCooperative && !_playerLife.IsAlive && _playerTwoLife.IsAlive
            ? _runnerTwo
            : _runner;
        Vector2? secondTarget = IsCooperative && _playerLife.IsAlive && _playerTwoLife.IsAlive
            ? _runnerTwo.Position
            : null;
        _rounds.Update(_maze, _enemies, primaryRunner.Position, deltaSeconds, secondTarget);
        if (_rounds.RoundAdvancedThisUpdate)
        {
            _score.RecordRoundCompleted(_rounds.RoundNumber - 1, _playerLife.Lives);
            if (IsCooperative)
            {
                _scoreTwo.RecordRoundCompleted(_rounds.RoundNumber - 1, _playerTwoLife.Lives);
                if (!_playerLife.IsGameOver && !_playerTwoLife.IsGameOver)
                {
                    _score.RecordTeamSurvivalBonus(_rounds.RoundNumber - 1);
                    _scoreTwo.RecordTeamSurvivalBonus(_rounds.RoundNumber - 1);
                }

                RecoverEliminatedPartner();
            }
        }

        var primaryFacing = ReferenceEquals(primaryRunner, _runner) ? _runner.Facing : _runnerTwo.Facing;
        _enemies.Update(
            _maze,
            deltaSeconds,
            new EnemyContext(
                primaryRunner.Position,
                primaryFacing,
                _projectiles,
                secondTarget,
                _runnerTwo.Facing));
        while (_enemies.TryDestroyWithProjectiles(_projectiles, out var ownerId, out var destroyedKind))
        {
            _rounds.NotifyEnemyDefeated();
            if (ownerId == 2 && IsCooperative)
            {
                _scoreTwo.RecordEnemyDestroyed(destroyedKind);
            }
            else
            {
                _score.RecordEnemyDestroyed(destroyedKind);
            }
        }

        if (_playerLife.IsAlive &&
            _enemies.HasContact(_runner.Position, Runner.CollisionRadius) &&
            _playerLife.TryDamage())
        {
            _score.ResetChain();
            _projectiles.Clear();
        }

        if (IsCooperative &&
            _playerTwoLife.IsAlive &&
            _enemies.HasContact(_runnerTwo.Position, Runner.CollisionRadius) &&
            _playerTwoLife.TryDamage())
        {
            _scoreTwo.ResetChain();
            _projectiles.Clear();
        }

        if (LocalTeamRules.IsRunOver(_flow.Mode, _playerLife, _playerTwoLife))
        {
            _enemies.Clear();
            _projectiles.Clear();
            _flow.EndGame();
            BeginHighScoreEntry();
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
        RenderRunner(graphics, _runner, _playerLife, Color.FromArgb(255, 255, 206, 72));
        if (IsCooperative)
        {
            RenderRunner(graphics, _runnerTwo, _playerTwoLife, Color.FromArgb(255, 85, 175, 255));
        }

        using var accentBrush = new SolidBrush(Color.FromArgb(255, 240, 85, 150));
        using var dimBrush = new SolidBrush(Color.FromArgb(255, 150, 164, 190));
        if (IsCooperative)
        {
            DrawCentered(
                graphics,
                GetPlayerHud("P1", _score, _playerLife),
                textFont,
                accentBrush,
                220);
            DrawCentered(
                graphics,
                GetPlayerHud("P2", _scoreTwo, _playerTwoLife),
                textFont,
                dimBrush,
                231);
        }
        else
        {
            var status = GetPlayerHud("P1", _score, _playerLife);
            DrawCentered(graphics, status, textFont, accentBrush, 222);
            var footer = _audio.Muted ? "SPACE FIRE // M AUDIO ON" : "SPACE FIRE // M MUTE";
            DrawCentered(graphics, footer, textFont, dimBrush, 232);
        }

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
        DrawCentered(graphics, "A SIGNAL-RUNNER ARCADE", textFont, textBrush, 116);
        DrawCentered(
            graphics,
            $"ENTER LAST: {(_flow.Mode == GameMode.Solo ? "SOLO" : "DUAL")}",
            textFont,
            glowBrush,
            142);
        DrawCentered(graphics, "1 SOLO   2 DUAL   I GUIDE", textFont, subtitleBrush, 158);
        DrawCentered(graphics, "M AUDIO", textFont, textBrush, 174);
        DrawCentered(graphics, "TOP SIGNALS", textFont, dimBrush, 194);
        for (var i = 0; i < Math.Min(3, _profile.HighScores.Count); i++)
        {
            var entry = _profile.HighScores[i];
            DrawCentered(
                graphics,
                $"{i + 1} {entry.Callsign,-8} {entry.Score:000000} C{entry.Round:00}",
                textFont,
                textBrush,
                205 + (i * 10));
        }
    }

    private static void RenderInstructions(Graphics graphics, Brush glowBrush, Font textFont)
    {
        using var headingFont = new Font(FontFamily.GenericMonospace, 18, FontStyle.Bold, GraphicsUnit.Pixel);
        using var accentBrush = new SolidBrush(Color.FromArgb(255, 255, 82, 164));
        using var textBrush = new SolidBrush(Color.FromArgb(255, 190, 210, 232));
        DrawCentered(graphics, "SIGNAL PROTOCOL", headingFont, glowBrush, 20);
        DrawCentered(graphics, "W A S D     MOVE", textFont, textBrush, 64);
        DrawCentered(graphics, "SPACE       FIRE PULSE", textFont, textBrush, 82);
        DrawCentered(graphics, "ARROWS      P2 MOVE", textFont, textBrush, 100);
        DrawCentered(graphics, "ENTER/RCTRL P2 FIRE", textFont, textBrush, 118);
        DrawCentered(graphics, "P / ESC PAUSE    M MUTE", textFont, textBrush, 136);
        DrawCentered(graphics, "CLEAR EVERY HOSTILE SIGNAL", textFont, accentBrush, 158);
        DrawCentered(graphics, "NO FRIENDLY FIRE // SCORE ALONE", textFont, textBrush, 176);
        DrawCentered(graphics, "ONE SURVIVOR KEEPS THE LINK", textFont, textBrush, 192);
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

            pulseBrush.Color = projectile.OwnerId == 2
                ? Color.FromArgb(255, 80, 175, 255)
                : Color.FromArgb(255, 255, 82, 164);
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

    private void RenderRunner(Graphics graphics, Runner runner, PlayerLife life, Color color)
    {
        if (!life.IsAlive ||
            (life.IsProtected && ((int)(_presentationTime * 10) & 1) == 0))
        {
            return;
        }

        const int tileSize = Runner.TileSize;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = 32;
        var x = offsetX + (int)MathF.Round(runner.Position.X);
        var y = offsetY + (int)MathF.Round(runner.Position.Y);

        using var shadowBrush = new SolidBrush(Color.FromArgb(255, 30, 52, 80));
        using var runnerBrush = new SolidBrush(color);
        graphics.FillRectangle(shadowBrush, x - 4, y - 4, 8, 8);

        Point[] arrow = runner.Facing switch
        {
            Direction.Up => [new(x, y - 4), new(x - 3, y + 3), new(x + 3, y + 3)],
            Direction.Down => [new(x, y + 4), new(x - 3, y - 3), new(x + 3, y - 3)],
            Direction.Left => [new(x - 4, y), new(x + 3, y - 3), new(x + 3, y + 3)],
            _ => [new(x + 4, y), new(x - 3, y - 3), new(x - 3, y + 3)]
        };
        graphics.FillPolygon(runnerBrush, arrow);

        if (runner.AnimationFrame == 1)
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
        var finalScore = IsCooperative
            ? $"P1 {_score.Score:000000}  P2 {_scoreTwo.Score:000000}"
            : $"FINAL SCORE {_score.Score:000000}";
        DrawCentered(graphics, finalScore, textFont, textBrush, 122);
        if (IsEnteringCallsign)
        {
            var pending = _pendingScores[_pendingScoreIndex];
            DrawCentered(
                graphics,
                $"P{pending.PlayerId} CALLSIGN: {_callsignBuffer}_",
                textFont,
                textBrush,
                140);
            DrawCentered(graphics, "TYPE 1-8 KEYS // ENTER SAVE", textFont, textBrush, 152);
        }
        else
        {
            DrawCentered(graphics, "ENTER TO RECONNECT", textFont, textBrush, 145);
        }
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

    private Direction GetPlayerOneDirection() =>
        GetDirection(Keys.W, Keys.S, Keys.A, Keys.D);

    private Direction GetPlayerTwoDirection() =>
        GetDirection(Keys.Up, Keys.Down, Keys.Left, Keys.Right);

    private Direction GetDirection(Keys up, Keys down, Keys left, Keys right)
    {
        var direction = Direction.None;
        var newest = -1L;
        Select(up, Direction.Up);
        Select(down, Direction.Down);
        Select(left, Direction.Left);
        Select(right, Direction.Right);
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
            _profile.Settings.Muted = _audio.Muted;
        }

        switch (_flow.Screen)
        {
            case GameScreen.Title:
                if (_keyboard.WasPressed(Keys.Enter))
                {
                    ResetGame();
                    _flow.StartGame();
                }
                else if (_keyboard.WasPressed(Keys.D1))
                {
                    SelectAndStartMode(GameMode.Solo);
                }
                else if (_keyboard.WasPressed(Keys.D2))
                {
                    SelectAndStartMode(GameMode.Cooperative);
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
        _pendingScores.Clear();
        _pendingScoreIndex = 0;
        _callsignBuffer = string.Empty;
        _enemies.Clear();
        _projectiles.Clear();
        _rounds.Reset();
        _score.Reset();
        _scoreTwo.Reset();
        _playerLife.Reset();
        _playerTwoLife.Reset();
        var spawns = SpawnPlanner.FindPlayerSpawns(_maze);
        _runner.Respawn(spawns.PlayerOne);
        _runnerTwo.Respawn(spawns.PlayerTwo);
        _clock.Reset();
    }

    private void FirePulse(int playerId, Runner runner)
    {
        var origin = runner.Position + (runner.Facing.ToVector() * 5f);
        if (_projectiles.TryFire(playerId, origin, runner.Facing))
        {
            _audio.PlayFire();
        }
    }

    private void SelectAndStartMode(GameMode mode)
    {
        _flow.SelectMode(mode);
        _profile.Settings.LastMode = mode;
        ResetGame();
        _flow.StartGame();
    }

    private void BeginHighScoreEntry()
    {
        _pendingScores.Clear();
        _pendingScoreIndex = 0;
        if (_profile.QualifiesForHighScore(_score.Score))
        {
            _pendingScores.Add(new PendingScore(1, _score.Score, _rounds.RoundNumber));
        }

        if (IsCooperative && _profile.QualifiesForHighScore(_scoreTwo.Score))
        {
            _pendingScores.Add(new PendingScore(2, _scoreTwo.Score, _rounds.RoundNumber));
        }

        LoadPendingCallsign();
    }

    private void LoadPendingCallsign()
    {
        if (!IsEnteringCallsign)
        {
            _callsignBuffer = string.Empty;
            return;
        }

        _callsignBuffer = _pendingScores[_pendingScoreIndex].PlayerId == 1
            ? _profile.Settings.PlayerOneCallsign
            : _profile.Settings.PlayerTwoCallsign;
    }

    private void HandleCallsignKey(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;
        if (e.KeyCode == Keys.Back)
        {
            if (_callsignBuffer.Length > 0)
            {
                _callsignBuffer = _callsignBuffer[..^1];
            }

            return;
        }

        if (e.KeyCode is Keys.Enter or Keys.Escape)
        {
            CompletePendingScore();
            return;
        }

        var character = GetCallsignCharacter(e.KeyCode);
        if (character != '\0' && _callsignBuffer.Length < 8)
        {
            _callsignBuffer += character;
        }
    }

    private void CompletePendingScore()
    {
        var pending = _pendingScores[_pendingScoreIndex];
        var callsign = PlayerProfile.SanitizeCallsign(_callsignBuffer);
        _profile.AddHighScore(
            callsign,
            pending.Score,
            pending.Round,
            _flow.Mode,
            DateTimeOffset.UtcNow);
        if (pending.PlayerId == 1)
        {
            _profile.Settings.PlayerOneCallsign = callsign;
        }
        else
        {
            _profile.Settings.PlayerTwoCallsign = callsign;
        }

        _pendingScoreIndex++;
        LoadPendingCallsign();
        SaveProfileSafely();
    }

    private void SaveProfileSafely()
    {
        try
        {
            _profile.Settings.Muted = _audio.Muted;
            _profile.Settings.LastMode = _flow.Mode;
            _profileStore.Save(_profile);
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"Profile save failed: {exception}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.WriteLine($"Profile save access failed: {exception}");
        }
    }

    private static char GetCallsignCharacter(Keys key)
    {
        if (key is >= Keys.A and <= Keys.Z)
        {
            return (char)('A' + ((int)key - (int)Keys.A));
        }

        if (key is >= Keys.D0 and <= Keys.D9)
        {
            return (char)('0' + ((int)key - (int)Keys.D0));
        }

        if (key is >= Keys.NumPad0 and <= Keys.NumPad9)
        {
            return (char)('0' + ((int)key - (int)Keys.NumPad0));
        }

        return '\0';
    }

    private void RecoverEliminatedPartner()
    {
        if (LocalTeamRules.ShouldRecoverAtNextRound(_flow.Mode, _playerLife, _playerTwoLife))
        {
            _playerLife.ReviveForNextRound();
            _runner.Respawn(SpawnPlanner.FindSafestPlayerSpawn(
                _maze,
                _enemies,
                teammatePosition: _runnerTwo.Position));
        }
        else if (LocalTeamRules.ShouldRecoverAtNextRound(_flow.Mode, _playerTwoLife, _playerLife))
        {
            _playerTwoLife.ReviveForNextRound();
            _runnerTwo.Respawn(SpawnPlanner.FindSafestPlayerSpawn(
                _maze,
                _enemies,
                teammatePosition: _runner.Position));
        }
    }

    private static string GetPlayerHud(string label, ScoreSystem score, PlayerLife life)
    {
        if (life.IsGameOver)
        {
            return $"{label} OFFLINE  SCORE {score.Score:000000}";
        }

        if (!life.IsAlive)
        {
            return $"{label} RETURN {life.RespawnSecondsRemaining:0.0}  SCORE {score.Score:000000}";
        }

        return $"{label} {score.Score:000000} x{score.Multiplier} L{life.Lives}";
    }

    private bool IsCooperative => _flow.Mode == GameMode.Cooperative;

    private bool IsEnteringCallsign => _pendingScoreIndex < _pendingScores.Count;

    private readonly record struct PendingScore(int PlayerId, int Score, int Round);

    private static void DrawCentered(Graphics graphics, string text, Font font, Brush brush, float y)
    {
        var size = graphics.MeasureString(text, font);
        graphics.DrawString(text, font, brush, (LogicalWidth - size.Width) / 2, y);
    }
}
