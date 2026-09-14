using System.Drawing;
using Forms = System.Windows.Forms;

namespace ScreenInk.App.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly Action _openAction;
    private readonly Action _toggleDrawingAction;
    private readonly Action _clearDrawingsAction;
    private readonly Action _undoLastStrokeAction;
    private readonly Action _redoLastAction;
    private readonly Action _exitAction;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Icon _applicationIcon;
    private readonly Forms.ToolStripMenuItem _toggleDrawingItem;
    private bool _disposed;

    public TrayIconService(
        Action openAction,
        Action toggleDrawingAction,
        Action clearDrawingsAction,
        Action undoLastStrokeAction,
        Action redoLastAction,
        Action exitAction)
    {
        _openAction = openAction ?? throw new ArgumentNullException(nameof(openAction));
        _toggleDrawingAction = toggleDrawingAction ?? throw new ArgumentNullException(nameof(toggleDrawingAction));
        _clearDrawingsAction = clearDrawingsAction ?? throw new ArgumentNullException(nameof(clearDrawingsAction));
        _undoLastStrokeAction = undoLastStrokeAction ?? throw new ArgumentNullException(nameof(undoLastStrokeAction));
        _redoLastAction = redoLastAction ?? throw new ArgumentNullException(nameof(redoLastAction));
        _exitAction = exitAction ?? throw new ArgumentNullException(nameof(exitAction));

        _menu = new Forms.ContextMenuStrip();
        _menu.Items.Add("Open Screen Ink", image: null, (_, _) => _openAction());
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _toggleDrawingItem = new Forms.ToolStripMenuItem("Start drawing", image: null, (_, _) => _toggleDrawingAction());
        _menu.Items.Add(_toggleDrawingItem);
        _menu.Items.Add("Undo last stroke", image: null, (_, _) => _undoLastStrokeAction());
        _menu.Items.Add("Redo", image: null, (_, _) => _redoLastAction());
        _menu.Items.Add("Clear drawings", image: null, (_, _) => _clearDrawingsAction());
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("Exit", image: null, (_, _) => _exitAction());

        using var extractedIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty);
        _applicationIcon = (Icon)(extractedIcon ?? SystemIcons.Application).Clone();
        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = _applicationIcon,
            Text = "Screen Ink",
        };

        _notifyIcon.DoubleClick += (_, _) => _openAction();
    }

    public void Show()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _notifyIcon.Visible = true;
    }

    public void SetDrawingActive(bool isActive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _toggleDrawingItem.Text = isActive ? "Stop drawing" : "Start drawing";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _applicationIcon.Dispose();
        _menu.Dispose();
        _disposed = true;
    }
}
