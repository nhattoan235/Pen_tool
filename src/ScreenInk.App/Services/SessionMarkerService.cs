using System.IO;
using ScreenInk.Core.Diagnostics;

namespace ScreenInk.App.Services;

internal sealed class SessionMarkerService
{
    private readonly string _markerPath;
    private readonly IAppLogger _logger;

    public SessionMarkerService(string dataDirectory, IAppLogger logger)
    {
        _markerPath = Path.Combine(dataDirectory, "state", "running.marker");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool BeginSession()
    {
        var previousSessionWasUnclean = File.Exists(_markerPath);
        try
        {
            var directory = Path.GetDirectoryName(_markerPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                _markerPath,
                $"pid={Environment.ProcessId}{Environment.NewLine}startedUtc={DateTimeOffset.UtcNow:O}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to create the session marker.", exception);
        }

        return previousSessionWasUnclean;
    }

    public void CompleteSession()
    {
        try
        {
            File.Delete(_markerPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.Error("Unable to remove the session marker.", exception);
        }
    }
}
