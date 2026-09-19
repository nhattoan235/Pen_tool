using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using ScreenInk.App.Interop;
using ScreenInk.Core.Diagnostics;
using ScreenInk.Core.Interaction;

namespace ScreenInk.App.Services;

internal sealed class MouseShortcutService : IDisposable
{
    private const int WheelDeltaPerDetent = 120;
    private const int WheelCommitDelayMilliseconds = 45;
    private const int PlainWheelPassThroughMilliseconds = 350;

    private readonly Action<int> _cycleTool;
    private readonly Action<int> _adjustSize;
    private readonly Action<int, int> _pointerClick;
    private readonly Action<bool> _setWheelPassThrough;
    private readonly IAppLogger _logger;
    private readonly Dispatcher _dispatcher;
    private readonly NativeMethods.LowLevelMouseProcedure _hookProcedure;
    private readonly object _wheelSync = new();
    private readonly System.Threading.Timer _toolCommitTimer;
    private readonly System.Threading.Timer _sizeCommitTimer;
    private readonly DispatcherTimer _plainWheelPassThroughTimer;
    private nint _hookHandle;
    private int _toolWheelRemainder;
    private int _sizeWheelRemainder;
    private int _pendingToolSteps;
    private int _pendingSizeSteps;
    private InteractionMode _mode = InteractionMode.Pointer;
    private bool _plainWheelPassThroughActive;
    private bool _disposed;

    public MouseShortcutService(
        Action<int> cycleTool,
        Action<int> adjustSize,
        Action<int, int> pointerClick,
        Action<bool> setWheelPassThrough,
        IAppLogger logger)
    {
        _cycleTool = cycleTool ?? throw new ArgumentNullException(nameof(cycleTool));
        _adjustSize = adjustSize ?? throw new ArgumentNullException(nameof(adjustSize));
        _pointerClick = pointerClick ?? throw new ArgumentNullException(nameof(pointerClick));
        _setWheelPassThrough = setWheelPassThrough ?? throw new ArgumentNullException(nameof(setWheelPassThrough));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = Dispatcher.CurrentDispatcher;
        _hookProcedure = OnMouseHook;
        _toolCommitTimer = new System.Threading.Timer(
            OnToolCommitTimer,
            state: null,
            Timeout.Infinite,
            Timeout.Infinite);
        _sizeCommitTimer = new System.Threading.Timer(
            OnSizeCommitTimer,
            state: null,
            Timeout.Infinite,
            Timeout.Infinite);
        _plainWheelPassThroughTimer = new DispatcherTimer(DispatcherPriority.Input, _dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(PlainWheelPassThroughMilliseconds),
        };
        _plainWheelPassThroughTimer.Tick += OnPlainWheelPassThroughTimer;

        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WhMouseLowLevel,
            _hookProcedure,
            NativeMethods.GetModuleHandle(null),
            0);
        if (_hookHandle == nint.Zero)
        {
            _logger.Error(
                "Unable to install low-level mouse shortcut hook.",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }
        else
        {
            _logger.Info("Installed low-level mouse shortcut hook.");
        }
    }

    public void SetMode(InteractionMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EndPlainWheelPassThrough();
        _mode = mode;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        EndPlainWheelPassThrough();
        _plainWheelPassThroughTimer.Tick -= OnPlainWheelPassThroughTimer;
        _toolCommitTimer.Dispose();
        _sizeCommitTimer.Dispose();

        if (_hookHandle != nint.Zero)
        {
            _ = NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = nint.Zero;
        }
    }

