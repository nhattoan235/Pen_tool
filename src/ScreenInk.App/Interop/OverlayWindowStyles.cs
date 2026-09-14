using System.ComponentModel;

namespace ScreenInk.App.Interop;

internal static class OverlayWindowStyles
{
    public static void SetInputMode(nint windowHandle, bool acceptsInput)
    {
        var styles = NativeMethods.GetWindowLongPtr(windowHandle, NativeMethods.GwlExStyle).ToInt64();
        styles |= NativeMethods.WsExLayered | NativeMethods.WsExToolWindow;

        if (acceptsInput)
        {
            styles &= ~NativeMethods.WsExTransparent;
            styles &= ~NativeMethods.WsExNoActivate;
        }
        else
        {
            styles |= NativeMethods.WsExTransparent | NativeMethods.WsExNoActivate;
        }

        System.Runtime.InteropServices.Marshal.SetLastPInvokeError(0);
        var previousStyles = NativeMethods.SetWindowLongPtr(
            windowHandle,
            NativeMethods.GwlExStyle,
            new nint(styles));

        if (previousStyles == nint.Zero)
        {
            ThrowIfLastError("Unable to update overlay window styles.");
        }

        if (!NativeMethods.SetWindowPos(
                windowHandle,
                NativeMethods.HwndTopmost,
                0,
                0,
                0,
                0,
                NativeMethods.SwpNoMove |
                NativeMethods.SwpNoSize |
                NativeMethods.SwpNoActivate |
                NativeMethods.SwpFrameChanged))
        {
            throw new Win32Exception("Unable to apply overlay window styles.");
        }
    }

    public static void Place(nint windowHandle, int x, int y, int width, int height)
    {
        if (!NativeMethods.SetWindowPos(
                windowHandle,
                NativeMethods.HwndTopmost,
                x,
                y,
                width,
                height,
                NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow))
        {
            throw new Win32Exception("Unable to position overlay window.");
        }
    }

    private static void ThrowIfLastError(string message)
    {
        var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
        if (error != 0)
        {
            throw new Win32Exception(error, message);
        }
    }
}
