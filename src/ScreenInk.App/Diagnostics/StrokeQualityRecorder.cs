using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenInk.Core.Diagnostics;

namespace ScreenInk.App.Diagnostics;

internal sealed class StrokeQualityRecorder : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _directory;
    private readonly IAppLogger _logger;
    private readonly object _taskSync = new();
    private readonly HashSet<Task> _pendingWrites = [];
    private bool _disposed;

    public StrokeQualityRecorder(string dataDirectory, bool enabled, IAppLogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        IsEnabled = enabled;
        _directory = Path.Combine(Path.GetFullPath(dataDirectory), "diagnostics", "stroke-quality");

        if (IsEnabled)
        {
            Directory.CreateDirectory(_directory);
            _logger.Info($"Stroke quality diagnostics enabled at {_directory}.");
        }
    }

    public bool IsEnabled { get; }

    public void Record(StrokeQualitySample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (!IsEnabled || _disposed)
        {
            return;
        }

        var fileName = $"{sample.CapturedAtUtc:yyyyMMdd-HHmmss-fff}_{sample.StrokeId:N}.json";
        var path = Path.Combine(_directory, fileName);
        var write = Task.Run(async () =>
        {
            try
            {
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(sample, JsonOptions));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.Error("Unable to write stroke quality sample.", exception);
            }
        });

        lock (_taskSync)
        {
            _pendingWrites.Add(write);
        }

        _ = write.ContinueWith(
            completed =>
            {
                lock (_taskSync)
                {
                    _pendingWrites.Remove(completed);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Task[] pending;
        lock (_taskSync)
        {
            pending = _pendingWrites.ToArray();
        }

        try
        {
            Task.WaitAll(pending, TimeSpan.FromSeconds(2));
        }
        catch (AggregateException exception)
        {
            _logger.Error("Unable to finish all stroke quality diagnostic writes.", exception);
        }
    }
}
