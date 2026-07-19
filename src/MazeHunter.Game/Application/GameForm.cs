using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using MazeHunter.Core.Mazes;
using MazeHunter.Core.Timing;
using MazeHunter.Game.Input;

namespace MazeHunter.Game.Application;

internal sealed class GameForm : Form
{
    private const int LogicalWidth = 320;
    private const int LogicalHeight = 240;

    private readonly Bitmap _framebuffer = new(LogicalWidth, LogicalHeight);
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly FixedStepClock _clock = new();
    private readonly System.Windows.Forms.Timer _pump = new() { Interval = 1 };
    private readonly KeyboardState _keyboard = new();
    private readonly Maze _maze = MazeCatalog.CreateSignalCrossing();
    private long _previousTicks;
    private double _presentationTime;

    public GameForm()
    {
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

        DrawCentered(graphics, "NEON LABYRINTH // GRID PREVIEW", titleFont, glowBrush, 8);
        RenderMaze(graphics);

        using var accentBrush = new SolidBrush(Color.FromArgb(255, 240, 85, 150));
        var inputSignal = GetInputSignal();
        DrawCentered(graphics, inputSignal, textFont, accentBrush, 222);
        using var dimBrush = new SolidBrush(Color.FromArgb(255, 150, 164, 190));
        DrawCentered(graphics, "WASD + ARROWS INPUT MONITOR", textFont, dimBrush, 232);
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

    private string GetInputSignal()
    {
        Span<char> signal = stackalloc char[4];
        signal[0] = _keyboard.IsDown(Keys.W) || _keyboard.IsDown(Keys.Up) ? '^' : '-';
        signal[1] = _keyboard.IsDown(Keys.A) || _keyboard.IsDown(Keys.Left) ? '<' : '-';
        signal[2] = _keyboard.IsDown(Keys.S) || _keyboard.IsDown(Keys.Down) ? 'v' : '-';
        signal[3] = _keyboard.IsDown(Keys.D) || _keyboard.IsDown(Keys.Right) ? '>' : '-';
        return $"INPUT [{new string(signal)}]";
    }

    private static void DrawCentered(Graphics graphics, string text, Font font, Brush brush, float y)
    {
        var size = graphics.MeasureString(text, font);
        graphics.DrawString(text, font, brush, (LogicalWidth - size.Width) / 2, y);
    }
}
