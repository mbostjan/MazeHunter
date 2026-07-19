using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using MazeHunter.Core.Timing;

namespace MazeHunter.Game.Application;

internal sealed class GameForm : Form
{
    private const int LogicalWidth = 320;
    private const int LogicalHeight = 240;

    private readonly Bitmap _framebuffer = new(LogicalWidth, LogicalHeight);
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly FixedStepClock _clock = new();
    private readonly System.Windows.Forms.Timer _pump = new() { Interval = 1 };
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
        Deactivate += (_, _) => _clock.Reset();
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
    }

    private void RenderLogicalFrame()
    {
        using var graphics = Graphics.FromImage(_framebuffer);
        graphics.Clear(Color.FromArgb(5, 7, 15));
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        using var borderPen = new Pen(Color.FromArgb(35, 214, 190), 2);
        graphics.DrawRectangle(borderPen, 18, 20, 283, 199);

        var pulse = (int)(20 + (Math.Sin(_presentationTime * 3) + 1) * 25);
        using var glowBrush = new SolidBrush(Color.FromArgb(255, 55 + pulse, 218, 190));
        using var titleFont = new Font(FontFamily.GenericMonospace, 22, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textFont = new Font(FontFamily.GenericMonospace, 9, FontStyle.Bold, GraphicsUnit.Pixel);

        DrawCentered(graphics, "NEON LABYRINTH", titleFont, glowBrush, 74);
        using var accentBrush = new SolidBrush(Color.FromArgb(255, 240, 85, 150));
        DrawCentered(graphics, "SIGNAL ACQUIRED", textFont, accentBrush, 114);
        using var dimBrush = new SolidBrush(Color.FromArgb(255, 150, 164, 190));
        DrawCentered(graphics, "SYSTEM CORE // MILESTONE 1", textFont, dimBrush, 142);
        DrawCentered(graphics, "FIXED 60 HZ SIMULATION ONLINE", textFont, dimBrush, 158);
        DrawCentered(graphics, "CLOSE WINDOW TO EXIT", textFont, dimBrush, 190);
    }

    private static void DrawCentered(Graphics graphics, string text, Font font, Brush brush, float y)
    {
        var size = graphics.MeasureString(text, font);
        graphics.DrawString(text, font, brush, (LogicalWidth - size.Width) / 2, y);
    }
}

