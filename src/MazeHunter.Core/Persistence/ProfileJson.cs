using System.Text.Json;
using System.Text.Json.Serialization;

namespace MazeHunter.Core.Persistence;

public static class ProfileJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize(PlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Normalize();
        return JsonSerializer.Serialize(profile, Options);
    }

    public static PlayerProfile DeserializeOrDefault(string? json, out bool recoveredFromInvalidData)
    {
        try
        {
            var profile = JsonSerializer.Deserialize<PlayerProfile>(json ?? string.Empty, Options);
            if (profile is null || profile.Version > PlayerProfile.CurrentVersion || profile.Version < 0)
            {
                recoveredFromInvalidData = true;
                return new PlayerProfile();
            }

            profile.Normalize();
            recoveredFromInvalidData = false;
            return profile;
        }
        catch (JsonException)
        {
            recoveredFromInvalidData = true;
            return new PlayerProfile();
        }
    }
}

