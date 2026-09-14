using Microsoft.Win32;
using ScreenInk.Core.Displays;
using Forms = System.Windows.Forms;

namespace ScreenInk.App.Services;

internal sealed class DisplayTopologyService : IDisposable
{
    public DisplayTopologyService()
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public event EventHandler? DisplaysChanged;

    public IReadOnlyList<DisplayDescriptor> GetDisplays()
    {
        return Forms.Screen.AllScreens
            .Select(screen => new DisplayDescriptor(
                screen.DeviceName,
                new DisplayBounds(
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    screen.Bounds.Width,
                    screen.Bounds.Height),
                screen.Primary))
            .ToArray();
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        DisplaysChanged?.Invoke(this, EventArgs.Empty);
    }
}
