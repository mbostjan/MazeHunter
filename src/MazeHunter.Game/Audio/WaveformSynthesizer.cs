namespace MazeHunter.Game.Audio;

internal static class WaveformSynthesizer
{
    private const int SampleRate = 22050;

    public static MemoryStream CreateFirePulse()
    {
        const float durationSeconds = 0.09f;
        var sampleCount = (int)(SampleRate * durationSeconds);
        var samples = new short[sampleCount];
        var phase = 0d;

        for (var i = 0; i < sampleCount; i++)
        {
            var progress = i / (float)sampleCount;
            var frequency = 760f - (progress * 410f);
            phase += frequency / SampleRate;
            var square = phase % 1 < 0.5 ? 1f : -1f;
            var envelope = (1f - progress) * (1f - progress);
            samples[i] = (short)(square * envelope * short.MaxValue * 0.22f);
        }

        return WriteWave(samples);
    }

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
}

