namespace MazeHunter.Game.Audio;

/// <summary>Generates every original PCM effect used by Neon Labyrinth.</summary>
internal static class WaveformSynthesizer
{
    private const int SampleRate = 22050;

    public static MemoryStream CreateFirePulse() =>
        CreateSweep(760, 350, 0.09f, 0.22f, WaveShape.Square);

    public static MemoryStream CreateEnemyDestroyed() =>
        CreateSweep(260, 980, 0.14f, 0.2f, WaveShape.Triangle);

    public static MemoryStream CreatePlayerDamage() =>
        CreateSweep(520, 90, 0.28f, 0.24f, WaveShape.Saw);

    public static MemoryStream CreateRoundComplete() =>
        CreateNotes([440, 554, 659, 880], 0.075f, 0.18f);

    public static MemoryStream CreateMenuInteraction() =>
        CreateSweep(380, 520, 0.045f, 0.12f, WaveShape.Triangle);

    public static MemoryStream CreateGameOver() =>
        CreateNotes([330, 247, 185, 110], 0.14f, 0.21f);

    private static MemoryStream CreateSweep(
        float startFrequency,
        float endFrequency,
        float durationSeconds,
        float volume,
        WaveShape shape)
    {
        var sampleCount = (int)(SampleRate * durationSeconds);
        var samples = new short[sampleCount];
        var phase = 0d;
        for (var i = 0; i < sampleCount; i++)
        {
            var progress = i / (float)sampleCount;
            var frequency = startFrequency + ((endFrequency - startFrequency) * progress);
            phase += frequency / SampleRate;
            var envelope = (1f - progress) * (1f - progress);
            samples[i] = ToSample(Evaluate(shape, phase) * envelope, volume);
        }

        return WriteWave(samples);
    }

    private static MemoryStream CreateNotes(
        ReadOnlySpan<int> frequencies,
        float noteDuration,
        float volume)
    {
        var samplesPerNote = (int)(SampleRate * noteDuration);
        var samples = new short[samplesPerNote * frequencies.Length];
        var phase = 0d;
        for (var note = 0; note < frequencies.Length; note++)
        {
            for (var i = 0; i < samplesPerNote; i++)
            {
                var noteProgress = i / (float)samplesPerNote;
                phase += frequencies[note] / (double)SampleRate;
                var envelope = MathF.Sin(MathF.PI * noteProgress);
                samples[(note * samplesPerNote) + i] =
                    ToSample(Evaluate(WaveShape.Triangle, phase) * envelope, volume);
            }
        }

        return WriteWave(samples);
    }

    private static float Evaluate(WaveShape shape, double phase)
    {
        var cycle = (float)(phase % 1);
        return shape switch
        {
            WaveShape.Square => cycle < 0.5f ? 1f : -1f,
            WaveShape.Saw => (cycle * 2f) - 1f,
            _ => 1f - (4f * MathF.Abs(cycle - 0.5f))
        };
    }

    private static short ToSample(float value, float volume) =>
        (short)(Math.Clamp(value * volume, -1f, 1f) * short.MaxValue);

    private static MemoryStream WriteWave(ReadOnlySpan<short> samples)
    {
        var stream = new MemoryStream(44 + (samples.Length * sizeof(short)));
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            var dataLength = samples.Length * sizeof(short);
            writer.Write("RIFF"u8);
            writer.Write(36 + dataLength);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * sizeof(short));
            writer.Write((short)sizeof(short));
            writer.Write((short)16);
            writer.Write("data"u8);
            writer.Write(dataLength);
            foreach (var sample in samples)
            {
                writer.Write(sample);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private enum WaveShape : byte
    {
        Square,
        Triangle,
        Saw
    }
}

