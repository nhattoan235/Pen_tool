using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenInk.Core.Diagnostics;
using Forms = System.Windows.Forms;

namespace ScreenInk.App.Services;

internal sealed class ScreenCaptureService
{
    private readonly IAppLogger _logger;

    public ScreenCaptureService(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void CopyVirtualDesktopToClipboard()
    {
        var image = CaptureVirtualDesktop();
        System.Windows.Clipboard.SetImage(image);
        _logger.Info($"Copied full desktop capture to clipboard ({image.PixelWidth}x{image.PixelHeight}).");
    }

    public void SaveVirtualDesktopPng()
    {
        var image = CaptureVirtualDesktop();
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save Screen Ink capture",
            Filter = "PNG image (*.png)|*.png",
            DefaultExt = ".png",
            AddExtension = true,
            FileName = $"ScreenInk-{DateTime.Now:yyyyMMdd-HHmmss}.png",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new System.IO.FileStream(
            dialog.FileName,
            System.IO.FileMode.Create,
            System.IO.FileAccess.Write,
            System.IO.FileShare.None);
        encoder.Save(stream);
        _logger.Info($"Saved full desktop capture to {dialog.FileName}.");
    }

    private static BitmapSource CaptureVirtualDesktop()
    {
        var bounds = Forms.SystemInformation.VirtualScreen;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("Windows reported an invalid virtual desktop size.");
        }

        using var bitmap = new Bitmap(
            bounds.Width,
            bounds.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                bounds.Left,
                bounds.Top,
                0,
                0,
                bounds.Size,
                CopyPixelOperation.SourceCopy);
        }

        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;
        var decoder = new PngBitmapDecoder(
            memory,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        var result = decoder.Frames[0];
        result.Freeze();
        return result;
    }
}
