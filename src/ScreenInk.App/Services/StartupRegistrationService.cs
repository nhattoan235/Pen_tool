using System.IO;
using System.Security;
using ScreenInk.Core.Diagnostics;

namespace ScreenInk.App.Services;

internal sealed class StartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ScreenInk";

    private readonly IAppLogger _logger;

    public StartupRegistrationService(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                throw new InvalidOperationException("Unable to open the current-user startup registry key.");
            }

            if (enabled)
            {
                var executablePath = Environment.ProcessPath
                    ?? throw new InvalidOperationException("Unable to locate the Screen Ink executable.");
                key.SetValue(ValueName, $"\"{executablePath}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            _logger.Info($"Start with Windows {(enabled ? "enabled" : "disabled")}.");
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or SecurityException or InvalidOperationException)
        {
            _logger.Error("Unable to update Start with Windows.", exception);
            return false;
        }
    }
}
