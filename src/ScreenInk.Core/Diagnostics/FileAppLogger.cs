using System.Globalization;
using System.Text;

namespace ScreenInk.Core.Diagnostics;

public sealed class FileAppLogger : IAppLogger, IDisposable
{
    private readonly Lock _sync = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    public FileAppLogger(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(
            new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };
    }

    public void Info(string message) => Write("INFO", message, exception: null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _writer.Dispose();
            _disposed = true;
        }
    }

    private void Write(string level, string message, Exception? exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var timestamp = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            _writer.Write(timestamp);
            _writer.Write(" [");
            _writer.Write(level);
            _writer.Write("] ");
            _writer.WriteLine(message);

            if (exception is not null)
            {
                _writer.WriteLine(exception);
            }
        }
    }
}
