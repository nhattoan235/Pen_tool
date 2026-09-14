using System.ComponentModel;
using System.Windows.Interop;
using ScreenInk.App.Interop;
using ScreenInk.Core.Diagnostics;

namespace ScreenInk.App.Services;

internal sealed class GlobalHotkeyService : IDisposable
{
    private const int ToggleDrawingId = 1;
    private const int EscapeDrawingId = 2;
    private const int ClearDrawingId = 3;
    private const int UndoDrawingId = 4;
    private const int RedoShiftZDrawingId = 5;
    private const int RedoYDrawingId = 6;
    private const int ColorTopRowIdBase = 10;
    private const int ColorNumPadIdBase = 20;
    private const int ColorShortcutCount = 5;

    private readonly Action _toggleDrawing;
    private readonly Action _returnToPointer;
    private readonly Action _clearDrawings;
    private readonly Action _undoLastStroke;
    private readonly Action _redoLastAction;
    private readonly Action<int> _selectColor;
    private readonly IAppLogger _logger;
    private readonly HwndSource _source;
    private bool _toggleRegistered;
    private ToggleHotkeyPreset? _toggleHotkeyPreset;
    private bool _escapeRegistered;
    private bool _clearRegistered;
    private bool _undoRegistered;
    private bool _redoShiftZRegistered;
    private bool _redoYRegistered;
    private readonly HashSet<int> _registeredColorHotkeyIds = [];
    private readonly Dictionary<int, int> _colorIndexByHotkeyId = [];
    private bool _drawingShortcutsEnabled;
    private bool _disposed;

    public GlobalHotkeyService(
        Action toggleDrawing,
        Action returnToPointer,
        Action clearDrawings,
        Action undoLastStroke,
        Action redoLastAction,
        Action<int> selectColor,
        IAppLogger logger)
    {
        _toggleDrawing = toggleDrawing ?? throw new ArgumentNullException(nameof(toggleDrawing));
        _returnToPointer = returnToPointer ?? throw new ArgumentNullException(nameof(returnToPointer));
        _clearDrawings = clearDrawings ?? throw new ArgumentNullException(nameof(clearDrawings));
        _undoLastStroke = undoLastStroke ?? throw new ArgumentNullException(nameof(undoLastStroke));
        _redoLastAction = redoLastAction ?? throw new ArgumentNullException(nameof(redoLastAction));
        _selectColor = selectColor ?? throw new ArgumentNullException(nameof(selectColor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var parameters = new HwndSourceParameters("ScreenInk.Hotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WindowProcedure);
    }

    public bool RegisterToggleDrawing(ToggleHotkeyPreset preset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return TryRegisterToggleDrawing(preset);
    }

    public bool TryChangeToggleDrawing(ToggleHotkeyPreset preset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_toggleRegistered && _toggleHotkeyPreset == preset)
        {
            return true;
        }

        var previousPreset = _toggleHotkeyPreset;
        if (_toggleRegistered)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, ToggleDrawingId);
            _toggleRegistered = false;
        }

        if (TryRegisterToggleDrawing(preset))
        {
            return true;
        }

        if (previousPreset is { } rollbackPreset)
        {
            _ = TryRegisterToggleDrawing(rollbackPreset);
        }

        return false;
    }

