using System.IO;
using System.Text.Json;
using ScreenInk.Core.Diagnostics;

namespace ScreenInk.App.Services;

internal sealed class AppPreferencesStore
{
    private readonly string _path;
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public AppPreferencesStore(string path, IAppLogger logger)
    {
        _path = Path.GetFullPath(path);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AppPreferences Load()
    {
        if (!File.Exists(_path))
        {
            return AppPreferences.Default;
        }

        try
        {
            var preferences = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(_path));
            return preferences is not null && Enum.IsDefined(preferences.ToggleHotkey)
                ? preferences
                : AppPreferences.Default;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to load app preferences; defaults restored.", exception);
            return AppPreferences.Default;
        }
    }

    public async Task SaveAsync(AppPreferences preferences)
    {
        await _writeLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = _path + ".tmp";
            await File.WriteAllTextAsync(
                temporaryPath,
                JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, _path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to save app preferences.", exception);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
