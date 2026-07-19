using System.Diagnostics;
using MazeHunter.Core.Persistence;

namespace MazeHunter.Game.Persistence;

internal sealed class ProfileStore
{
    private readonly string _directory;
    private readonly string _profilePath;

    public ProfileStore(string? directory = null)
    {
        _directory = directory ??
            Environment.GetEnvironmentVariable("NEON_LABYRINTH_DATA_DIR") ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NeonLabyrinth");
        _profilePath = Path.Combine(_directory, "profile.json");
    }

    public string ProfilePath => _profilePath;

    public PlayerProfile Load()
    {
        if (!File.Exists(_profilePath))
        {
            return new PlayerProfile();
        }

        try
        {
            var json = File.ReadAllText(_profilePath);
            var profile = ProfileJson.DeserializeOrDefault(json, out var recovered);
            if (recovered)
            {
                QuarantineInvalidProfile();
            }

            return profile;
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"Profile load failed: {exception}");
            return new PlayerProfile();
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.WriteLine($"Profile access failed: {exception}");
            return new PlayerProfile();
        }
    }

    public void Save(PlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Directory.CreateDirectory(_directory);
        var temporaryPath = _profilePath + ".tmp";
        File.WriteAllText(temporaryPath, ProfileJson.Serialize(profile));
        File.Move(temporaryPath, _profilePath, overwrite: true);
    }

    private void QuarantineInvalidProfile()
    {
        try
        {
            var quarantinePath = Path.Combine(
                _directory,
                $"profile.corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.json");
            File.Move(_profilePath, quarantinePath, overwrite: false);
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"Profile quarantine failed: {exception}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.WriteLine($"Profile quarantine access failed: {exception}");
        }
    }
}
