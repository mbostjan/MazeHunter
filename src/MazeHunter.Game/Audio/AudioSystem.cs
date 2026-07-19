using System.Media;

namespace MazeHunter.Game.Audio;

/// <summary>Owns preloaded original waveforms; SoundPlayer.Play is asynchronous.</summary>
internal sealed class AudioSystem : IDisposable
{
    private readonly MemoryStream _fireStream = WaveformSynthesizer.CreateFirePulse();
    private readonly SoundPlayer _firePlayer;

    public AudioSystem()
    {
        _firePlayer = new SoundPlayer(_fireStream);
        _firePlayer.Load();
    }

    public bool Muted { get; private set; }

    public void ToggleMute() => Muted = !Muted;

    public void PlayFire()
    {
        if (!Muted)
        {
            _firePlayer.Play();
        }
    }

    public void Dispose()
    {
        _firePlayer.Dispose();
        _fireStream.Dispose();
    }
}

