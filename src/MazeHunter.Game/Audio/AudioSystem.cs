using System.Media;

namespace MazeHunter.Game.Audio;

/// <summary>Owns preloaded original waveforms; every Play call is asynchronous.</summary>
internal sealed class AudioSystem : IDisposable
{
    private readonly SoundAsset _fire = new(WaveformSynthesizer.CreateFirePulse());
    private readonly SoundAsset _destroy = new(WaveformSynthesizer.CreateEnemyDestroyed());
    private readonly SoundAsset _damage = new(WaveformSynthesizer.CreatePlayerDamage());
    private readonly SoundAsset _round = new(WaveformSynthesizer.CreateRoundComplete());
    private readonly SoundAsset _menu = new(WaveformSynthesizer.CreateMenuInteraction());
    private readonly SoundAsset _gameOver = new(WaveformSynthesizer.CreateGameOver());

    public bool Muted { get; private set; }

    public void ToggleMute() => Muted = !Muted;

    public void SetMuted(bool muted) => Muted = muted;

    public void PlayFire() => Play(_fire);

    public void PlayEnemyDestroyed() => Play(_destroy);

    public void PlayPlayerDamage() => Play(_damage);

    public void PlayRoundComplete() => Play(_round);

    public void PlayMenuInteraction() => Play(_menu);

    public void PlayGameOver() => Play(_gameOver);

    public void Dispose()
    {
        _fire.Dispose();
        _destroy.Dispose();
        _damage.Dispose();
        _round.Dispose();
        _menu.Dispose();
        _gameOver.Dispose();
    }

    private void Play(SoundAsset sound)
    {
        if (!Muted)
        {
            sound.Play();
        }
    }

    private sealed class SoundAsset : IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly SoundPlayer _player;

        public SoundAsset(MemoryStream stream)
        {
            _stream = stream;
            _player = new SoundPlayer(stream);
            _player.Load();
        }

        public void Play() => _player.Play();

        public void Dispose()
        {
            _player.Dispose();
            _stream.Dispose();
        }
    }
}
