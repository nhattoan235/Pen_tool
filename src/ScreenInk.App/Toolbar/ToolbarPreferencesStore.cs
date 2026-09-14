using System.IO;
using System.Text.Json;
using ScreenInk.Core.Diagnostics;

namespace ScreenInk.App.Toolbar;

internal sealed class ToolbarPreferencesStore
{
    private readonly string _path;
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public ToolbarPreferencesStore(string path, IAppLogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ToolbarPreferences Load()
    {
        if (!File.Exists(_path))
        {
            return ToolbarPreferences.Default;
        }

        try
        {
            var preferences = JsonSerializer.Deserialize<ToolbarPreferences>(File.ReadAllText(_path));
            return preferences is null ||
                preferences.ColorIndex < 0 ||
                !Enum.IsDefined(preferences.Theme) ||
                !Enum.IsDefined(preferences.Tool) ||
                !IsSizeIndexValid(preferences.PenSizeIndex) ||
                !IsSizeIndexValid(preferences.HighlighterSizeIndex) ||
                !IsSizeIndexValid(preferences.PixelEraserSizeIndex) ||
                !IsSizeIndexValid(preferences.ObjectEraserSizeIndex)
                ? ToolbarPreferences.Default
                : preferences;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to load toolbar preferences.", exception);
            return ToolbarPreferences.Default;
        }
    }

    public void Save(ToolbarPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        _writeLock.Wait();
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                _path,
                JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to save toolbar preferences.", exception);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task SaveAsync(ToolbarPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        await _writeLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(
                _path,
                JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to save toolbar preferences asynchronously.", exception);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static bool IsSizeIndexValid(int value) => value is >= 0 and <= 2;
}
