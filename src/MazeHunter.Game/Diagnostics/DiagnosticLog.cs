namespace MazeHunter.Game.Diagnostics;

internal sealed class DiagnosticLog
{
    private const long MaximumLogBytes = 1_000_000;
    private readonly string _path;

    public DiagnosticLog(string directory)
    {
        _path = Path.Combine(directory, "diagnostics.log");
    }

    public void Write(string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(_path) && new FileInfo(_path).Length > MaximumLogBytes)
            {
                File.Move(_path, _path + ".previous", overwrite: true);
            }

            File.AppendAllText(_path, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Logging must never interfere with startup, gameplay, or shutdown.
        }
        catch (UnauthorizedAccessException)
        {
            // A read-only profile location is recoverable.
        }
    }
}

