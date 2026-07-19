using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Numerics;
using MazeHunter.Core.Actors;
using MazeHunter.Core.Combat;
using MazeHunter.Core.Enemies;
using MazeHunter.Core.Geometry;
using MazeHunter.Core.Levels;
using MazeHunter.Core.Mazes;
using MazeHunter.Core.Persistence;
using MazeHunter.Core.Players;
using MazeHunter.Core.Scoring;
using MazeHunter.Core.Spawning;
using MazeHunter.Core.State;
using MazeHunter.Core.Timing;
using MazeHunter.Game.Audio;
using MazeHunter.Game.Diagnostics;
using MazeHunter.Game.Input;
using MazeHunter.Game.Persistence;
using MazeHunter.Game.Rendering;

namespace MazeHunter.Game.Application;

internal sealed class GameForm : Form
{
    private const int LogicalWidth = 400;
    private const int LogicalHeight = 300;
    private const int MazeOffsetY = 38;

    private readonly Bitmap _framebuffer = new(LogicalWidth, LogicalHeight);
    private Bitmap _mazeLayer;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly FixedStepClock _clock = new();
    private readonly System.Windows.Forms.Timer _pump = new() { Interval = 8 };
    private readonly KeyboardState _keyboard = new();
    private readonly GameGeometry _geometry = GameGeometry.Default;
    private Maze _maze;
    private readonly Runner _runner;
    private readonly Runner _runnerTwo;
    private readonly ProjectileSystem _projectiles;
    private readonly EnemySystem _enemies;
    private readonly LevelDirector _levels = new();
    private readonly PlayerLife _playerLife = new();
    private readonly PlayerLife _playerTwoLife = new();
    private readonly ScoreSystem _score = new();
    private readonly ScoreSystem _scoreTwo = new();
    private readonly GameFlow _flow = new();
    private readonly AudioSystem _audio = new();
    private readonly ProfileStore _profileStore = new();
    private readonly PlayerProfile _profile;
    private readonly DiagnosticLog _log;
    private readonly List<PendingScore> _pendingScores = [];
    private readonly VisualEffectSystem _effects = new();
    private readonly GameRenderResources _render = new();
    private int _pendingScoreIndex;
    private string _callsignBuffer = string.Empty;
    private long _previousTicks;
    private double _presentationTime;
    private bool _diagnosticsEnabled;
    private int _diagnosticFrames;
    private double _diagnosticSampleSeconds;
    private double _framesPerSecond;
    private double _lastUpdateMilliseconds;
    private long _lastAllocatedBytes = GC.GetTotalAllocatedBytes();
    private double _allocatedBytesPerSecond;