    private nint OnMouseHook(int code, nint wordParameter, nint longParameter)
    {
        if (code < 0 || _disposed)
        {
            return NativeMethods.CallNextHookEx(_hookHandle, code, wordParameter, longParameter);
        }

        var message = wordParameter.ToInt32();
        var hookData = Marshal.PtrToStructure<LowLevelMouseHookData>(longParameter);

        if (message == NativeMethods.WmLeftButtonDown && _mode == InteractionMode.Pointer)
        {
            DispatchPointerAction(_pointerClick, hookData.Point.X, hookData.Point.Y);
        }
        else if (message == NativeMethods.WmMouseWheel && _mode == InteractionMode.Draw)
        {
            var wheelDelta = unchecked((short)(hookData.MouseData >> 16));
            if (IsExactModifier(NativeMethods.VkShift))
            {
                QueueEndPlainWheelPassThrough();
                QueueToolWheel(wheelDelta);
                return new nint(1);
            }

            if (IsExactModifier(NativeMethods.VkControl))
            {
                QueueEndPlainWheelPassThrough();
                QueueSizeWheel(wheelDelta);
                return new nint(1);
            }

            if (!HasAnyModifier())
            {
                QueuePlainWheelPassThrough();
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, code, wordParameter, longParameter);
    }

    private void DispatchPointerAction(Action<int, int> action, int x, int y)
    {
        if (_disposed || _dispatcher.HasShutdownStarted)
        {
            return;
        }

        _dispatcher.BeginInvoke(
            () =>
            {
                if (!_disposed)
                {
                    action(x, y);
                }
            },
            DispatcherPriority.Input);
    }

    private void QueueToolWheel(int wheelDelta)
    {
        lock (_wheelSync)
        {
            _toolWheelRemainder += wheelDelta;
            var detents = _toolWheelRemainder / WheelDeltaPerDetent;
            _toolWheelRemainder %= WheelDeltaPerDetent;
            if (detents == 0)
            {
                return;
            }

            // Wheel up selects the previous tool; wheel down selects the next one.
            _pendingToolSteps -= detents;
            _toolCommitTimer.Change(WheelCommitDelayMilliseconds, Timeout.Infinite);
        }
    }

    private void QueueSizeWheel(int wheelDelta)
    {
        lock (_wheelSync)
        {
            _sizeWheelRemainder += wheelDelta;
            var detents = _sizeWheelRemainder / WheelDeltaPerDetent;
            _sizeWheelRemainder %= WheelDeltaPerDetent;
            if (detents == 0)
            {
                return;
            }

            _pendingSizeSteps += detents;
            _sizeCommitTimer.Change(WheelCommitDelayMilliseconds, Timeout.Infinite);
        }
    }

    private void OnToolCommitTimer(object? state)
    {
        int steps;
        lock (_wheelSync)
        {
            steps = _pendingToolSteps;
            _pendingToolSteps = 0;
        }

        DispatchWheelAction(_cycleTool, steps);
    }

    private void OnSizeCommitTimer(object? state)
    {
        int steps;
        lock (_wheelSync)
        {
            steps = _pendingSizeSteps;
            _pendingSizeSteps = 0;
        }

        DispatchWheelAction(_adjustSize, steps);
    }

    private void DispatchWheelAction(Action<int> action, int steps)
    {
        if (steps == 0 || _disposed || _dispatcher.HasShutdownStarted)
        {
            return;
        }

        _dispatcher.BeginInvoke(
            () =>
            {
                if (!_disposed)
                {
                    action(steps);
                }
            },
            DispatcherPriority.Input);
    }

    private static bool IsExactModifier(int expectedVirtualKey)
    {
        var control = IsKeyDown(NativeMethods.VkControl);
        var alt = IsKeyDown(NativeMethods.VkMenu);
        var shift = IsKeyDown(NativeMethods.VkShift);
        var windows = IsKeyDown(NativeMethods.VkLeftWindows) || IsKeyDown(NativeMethods.VkRightWindows);

        if (windows)
        {
            return false;
        }

        return expectedVirtualKey switch
        {
            NativeMethods.VkControl => control && !alt && !shift,
            NativeMethods.VkMenu => alt && !control && !shift,
            NativeMethods.VkShift => shift && !control && !alt,
            _ => false,
        };
    }

    private void BeginPlainWheelPassThrough()
    {
        if (!_plainWheelPassThroughActive)
        {
            _plainWheelPassThroughActive = true;
            _setWheelPassThrough(true);
        }

        _plainWheelPassThroughTimer.Stop();
        _plainWheelPassThroughTimer.Start();
    }

    private void QueuePlainWheelPassThrough()
    {
        if (_disposed || _dispatcher.HasShutdownStarted)
        {
            return;
        }

        _dispatcher.BeginInvoke(
            () =>
            {
                if (!_disposed && _mode == InteractionMode.Draw)
                {
                    BeginPlainWheelPassThrough();
                }
            },
            DispatcherPriority.Input);
    }

    private void QueueEndPlainWheelPassThrough()
    {
        if (_disposed || _dispatcher.HasShutdownStarted)
        {
            return;
        }

        _dispatcher.BeginInvoke(
            () =>
            {
                if (!_disposed)
                {
                    EndPlainWheelPassThrough();
                }
            },
            DispatcherPriority.Input);
    }

    private void OnPlainWheelPassThroughTimer(object? sender, EventArgs e)
    {
        EndPlainWheelPassThrough();
    }

    private void EndPlainWheelPassThrough()
    {
        _plainWheelPassThroughTimer.Stop();
        if (!_plainWheelPassThroughActive)
        {
            return;
        }

        _plainWheelPassThroughActive = false;
        _setWheelPassThrough(false);
    }

    private static bool HasAnyModifier()
    {
        return IsKeyDown(NativeMethods.VkControl) ||
            IsKeyDown(NativeMethods.VkMenu) ||
            IsKeyDown(NativeMethods.VkShift) ||
            IsKeyDown(NativeMethods.VkLeftWindows) ||
            IsKeyDown(NativeMethods.VkRightWindows);
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }
}
