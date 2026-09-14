using ScreenInk.App.Interop;

namespace ScreenInk.App.Services;

internal enum ToggleHotkeyPreset
{
    ControlShiftD,
    ControlAltD,
    ControlShiftSpace,
    ControlAltSpace,
}

internal readonly record struct ToggleHotkeyDefinition(
    string DisplayName,
    uint Modifiers,
    uint VirtualKey);

internal static class ToggleHotkeyPresets
{
    public static ToggleHotkeyDefinition GetDefinition(this ToggleHotkeyPreset preset) => preset switch
    {
        ToggleHotkeyPreset.ControlShiftD => new(
            "Ctrl+Shift+D",
            NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModNoRepeat,
            NativeMethods.VkD),
        ToggleHotkeyPreset.ControlAltD => new(
            "Ctrl+Alt+D",
            NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModNoRepeat,
            NativeMethods.VkD),
        ToggleHotkeyPreset.ControlShiftSpace => new(
            "Ctrl+Shift+Space",
            NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModNoRepeat,
            NativeMethods.VkSpace),
        ToggleHotkeyPreset.ControlAltSpace => new(
            "Ctrl+Alt+Space",
            NativeMethods.ModControl | NativeMethods.ModAlt | NativeMethods.ModNoRepeat,
            NativeMethods.VkSpace),
        _ => throw new ArgumentOutOfRangeException(nameof(preset)),
    };
}