    public GameForm()
    {
        _maze = _levels.CurrentLevel.Maze;
        var spawns = SpawnPlanner.FindPlayerSpawns(_levels.CurrentLevel, _geometry.TileSize);
        _runner = new Runner(spawns.PlayerOne, _geometry);
        _runnerTwo = new Runner(spawns.PlayerTwo, _geometry);
        _projectiles = new ProjectileSystem(geometry: _geometry);
        _enemies = new EnemySystem(seed: 0x4E454F4E, geometry: _geometry);
        _profile = _profileStore.Load();
        _log = new DiagnosticLog(Path.GetDirectoryName(_profileStore.ProfilePath) ?? ".");
        _log.Write($"Startup .NET {Environment.Version} mode={_profile.Settings.LastMode}");
        _flow.SelectMode(_profile.Settings.LastMode);
        _audio.SetMuted(_profile.Settings.Muted);
        _mazeLayer = CreateMazeLayer();
        Text = "Neon Labyrinth";
        ClientSize = new Size(1200, 900);
        MinimumSize = new Size(800, 650);
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
        FormClosing += (_, _) =>
        {
            SaveProfileSafely();
            _log.Write(
                $"Clean shutdown fps={_framesPerSecond:0.0} " +
                $"updateMs={_lastUpdateMilliseconds:0.00} " +
                $"allocKiBs={_allocatedBytesPerSecond / 1024:0.0}");
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pump.Stop();
            _pump.Dispose();
            _framebuffer.Dispose();
            _mazeLayer.Dispose();
            _audio.Dispose();
            _render.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        RenderLogicalFrame();
        _diagnosticFrames++;

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
        var updateStart = Stopwatch.GetTimestamp();
        for (var i = 0; i < steps; i++)
        {
            UpdateSimulation(_clock.StepSeconds);
        }
        _lastUpdateMilliseconds =
            (Stopwatch.GetTimestamp() - updateStart) * 1000d / Stopwatch.Frequency;
        UpdateDiagnosticSample(elapsed);

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
        _effects.Update(deltaSeconds);
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

        if (_levels.IsTransitioning)
        {
            _levels.Update(
                _enemies,
                _runner.Position,
                deltaSeconds,
                IsCooperative ? _runnerTwo.Position : null);
            HandleLevelAdvance();
            _keyboard.EndUpdate();
            return;
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
        _levels.Update(_enemies, primaryRunner.Position, deltaSeconds, secondTarget);
        HandleLevelAdvance();

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
        while (_enemies.TryDestroyWithProjectiles(
                   _projectiles,
                   out var ownerId,
                   out var destroyedKind,
                   out var destroyedPosition))
        {
            _audio.PlayEnemyDestroyed();
            _effects.Spawn(
                VisualEffectKind.EnemyBurst,
                destroyedPosition,
                _profile.Settings.ReducedFlashes);
            _levels.NotifyEnemyDefeated();
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
            _enemies.HasContact(_runner.Position, _runner.CollisionRadius) &&
            _playerLife.TryDamage())
        {
            _audio.PlayPlayerDamage();
            _effects.Spawn(
                VisualEffectKind.PlayerDamage,
                _runner.Position,
                _profile.Settings.ReducedFlashes);
            _score.ResetChain();
            _projectiles.Clear();
        }

        if (IsCooperative &&
            _playerTwoLife.IsAlive &&
            _enemies.HasContact(_runnerTwo.Position, _runnerTwo.CollisionRadius) &&
            _playerTwoLife.TryDamage())
        {
            _audio.PlayPlayerDamage();
            _effects.Spawn(
                VisualEffectKind.PlayerDamage,
                _runnerTwo.Position,
                _profile.Settings.ReducedFlashes);
            _scoreTwo.ResetChain();
            _projectiles.Clear();
        }

        if (LocalTeamRules.IsRunOver(_flow.Mode, _playerLife, _playerTwoLife))
        {
            _enemies.Clear();
            _projectiles.Clear();
            _flow.EndGame();
            _audio.PlayGameOver();
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
        _render.GlowBrush.Color = Color.FromArgb(255, 55 + pulse, 218, 190);
        var glowBrush = _render.GlowBrush;
        var titleFont = _render.HeaderFont;
        var textFont = _render.TextFont;

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

        var header = _levels.IsTransitioning
            ? $"LEVEL {_levels.LevelNumber} CLEARED"
            : $"LEVEL {_levels.LevelNumber:00} {_levels.CurrentLevel.Name.ToUpperInvariant()} // SIGNALS {_levels.RemainingThisLevel:00}";
        DrawCentered(graphics, header, titleFont, glowBrush, 8);
        RenderMaze(graphics);
        RenderProjectiles(graphics);
        RenderEnemies(graphics);
        RenderEffects(graphics);
        RenderRunner(
            graphics,
            _runner,
            _playerLife,
            _profile.Settings.HighContrast ? Color.Yellow : Color.FromArgb(255, 255, 206, 72));
        if (IsCooperative)
        {
            RenderRunner(
                graphics,
                _runnerTwo,
                _playerTwoLife,
                _profile.Settings.HighContrast ? Color.Cyan : Color.FromArgb(255, 85, 175, 255));
        }

        var accentBrush = _render.AccentBrush;
        var dimBrush = _render.DimBrush;
        if (IsCooperative)
        {
            DrawCentered(
                graphics,
                GetPlayerHud("P1", _score, _playerLife),
                textFont,
                accentBrush,
                270);
            DrawCentered(
                graphics,
                GetPlayerHud("P2", _scoreTwo, _playerTwoLife),
                textFont,
                dimBrush,
                286);
        }
        else
        {
            var status = GetPlayerHud("P1", _score, _playerLife);
            DrawCentered(graphics, status, textFont, accentBrush, 272);
            var footer = _audio.Muted ? "SPACE FIRE // M AUDIO ON" : "SPACE FIRE // M MUTE";
            DrawCentered(graphics, footer, textFont, dimBrush, 286);
        }

        if (_flow.Screen == GameScreen.Paused)
        {
            RenderPaused(graphics);
        }

        if (_levels.IsTransitioning)
        {
            RenderLevelTransition(graphics);
        }
        else if (_flow.Screen == GameScreen.GameOver)
        {
            RenderGameOver(graphics);
        }

        if (_diagnosticsEnabled)
        {
            RenderDiagnostics(graphics);
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
        DrawCentered(graphics, "P/ESC PAUSE  M MUTE", textFont, textBrush, 136);
        DrawCentered(graphics, "CLEAR EVERY HOSTILE SIGNAL", textFont, accentBrush, 158);
        DrawCentered(graphics, "NO FRIENDLY FIRE // SCORE ALONE", textFont, textBrush, 176);
        DrawCentered(graphics, "F2 CONTRAST  F4 REDUCED FLASH", textFont, textBrush, 192);
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
        var tileSize = _geometry.TileSize;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = MazeOffsetY;
        var shellBrush = _render.EnemyShellBrush;
        var eyeBrush = _render.EnemyEyeBrush;
        var backgroundBrush = _render.BackgroundBrush;

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
                graphics.FillEllipse(shellBrush, x - 5, y - 5, 10, 10);
            }
            else
            {
                graphics.FillRectangle(shellBrush, x - 4, y - 4, 9, 9);
            }

            graphics.FillRectangle(backgroundBrush, x - 2, y - 2, 4, 4);
            var eyeOffset = enemy.AnimationFrame == 0 ? -3 : 3;
            graphics.FillRectangle(eyeBrush, x + eyeOffset, y - 1, 2, 2);
        }
    }

    private void RenderProjectiles(Graphics graphics)
    {
        var tileSize = _geometry.TileSize;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = MazeOffsetY;
        var pulseBrush = _render.PulseBrush;
        var coreBrush = _render.CoreBrush;

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

        var tileSize = _geometry.TileSize;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = MazeOffsetY;
        var x = offsetX + (int)MathF.Round(runner.Position.X);
        var y = offsetY + (int)MathF.Round(runner.Position.Y);

        var shadowBrush = _render.RunnerShadowBrush;
        var runnerBrush = _render.RunnerBrush;
        runnerBrush.Color = color;
        graphics.FillRectangle(shadowBrush, x - 5, y - 5, 10, 10);

        var arrow = _render.RunnerTriangle;
        (arrow[0], arrow[1], arrow[2]) = runner.Facing switch
        {
            Direction.Up => (new Point(x, y - 5), new Point(x - 4, y + 4), new Point(x + 4, y + 4)),
            Direction.Down => (new Point(x, y + 5), new Point(x - 4, y - 4), new Point(x + 4, y - 4)),
            Direction.Left => (new Point(x - 5, y), new Point(x + 4, y - 4), new Point(x + 4, y + 4)),
            _ => (new Point(x + 5, y), new Point(x - 4, y - 4), new Point(x - 4, y + 4))
        };
        graphics.FillPolygon(runnerBrush, arrow);

        if (runner.AnimationFrame == 1)
        {
            graphics.FillRectangle(_render.CoreBrush, x - 1, y - 1, 2, 2);
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
        var offsetX = (LogicalWidth - _mazeLayer.Width) / 2;
        const int offsetY = MazeOffsetY;
        graphics.DrawImageUnscaled(_mazeLayer, offsetX, offsetY);
    }

    private Bitmap CreateMazeLayer()
    {
        var tileSize = _geometry.TileSize;
        var layer = new Bitmap(_maze.Width * tileSize, _maze.Height * tileSize);
        using var graphics = Graphics.FromImage(layer);
        var highContrast = _profile.Settings.HighContrast;
        using var wallBrush = new SolidBrush(
            highContrast ? Color.FromArgb(255, 20, 80, 190) : Color.FromArgb(255, 29, 75, 112));
        using var wallCoreBrush = new SolidBrush(
            highContrast ? Color.White : Color.FromArgb(255, 44, 210, 190));
        using var floorBrush = new SolidBrush(
            highContrast ? Color.Black : Color.FromArgb(255, 8, 13, 27));
        for (var y = 0; y < _maze.Height; y++)
        {
            for (var x = 0; x < _maze.Width; x++)
            {
                var pixelX = x * tileSize;
                var pixelY = y * tileSize;
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

        return layer;
    }

    private void RebuildMazeLayer()
    {
        var replacement = CreateMazeLayer();
        _mazeLayer.Dispose();
        _mazeLayer = replacement;
    }

    private void LoadCurrentLevel()
    {
        _maze = _levels.CurrentLevel.Maze;
        _enemies.Clear();
        _projectiles.Clear();
        RebuildMazeLayer();
        var spawns = SpawnPlanner.FindPlayerSpawns(_levels.CurrentLevel, _geometry.TileSize);
        if (_playerLife.IsAlive)
        {
            _runner.Respawn(spawns.PlayerOne);
        }

        if (IsCooperative && _playerTwoLife.IsAlive)
        {
            _runnerTwo.Respawn(spawns.PlayerTwo);
        }
    }

    private void HandleLevelAdvance()
    {
        if (!_levels.LevelAdvancedThisUpdate)
        {
            return;
        }

        var completedLevel = _levels.LevelNumber - 1;
        _audio.PlayRoundComplete();
        _score.RecordRoundCompleted(completedLevel, _playerLife.Lives);
        if (IsCooperative)
        {
            _scoreTwo.RecordRoundCompleted(completedLevel, _playerTwoLife.Lives);
            if (!_playerLife.IsGameOver && !_playerTwoLife.IsGameOver)
            {
                _score.RecordTeamSurvivalBonus(completedLevel);
                _scoreTwo.RecordTeamSurvivalBonus(completedLevel);
            }

            RecoverEliminatedPartner();
        }

        LoadCurrentLevel();
        _effects.Spawn(
            VisualEffectKind.RoundClear,
            new Vector2((_maze.Width * _geometry.TileSize) / 2f, (_maze.Height * _geometry.TileSize) / 2f),
            _profile.Settings.ReducedFlashes);
    }

    private void RenderLevelTransition(Graphics graphics)
    {
        var progress = _levels.TransitionProgress;
        var bandWidth = 110 + (int)(progress * 170);
        var left = (LogicalWidth - bandWidth) / 2;
        using var overlay = new SolidBrush(Color.FromArgb(225, 5, 7, 15));
        using var border = new Pen(Color.FromArgb(255, 55, 238, 210), 2);
        using var title = new Font(FontFamily.GenericMonospace, 21, FontStyle.Bold, GraphicsUnit.Pixel);
        graphics.FillRectangle(overlay, left, 108, bandWidth, 76);
        graphics.DrawRectangle(border, left, 108, bandWidth - 1, 75);
        DrawCentered(graphics, $"LEVEL {_levels.LevelNumber} COMPLETE", title, _render.GlowBrush, 121);
        DrawCentered(
            graphics,
            $"UPLINKING {_levels.LevelNumber + 1}: {LevelCatalog.Get(_levels.LevelNumber + 1).Name.ToUpperInvariant()}",
            _render.TextFont,
            _render.AccentBrush,
            157);
    }

    private void RenderEffects(Graphics graphics)
    {
        var tileSize = _geometry.TileSize;
        var offsetX = (LogicalWidth - (_maze.Width * tileSize)) / 2;
        const int offsetY = MazeOffsetY;
        var pen = _render.EffectPen;
        for (var i = 0; i < _effects.Capacity; i++)
        {
            var effect = _effects[i];
            if (!effect.Active)
            {
                continue;
            }

            var progress = effect.Age / effect.Duration;
            var alpha = _profile.Settings.ReducedFlashes
                ? (int)(90 * (1 - progress))
                : (int)(220 * (1 - progress));
            pen.Color = effect.Kind switch
            {
                VisualEffectKind.PlayerDamage => Color.FromArgb(alpha, 255, 70, 95),
                VisualEffectKind.RoundClear => Color.FromArgb(alpha, 80, 255, 210),
                _ => Color.FromArgb(alpha, 255, 120, 210)
            };
            var x = offsetX + (int)effect.Position.X;
            var y = offsetY + (int)effect.Position.Y;
            var radius = effect.Kind == VisualEffectKind.RoundClear
                ? 12 + (int)(progress * 80)
                : 2 + (int)(progress * 10);
            graphics.DrawRectangle(pen, x - radius, y - radius, radius * 2, radius * 2);
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
        if (_keyboard.WasPressed(Keys.F3))
        {
            _diagnosticsEnabled = !_diagnosticsEnabled;
            _log.Write($"Diagnostics {(_diagnosticsEnabled ? "enabled" : "disabled")}");
        }

        if (_keyboard.WasPressed(Keys.F2))
        {
            _profile.Settings.HighContrast = !_profile.Settings.HighContrast;
            RebuildMazeLayer();
            _audio.PlayMenuInteraction();
        }

        if (_keyboard.WasPressed(Keys.F4))
        {
            _profile.Settings.ReducedFlashes = !_profile.Settings.ReducedFlashes;
            _audio.PlayMenuInteraction();
        }

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
                    _audio.PlayMenuInteraction();
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
                    _audio.PlayMenuInteraction();
                    _flow.ShowInstructions();
                }

                break;
            case GameScreen.Instructions:
                if (_keyboard.WasPressed(Keys.Enter) || _keyboard.WasPressed(Keys.Escape))
                {
                    _audio.PlayMenuInteraction();
                    _flow.ReturnToTitle();
                }

                break;
            case GameScreen.Playing:
                if (_keyboard.WasPressed(Keys.P) || _keyboard.WasPressed(Keys.Escape))
                {
                    _audio.PlayMenuInteraction();
                    _flow.TogglePause();
                }

                break;
            case GameScreen.Paused:
                if (_keyboard.WasPressed(Keys.P) || _keyboard.WasPressed(Keys.Escape))
                {
                    _audio.PlayMenuInteraction();
                    _flow.TogglePause();
                }
                else if (_keyboard.WasPressed(Keys.R))
                {
                    ResetGame();
                    _flow.TogglePause();
                }
                else if (_keyboard.WasPressed(Keys.Q))
                {
                    _audio.PlayMenuInteraction();
                    _flow.ReturnToTitle();
                }

                break;
            case GameScreen.GameOver:
                if (_keyboard.WasPressed(Keys.Enter))
                {
                    _audio.PlayMenuInteraction();
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
        _effects.Clear();
        _levels.Reset();
        _score.Reset();
        _scoreTwo.Reset();
        _playerLife.Reset();
        _playerTwoLife.Reset();
        _maze = _levels.CurrentLevel.Maze;
        RebuildMazeLayer();
        var spawns = SpawnPlanner.FindPlayerSpawns(_levels.CurrentLevel, _geometry.TileSize);
        _runner.Respawn(spawns.PlayerOne);
        _runnerTwo.Respawn(spawns.PlayerTwo);
        _clock.Reset();
    }

    private void FirePulse(int playerId, Runner runner)
    {
        var origin = runner.Position + (runner.Facing.ToVector() * (runner.CollisionRadius + 2f));
        if (_projectiles.TryFire(playerId, origin, runner.Facing))
        {
            _audio.PlayFire();
        }
    }

    private void SelectAndStartMode(GameMode mode)
    {
        _audio.PlayMenuInteraction();
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
            _pendingScores.Add(new PendingScore(1, _score.Score, _levels.LevelNumber));
        }

        if (IsCooperative && _profile.QualifiesForHighScore(_scoreTwo.Score))
        {
            _pendingScores.Add(new PendingScore(2, _scoreTwo.Score, _levels.LevelNumber));
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
            _log.Write($"Profile save failed: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.WriteLine($"Profile save access failed: {exception}");
            _log.Write($"Profile save access failed: {exception.Message}");
        }
    }

    private void UpdateDiagnosticSample(double elapsedSeconds)
    {
        _diagnosticSampleSeconds += elapsedSeconds;
        if (_diagnosticSampleSeconds < 1)
        {
            return;
        }

        _framesPerSecond = _diagnosticFrames / _diagnosticSampleSeconds;
        var allocatedBytes = GC.GetTotalAllocatedBytes();
        _allocatedBytesPerSecond =
            (allocatedBytes - _lastAllocatedBytes) / _diagnosticSampleSeconds;
        _lastAllocatedBytes = allocatedBytes;
        _diagnosticFrames = 0;
        _diagnosticSampleSeconds = 0;
    }

    private void RenderDiagnostics(Graphics graphics)
    {
        using var background = new SolidBrush(Color.FromArgb(220, 0, 0, 0));
        using var foreground = new SolidBrush(Color.FromArgb(255, 120, 255, 190));
        using var font = new Font(FontFamily.GenericMonospace, 8, FontStyle.Regular, GraphicsUnit.Pixel);
        graphics.FillRectangle(background, 2, 30, 156, 73);
        graphics.DrawString(
            $"FPS {_framesPerSecond,5:0.0}  UPDATE {_lastUpdateMilliseconds,5:0.00}ms",
            font,
            foreground,
            5,
            33);
        graphics.DrawString(
            $"ALLOC {_allocatedBytesPerSecond / 1024,7:0.0} KiB/s",
            font,
            foreground,
            5,
            44);
        graphics.DrawString(
            $"STATE {_flow.Screen} MODE {_flow.Mode}",
            font,
            foreground,
            5,
            55);
        graphics.DrawString(
            $"LEVEL {_levels.LevelNumber} ENEMY {_enemies.ActiveCount} SHOT {_projectiles.ActiveCount}",
            font,
            foreground,
            5,
            66);
        graphics.DrawString(
            $"P1 {_playerLife.Lives}/{_playerLife.IsAlive} P2 {_playerTwoLife.Lives}/{_playerTwoLife.IsAlive}",
            font,
            foreground,
            5,
            77);
        graphics.DrawString(
            $"FX {_effects.ActiveCount} SEED 0x{_enemies.Seed:X8}",
            font,
            foreground,
            5,
            88);
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
