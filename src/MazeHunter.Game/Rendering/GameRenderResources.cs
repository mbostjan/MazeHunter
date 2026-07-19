namespace MazeHunter.Game.Rendering;

/// <summary>Reusable GDI objects for the steady gameplay render path.</summary>
internal sealed class GameRenderResources : IDisposable
{
    public Font HeaderFont { get; } =
        new(FontFamily.GenericMonospace, 13, FontStyle.Bold, GraphicsUnit.Pixel);

    public Font TextFont { get; } =
        new(FontFamily.GenericMonospace, 9, FontStyle.Bold, GraphicsUnit.Pixel);

    public SolidBrush GlowBrush { get; } = new(Color.White);

    public SolidBrush AccentBrush { get; } = new(Color.FromArgb(255, 240, 85, 150));

    public SolidBrush DimBrush { get; } = new(Color.FromArgb(255, 150, 164, 190));

    public SolidBrush PulseBrush { get; } = new(Color.White);

    public SolidBrush CoreBrush { get; } = new(Color.White);

    public SolidBrush EnemyShellBrush { get; } = new(Color.White);

    public SolidBrush EnemyEyeBrush { get; } = new(Color.FromArgb(255, 110, 245, 255));

    public SolidBrush BackgroundBrush { get; } = new(Color.FromArgb(255, 8, 13, 27));

    public SolidBrush RunnerShadowBrush { get; } = new(Color.FromArgb(255, 30, 52, 80));

    public SolidBrush RunnerBrush { get; } = new(Color.White);

    public Pen EffectPen { get; } = new(Color.White, 1);

    public Point[] RunnerTriangle { get; } = new Point[3];

    public void Dispose()
    {
        HeaderFont.Dispose();
        TextFont.Dispose();
        GlowBrush.Dispose();
        AccentBrush.Dispose();
        DimBrush.Dispose();
        PulseBrush.Dispose();
        CoreBrush.Dispose();
        EnemyShellBrush.Dispose();
        EnemyEyeBrush.Dispose();
        BackgroundBrush.Dispose();
        RunnerShadowBrush.Dispose();
        RunnerBrush.Dispose();
        EffectPen.Dispose();
    }
}
