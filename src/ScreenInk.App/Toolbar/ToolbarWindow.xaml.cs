using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ScreenInk.App.Interop;
using ScreenInk.Core.Diagnostics;
using ScreenInk.Core.Ink;
using ScreenInk.Core.Interaction;
using Forms = System.Windows.Forms;

namespace ScreenInk.App.Toolbar;

public partial class ToolbarWindow : Window
{
    private const double ExpandedWidth = 224;
    private const double CollapsedWidth = 38;
    private const int EdgeMarginPixels = 12;

    private readonly Action _pointerAction;
    private readonly Action _drawAction;
    private readonly Action _undoAction;
    private readonly Action _redoAction;
    private readonly Action _clearAction;
    private readonly Action _copyFullScreenAction;
    private readonly Action _saveFullScreenAction;
    private readonly Action _exitAction;
    private readonly InkStyleController _inkStyleController;
    private readonly ToolbarPlacementStore _placementStore;
    private readonly ToolbarPreferencesStore _preferencesStore;
    private readonly DispatcherTimer _preferencesSaveTimer;
    private readonly IAppLogger _logger;
    private ToolbarPlacement? _placement;
    private nint _windowHandle;
    private volatile bool _isCollapsed;
    private bool _isPopupOpen;
    private bool _preferencesSavePending;
    private InteractionMode _currentMode = InteractionMode.Pointer;
    private ToolbarTheme _theme;

