using System.IO;
using System.Text.Json;
using ScreenInk.Core.Diagnostics;

namespace ScreenInk.App.Toolbar;

internal sealed class ToolbarPlacementStore
{
    private readonly string _path;
    private readonly IAppLogger _logger;

    public ToolbarPlacementStore(string path, IAppLogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ToolbarPlacement? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<ToolbarPlacement>(json);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to load toolbar placement.", exception);
            return null;
        }
    }

    public void Save(ToolbarPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                _path,
                JsonSerializer.Serialize(placement, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to save toolbar placement.", exception);
        }
    }
}
