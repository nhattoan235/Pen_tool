namespace ScreenInk.App.Services;

internal sealed record AppPreferences
{
    public static AppPreferences Default { get; } = new();

    public ToggleHotkeyPreset ToggleHotkey { get; init; } = ToggleHotkeyPreset.ControlShiftD;

    public bool StartWithWindows { get; init; }
}