    internal ToolbarWindow(
        Action pointerAction,
        Action drawAction,
        Action undoAction,
        Action redoAction,
        Action clearAction,
        Action copyFullScreenAction,
        Action saveFullScreenAction,
        Action exitAction,
        InkStyleController inkStyleController,
        ToolbarPlacementStore placementStore,
        ToolbarPreferencesStore preferencesStore,
        ToolbarTheme initialTheme,
        IAppLogger logger)
    {
        _pointerAction = pointerAction ?? throw new ArgumentNullException(nameof(pointerAction));
        _drawAction = drawAction ?? throw new ArgumentNullException(nameof(drawAction));
        _undoAction = undoAction ?? throw new ArgumentNullException(nameof(undoAction));
        _redoAction = redoAction ?? throw new ArgumentNullException(nameof(redoAction));
        _clearAction = clearAction ?? throw new ArgumentNullException(nameof(clearAction));
        _copyFullScreenAction = copyFullScreenAction ?? throw new ArgumentNullException(nameof(copyFullScreenAction));
        _saveFullScreenAction = saveFullScreenAction ?? throw new ArgumentNullException(nameof(saveFullScreenAction));
        _exitAction = exitAction ?? throw new ArgumentNullException(nameof(exitAction));
        _inkStyleController = inkStyleController ?? throw new ArgumentNullException(nameof(inkStyleController));
        _placementStore = placementStore ?? throw new ArgumentNullException(nameof(placementStore));
        _preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        _theme = initialTheme;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _preferencesSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350),
        };
        _preferencesSaveTimer.Tick += OnPreferencesSaveTimerTick;

        InitializeComponent();
        _inkStyleController.StyleChanged += OnInkStyleChanged;
        ApplyTheme();
        UpdateColorSwatch();
        UpdateToolUi();
        SetMode(InteractionMode.Pointer);
    }

    public void Collapse()
    {
        if (_isCollapsed)
        {
            return;
        }

        _isCollapsed = true;
        ExpandedSurface.Visibility = Visibility.Collapsed;
        CollapsedButton.Visibility = Visibility.Visible;
        Width = CollapsedWidth;
        ApplyPlacement();
    }

    public void Expand()
    {
        if (!_isCollapsed)
        {
            return;
        }

        _isCollapsed = false;
        CollapsedButton.Visibility = Visibility.Collapsed;
        ExpandedSurface.Visibility = Visibility.Visible;
        Width = ExpandedWidth;
        ApplyPlacement();
    }

    public void SetMode(InteractionMode mode)
    {
        _currentMode = mode;
        var inkColor = _inkStyleController.CurrentColor;
        var activeBrush = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(52, inkColor.Red, inkColor.Green, inkColor.Blue));
        activeBrush.Freeze();

        PointerButton.Background = mode == InteractionMode.Pointer
            ? activeBrush
            : System.Windows.Media.Brushes.Transparent;
        PenButton.Background = mode == InteractionMode.Draw
            ? activeBrush
            : System.Windows.Media.Brushes.Transparent;
    }

    public void CollapseIfOutside(int screenX, int screenY)
    {
        if (_isCollapsed || _isPopupOpen || _windowHandle == nint.Zero ||
            !NativeMethods.GetWindowRect(_windowHandle, out var bounds))
        {
            return;
        }

        var isInside = screenX >= bounds.Left && screenX < bounds.Right &&
            screenY >= bounds.Top && screenY < bounds.Bottom;
        if (!isInside)
        {
            Collapse();
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        _placement ??= _placementStore.Load() ?? CreateDefaultPlacement();
        ApplyPlacement();
    }

    protected override void OnClosed(EventArgs e)
    {
        _preferencesSaveTimer.Stop();
        _preferencesSaveTimer.Tick -= OnPreferencesSaveTimerTick;
        FlushPendingPreferences();
        _inkStyleController.StyleChanged -= OnInkStyleChanged;
        base.OnClosed(e);
    }

    private void OnPointerClick(object sender, RoutedEventArgs e)
    {
        _pointerAction();
        Collapse();
    }

    private void OnPenClick(object sender, RoutedEventArgs e)
    {
        ShowToolMenu();
    }

    private void OnColorClick(object sender, RoutedEventArgs e)
    {
        var menu = new System.Windows.Controls.ContextMenu
        {
            PlacementTarget = ColorButton,
        };

        for (var colorIndex = 0; colorIndex < _inkStyleController.ColorCount; colorIndex++)
        {
            var selectedIndex = colorIndex;
            var color = _inkStyleController.GetColor(colorIndex);
            var item = new System.Windows.Controls.MenuItem
            {
                Header = $"{colorIndex + 1}   {GetColorName(colorIndex)}",
                Icon = new System.Windows.Shapes.Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = CreateBrush(0xFF, color.Red, color.Green, color.Blue),
                    Stroke = CreateBrush(0x40, 0x80, 0x80, 0x80),
                    StrokeThickness = 1,
                },
                IsCheckable = true,
                IsChecked = colorIndex == _inkStyleController.CurrentColorIndex,
            };
            item.Click += (_, _) => _inkStyleController.SelectColor(selectedIndex);
            menu.Items.Add(item);
        }

        OpenMenu(menu);
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        _undoAction();
    }

    private void OnRedoClick(object sender, RoutedEventArgs e)
    {
        _redoAction();
    }

    private void OnMoreClick(object sender, RoutedEventArgs e)
    {
        var menu = new System.Windows.Controls.ContextMenu
        {
            PlacementTarget = MoreButton,
        };

        var clearItem = new System.Windows.Controls.MenuItem { Header = "Clear drawings" };
        clearItem.Click += (_, _) => _clearAction();
        menu.Items.Add(clearItem);

        var redoItem = new System.Windows.Controls.MenuItem { Header = "Redo · Ctrl+Y" };
        redoItem.Click += (_, _) => _redoAction();
        menu.Items.Add(redoItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var copyCaptureItem = new System.Windows.Controls.MenuItem
        {
            Header = "Copy full screen",
        };
        copyCaptureItem.Click += async (_, _) => await RunCaptureAsync(_copyFullScreenAction);
        menu.Items.Add(copyCaptureItem);

        var saveCaptureItem = new System.Windows.Controls.MenuItem
        {
            Header = "Save full screen…",
        };
        saveCaptureItem.Click += async (_, _) => await RunCaptureAsync(_saveFullScreenAction);
        menu.Items.Add(saveCaptureItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var themeItem = new System.Windows.Controls.MenuItem
        {
            Header = _theme == ToolbarTheme.Dark ? "Use light toolbar" : "Use dark toolbar",
        };
        themeItem.Click += (_, _) => ToggleTheme();
        menu.Items.Add(themeItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit Screen Ink" };
        exitItem.Click += (_, _) => _exitAction();
        menu.Items.Add(exitItem);

        OpenMenu(menu);
    }

    private async Task RunCaptureAsync(Action captureAction)
    {
        Hide();
        try
        {
            // ContextMenu is a separate HWND. Give WPF and DWM one composition
            // turn to remove both it and the toolbar before copying the screen.
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(80);
            captureAction();
        }
        catch (Exception exception)
        {
            _logger.Error("Unable to capture the screen.", exception);
        }
        finally
        {
            Show();
            ApplyPlacement();
        }
    }

    private void OnCollapsedMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || _windowHandle == nint.Zero)
        {
            return;
        }

        var start = Forms.Cursor.Position;
        var dragSize = Forms.SystemInformation.DragSize;
        _ = NativeMethods.ReleaseCapture();
        _ = NativeMethods.SendMessage(
            _windowHandle,
            NativeMethods.WmNcLeftButtonDown,
            new nint(NativeMethods.HtCaption),
            nint.Zero);

        var end = Forms.Cursor.Position;
        var moved = Math.Abs(end.X - start.X) >= Math.Max(2, dragSize.Width / 2) ||
            Math.Abs(end.Y - start.Y) >= Math.Max(2, dragSize.Height / 2);
        if (moved)
        {
            SnapToNearestEdge();
        }
        else
        {
            Expand();
        }

        e.Handled = true;
    }

    private void OnDragGripMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException exception)
        {
            _logger.Error("Unable to drag toolbar.", exception);
            return;
        }

        SnapToNearestEdge();
    }

    private void SnapToNearestEdge()
    {
        var cursorPosition = Forms.Cursor.Position;
        var screen = Forms.Screen.FromPoint(cursorPosition);
        var bounds = screen.WorkingArea;
        var dockRight = cursorPosition.X >= bounds.Left + (bounds.Width / 2);
        var verticalRatio = bounds.Height <= 1
            ? 0.5
            : Math.Clamp((double)(cursorPosition.Y - bounds.Top) / bounds.Height, 0, 1);

        _placement = new ToolbarPlacement(screen.DeviceName, dockRight, verticalRatio);
        _placementStore.Save(_placement);
        ApplyPlacement();
    }

    private void ApplyPlacement()
    {
        if (_windowHandle == nint.Zero || _placement is null)
        {
            return;
        }

        var screen = Forms.Screen.AllScreens.FirstOrDefault(
                candidate => string.Equals(candidate.DeviceName, _placement.DisplayId, StringComparison.Ordinal))
            ?? Forms.Screen.PrimaryScreen
            ?? Forms.Screen.AllScreens[0];

        var bounds = screen.WorkingArea;
        var dpi = VisualTreeHelper.GetDpi(this);
        var widthPixels = Math.Max(1, (int)Math.Ceiling(Width * dpi.DpiScaleX));
        var heightPixels = Math.Max(1, (int)Math.Ceiling(Height * dpi.DpiScaleY));
        var x = _placement.DockRight
            ? bounds.Right - widthPixels - EdgeMarginPixels
            : bounds.Left + EdgeMarginPixels;
        var availableHeight = Math.Max(0, bounds.Height - heightPixels - (EdgeMarginPixels * 2));
        var y = bounds.Top + EdgeMarginPixels + (int)Math.Round(availableHeight * _placement.VerticalRatio);

        OverlayWindowStyles.Place(_windowHandle, x, y, widthPixels, heightPixels);
    }

    private static ToolbarPlacement CreateDefaultPlacement()
    {
        var screen = Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens[0];
        return new ToolbarPlacement(screen.DeviceName, DockRight: true, VerticalRatio: 0.5);
    }

    private void OnInkStyleChanged(object? sender, EventArgs e)
    {
        UpdateColorSwatch();
        UpdateToolUi();
        SetMode(_currentMode);
        SchedulePreferencesSave();
    }

    private void UpdateColorSwatch()
    {
        var color = _inkStyleController.CurrentColor;
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.Red, color.Green, color.Blue));
        brush.Freeze();
        ColorSwatch.Fill = brush;
        CollapsedButton.Foreground = brush;
        CollapsedButton.ToolTip = $"Mở Screen Ink · màu {color.Hex} · phím {_inkStyleController.CurrentColorIndex + 1}";
    }

    private void UpdateToolUi()
    {
        ToolGlyph.Text = GetToolGlyph(_inkStyleController.CurrentTool);
        PenButton.ToolTip =
            $"Mở Tools · {GetToolName(_inkStyleController.CurrentTool)}" +
            (_inkStyleController.CurrentTool == InkTool.Lasso
                ? " · khoanh để chọn, kéo để di chuyển"
                : $" · {_inkStyleController.CurrentSize:0} px") +
            (_inkStyleController.CurrentTool is InkTool.Pen or InkTool.Highlighter
                ? _inkStyleController.IsPersistent ? " · Persistent" : " · Temporary"
                : string.Empty) +
            (_inkStyleController.CurrentTool is InkTool.PixelEraser or InkTool.ObjectEraser
                ? $" · cursor { _inkStyleController.CurrentColor.Hex}"
                : string.Empty);
    }

    private void ShowToolMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu
        {
            PlacementTarget = PenButton,
        };

        foreach (var tool in Enum.GetValues<InkTool>())
        {
            var selectedTool = tool;
            var item = new System.Windows.Controls.MenuItem
            {
                Header = GetToolName(tool),
                Icon = new System.Windows.Controls.TextBlock
                {
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI Symbol"),
                    FontSize = 16,
                    Text = GetToolGlyph(tool),
                },
                IsCheckable = true,
                IsChecked = tool == _inkStyleController.CurrentTool,
            };
            item.Click += (_, _) =>
            {
                _inkStyleController.SelectTool(selectedTool);
                _drawAction();
                Collapse();
            };
            menu.Items.Add(item);
        }

        if (_inkStyleController.CurrentTool != InkTool.Lasso)
        {
            menu.Items.Add(new System.Windows.Controls.Separator());

            var sizeLabels = new[] { "Small", "Medium", "Large" };
            for (var sizeIndex = 0; sizeIndex < sizeLabels.Length; sizeIndex++)
            {
                var selectedSize = sizeIndex;
                var item = new System.Windows.Controls.MenuItem
                {
                    Header = sizeLabels[sizeIndex],
                    IsCheckable = true,
                    IsChecked = sizeIndex == _inkStyleController.CurrentSizeIndex,
                };
                item.Click += (_, _) => _inkStyleController.SelectSize(selectedSize);
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new System.Windows.Controls.Separator());

        var persistentItem = new System.Windows.Controls.MenuItem
        {
            Header = "Persistent ink",
            IsCheckable = true,
            IsChecked = _inkStyleController.IsPersistent,
            IsEnabled = _inkStyleController.CurrentTool is InkTool.Pen or InkTool.Highlighter,
        };
        persistentItem.Click += (_, _) =>
            _inkStyleController.SetPersistent(!_inkStyleController.IsPersistent);
        menu.Items.Add(persistentItem);

        menu.Items.Add(new System.Windows.Controls.MenuItem
        {
            Header = "Shift+wheel tools · Ctrl+wheel size",
            IsEnabled = false,
        });

        OpenMenu(menu);
    }

    private void ToggleTheme()
    {
        _theme = _theme == ToolbarTheme.Dark ? ToolbarTheme.Light : ToolbarTheme.Dark;
        ApplyTheme();
        SchedulePreferencesSave();
    }

    private void ApplyTheme()
    {
        var isDark = _theme == ToolbarTheme.Dark;
        var surface = isDark
            ? CreateBrush(0xF2, 0x1A, 0x1D, 0x24)
            : CreateBrush(0xF4, 0xF7, 0xF7, 0xF9);
        var border = isDark
            ? CreateBrush(0x30, 0xFF, 0xFF, 0xFF)
            : CreateBrush(0x24, 0x1A, 0x1D, 0x24);
        var foreground = isDark
            ? CreateBrush(0xFF, 0xE8, 0xEC, 0xF2)
            : CreateBrush(0xFF, 0x24, 0x29, 0x32);

        ExpandedSurface.Background = surface;
        ExpandedSurface.BorderBrush = border;
        ExpandedSurface.Effect = null;
        CollapsedButton.Background = surface;
        CollapsedButton.BorderBrush = border;
        CollapsedInnerDisc.Fill = isDark
            ? CreateBrush(0xFF, 0x10, 0x13, 0x1A)
            : CreateBrush(0xFF, 0x28, 0x2D, 0x36);
        CollapsedInnerDisc.Stroke = isDark
            ? CreateBrush(0x38, 0xFF, 0xFF, 0xFF)
            : CreateBrush(0x30, 0xFF, 0xFF, 0xFF);
        CollapsedNib.Fill = CreateBrush(0xFF, 0xF3, 0xF5, 0xF8);

        foreach (var button in new[] { PointerButton, PenButton, ColorButton, UndoButton, RedoButton, MoreButton })
        {
            button.Foreground = foreground;
        }
    }

    private void SavePreferences()
    {
        _preferencesStore.Save(CreatePreferences());
    }

    private ToolbarPreferences CreatePreferences()
    {
        return new ToolbarPreferences
        {
            ColorIndex = _inkStyleController.CurrentColorIndex,
            Theme = _theme,
            Tool = _inkStyleController.CurrentTool,
            PenSizeIndex = _inkStyleController.GetSizeIndex(InkTool.Pen),
            HighlighterSizeIndex = _inkStyleController.GetSizeIndex(InkTool.Highlighter),
            PixelEraserSizeIndex = _inkStyleController.GetSizeIndex(InkTool.PixelEraser),
            ObjectEraserSizeIndex = _inkStyleController.GetSizeIndex(InkTool.ObjectEraser),
        };
    }

    private void SchedulePreferencesSave()
    {
        _preferencesSavePending = true;
        _preferencesSaveTimer.Stop();
        _preferencesSaveTimer.Start();
    }

    private async void OnPreferencesSaveTimerTick(object? sender, EventArgs e)
    {
        _preferencesSaveTimer.Stop();
        if (!_preferencesSavePending)
        {
            return;
        }

        _preferencesSavePending = false;
        await _preferencesStore.SaveAsync(CreatePreferences());
    }

    private void FlushPendingPreferences()
    {
        if (!_preferencesSavePending)
        {
            return;
        }

        _preferencesSavePending = false;
        SavePreferences();
    }

    private static SolidColorBrush CreateBrush(byte alpha, byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, red, green, blue));
        brush.Freeze();
        return brush;
    }

    private void OpenMenu(System.Windows.Controls.ContextMenu menu)
    {
        _isPopupOpen = true;
        menu.Closed += (_, _) =>
        {
            _isPopupOpen = false;
            if (_currentMode == InteractionMode.Pointer && !IsMouseOver)
            {
                Collapse();
            }
        };
        menu.IsOpen = true;
    }

    private static string GetColorName(int colorIndex) => colorIndex switch
    {
        0 => "Red",
        1 => "Yellow",
        2 => "Blue",
        3 => "Green",
        4 => "White",
        _ => "Color",
    };

    private static string GetToolName(InkTool tool) => tool switch
    {
        InkTool.Pen => "Pen",
        InkTool.Highlighter => "Highlighter",
        InkTool.PixelEraser => "Pixel eraser",
        InkTool.ObjectEraser => "Object eraser",
        InkTool.Lasso => "Lasso selection",
        _ => "Tool",
    };

    private static string GetToolGlyph(InkTool tool) => tool switch
    {
        InkTool.Pen => "✎",
        InkTool.Highlighter => "▰",
        InkTool.PixelEraser => "⌫",
        InkTool.ObjectEraser => "▣",
        InkTool.Lasso => "◌",
        _ => "✎",
    };
}