    private bool TryRegisterToggleDrawing(ToggleHotkeyPreset preset)
    {
        var definition = preset.GetDefinition();

        _toggleRegistered = NativeMethods.RegisterHotKey(
            _source.Handle,
            ToggleDrawingId,
            definition.Modifiers,
            definition.VirtualKey);

        if (!_toggleRegistered)
        {
            _logger.Error(
                $"Unable to register {definition.DisplayName}.",
                new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
            return false;
        }

        _toggleHotkeyPreset = preset;
        _logger.Info($"Registered drawing toggle hotkey {definition.DisplayName}.");
        return true;
    }

    public void SetDrawingShortcutsEnabled(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (enabled == _drawingShortcutsEnabled)
        {
            return;
        }

        if (enabled)
        {
            _escapeRegistered = NativeMethods.RegisterHotKey(
                _source.Handle,
                EscapeDrawingId,
                NativeMethods.ModNone,
                NativeMethods.VkEscape);

            if (!_escapeRegistered)
            {
                _logger.Error(
                    "Unable to register temporary Escape hotkey.",
                    new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
            }

            _clearRegistered = NativeMethods.RegisterHotKey(
                _source.Handle,
                ClearDrawingId,
                NativeMethods.ModNone,
                NativeMethods.VkDelete);

            if (!_clearRegistered)
            {
                _logger.Error(
                    "Unable to register temporary Delete hotkey.",
                    new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
            }

            _undoRegistered = NativeMethods.RegisterHotKey(
                _source.Handle,
                UndoDrawingId,
                NativeMethods.ModControl,
                NativeMethods.VkZ);

            if (!_undoRegistered)
            {
                _logger.Error(
                    "Unable to register temporary Ctrl+Z hotkey.",
                    new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
            }

            _redoShiftZRegistered = NativeMethods.RegisterHotKey(
                _source.Handle,
                RedoShiftZDrawingId,
                NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModNoRepeat,
                NativeMethods.VkZ);
            if (!_redoShiftZRegistered)
            {
                LogRegistrationFailure("Ctrl+Shift+Z");
            }

            _redoYRegistered = NativeMethods.RegisterHotKey(
                _source.Handle,
                RedoYDrawingId,
                NativeMethods.ModControl | NativeMethods.ModNoRepeat,
                NativeMethods.VkY);
            if (!_redoYRegistered)
            {
                LogRegistrationFailure("Ctrl+Y");
            }

            RegisterColorHotkeys();
            _drawingShortcutsEnabled = true;

            return;
        }

        _ = NativeMethods.UnregisterHotKey(_source.Handle, EscapeDrawingId);
        _ = NativeMethods.UnregisterHotKey(_source.Handle, ClearDrawingId);
        _ = NativeMethods.UnregisterHotKey(_source.Handle, UndoDrawingId);
        _ = NativeMethods.UnregisterHotKey(_source.Handle, RedoShiftZDrawingId);
        _ = NativeMethods.UnregisterHotKey(_source.Handle, RedoYDrawingId);
        _escapeRegistered = false;
        _clearRegistered = false;
        _undoRegistered = false;
        _redoShiftZRegistered = false;
        _redoYRegistered = false;
        UnregisterColorHotkeys();
        _drawingShortcutsEnabled = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_escapeRegistered)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, EscapeDrawingId);
        }

        if (_clearRegistered)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, ClearDrawingId);
        }

        if (_undoRegistered)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, UndoDrawingId);
        }

        if (_redoShiftZRegistered)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, RedoShiftZDrawingId);
        }

        if (_redoYRegistered)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, RedoYDrawingId);
        }

        UnregisterColorHotkeys();

        if (_toggleRegistered)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, ToggleDrawingId);
        }

        _source.RemoveHook(WindowProcedure);
        _source.Dispose();
        _disposed = true;
    }

    private nint WindowProcedure(
        nint windowHandle,
        int message,
        nint wordParameter,
        nint longParameter,
        ref bool handled)
    {
        if (message != NativeMethods.WmHotkey)
        {
            return nint.Zero;
        }

        switch (wordParameter.ToInt32())
        {
            case ToggleDrawingId:
                _toggleDrawing();
                handled = true;
                break;
            case EscapeDrawingId:
                _returnToPointer();
                handled = true;
                break;
            case ClearDrawingId:
                _clearDrawings();
                handled = true;
                break;
            case UndoDrawingId:
                _undoLastStroke();
                handled = true;
                break;
            case RedoShiftZDrawingId:
            case RedoYDrawingId:
                _redoLastAction();
                handled = true;
                break;
            default:
                if (_colorIndexByHotkeyId.TryGetValue(wordParameter.ToInt32(), out var colorIndex))
                {
                    _selectColor(colorIndex);
                    handled = true;
                }

                break;
        }

        return nint.Zero;
    }

    private void RegisterColorHotkeys()
    {
        for (var colorIndex = 0; colorIndex < ColorShortcutCount; colorIndex++)
        {
            RegisterColorHotkey(
                ColorTopRowIdBase + colorIndex,
                NativeMethods.Vk1 + (uint)colorIndex,
                colorIndex,
                $"{colorIndex + 1}");
            RegisterColorHotkey(
                ColorNumPadIdBase + colorIndex,
                NativeMethods.VkNumPad1 + (uint)colorIndex,
                colorIndex,
                $"Numpad {colorIndex + 1}");
        }
    }

    private void RegisterColorHotkey(int id, uint virtualKey, int colorIndex, string label)
    {
        var registered = NativeMethods.RegisterHotKey(
            _source.Handle,
            id,
            NativeMethods.ModNone | NativeMethods.ModNoRepeat,
            virtualKey);

        if (!registered)
        {
            _logger.Error(
                $"Unable to register temporary {label} color hotkey.",
                new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
            return;
        }

        _registeredColorHotkeyIds.Add(id);
        _colorIndexByHotkeyId[id] = colorIndex;
    }

    private void UnregisterColorHotkeys()
    {
        foreach (var id in _registeredColorHotkeyIds)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, id);
        }

        _registeredColorHotkeyIds.Clear();
        _colorIndexByHotkeyId.Clear();
    }

    private void LogRegistrationFailure(string shortcut)
    {
        _logger.Error(
            $"Unable to register temporary {shortcut} hotkey.",
            new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()));
    }
}
