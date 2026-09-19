using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ScreenInk.App.Diagnostics;
using ScreenInk.App.Services;
using ScreenInk.App.Toolbar;
using ScreenInk.Core.Diagnostics;
using ScreenInk.Core.Ink;
using ScreenInk.Core.Interaction;

namespace ScreenInk.App;

public partial class App : System.Windows.Application
{
    private FileAppLogger? _logger;
    private TrayIconService? _trayIcon;
    private MainWindow? _mainWindow;
    private DisplayTopologyService? _displayTopology;
    private OverlayManager? _overlayManager;
    private InteractionModeController? _modeController;
    private InkStyleController? _inkStyleController;
    private ToolbarWindow? _toolbarWindow;
    private GlobalHotkeyService? _hotkeys;
    private MouseShortcutService? _mouseShortcuts;
    private StrokeQualityRecorder? _strokeQualityRecorder;
    private ScreenCaptureService? _screenCaptureService;
    private AppPreferencesStore? _appPreferencesStore;
    private AppPreferences _appPreferences = AppPreferences.Default;
    private StartupRegistrationService? _startupRegistration;
    private SessionMarkerService? _sessionMarker;
    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;
    private bool _toggleHotkeyAvailable;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: @"Local\ScreenInk.SingleInstance.4DB74511",
            createdNew: out _ownsSingleInstanceMutex);
        if (!_ownsSingleInstanceMutex)
        {
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var dataDirectory = ResolveDataDirectory();

        _logger = new FileAppLogger(Path.Combine(dataDirectory, "logs", "screenink.log"));
        _logger.Info("Screen Ink starting.");
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _sessionMarker = new SessionMarkerService(dataDirectory, _logger);
        if (_sessionMarker.BeginSession())
        {
            _logger.Info("Recovered after an unclean previous session; volatile overlay state was discarded.");
        }

        _appPreferencesStore = new AppPreferencesStore(
            Path.Combine(dataDirectory, "settings", "app-preferences.json"),
            _logger);
        _appPreferences = _appPreferencesStore.Load();
        _startupRegistration = new StartupRegistrationService(_logger);
        if (_appPreferences.StartWithWindows && !_startupRegistration.SetEnabled(true))
        {
            _appPreferences = _appPreferences with { StartWithWindows = false };
            _ = _appPreferencesStore.SaveAsync(_appPreferences);
        }

        _modeController = new InteractionModeController();
        _modeController.ModeChanged += OnInteractionModeChanged;
        _inkStyleController = new InkStyleController();

        var preferencesStore = new ToolbarPreferencesStore(
            Path.Combine(dataDirectory, "settings", "toolbar-preferences.json"),
            _logger);
        var preferences = preferencesStore.Load();
        if (preferences.ColorIndex < _inkStyleController.ColorCount)
        {
            _inkStyleController.SelectColor(preferences.ColorIndex);
        }
        _inkStyleController.SelectSize(InkTool.Pen, preferences.PenSizeIndex);
        _inkStyleController.SelectSize(InkTool.Highlighter, preferences.HighlighterSizeIndex);
        _inkStyleController.SelectSize(InkTool.PixelEraser, preferences.PixelEraserSizeIndex);
        _inkStyleController.SelectSize(InkTool.ObjectEraser, preferences.ObjectEraserSizeIndex);
        _inkStyleController.SelectTool(preferences.Tool);

        _strokeQualityRecorder = new StrokeQualityRecorder(
            dataDirectory,
            string.Equals(
                Environment.GetEnvironmentVariable("SCREENINK_STROKE_DIAGNOSTICS"),
                "1",
                StringComparison.Ordinal),
            _logger);
        _screenCaptureService = new ScreenCaptureService(_logger);

        _displayTopology = new DisplayTopologyService();
        _overlayManager = new OverlayManager(
            _displayTopology,
            _modeController,
            _inkStyleController,
            _strokeQualityRecorder,
            _logger);
        _overlayManager.StrokeStarted += OnStrokeStarted;
        _overlayManager.Start();

        _hotkeys = new GlobalHotkeyService(
            _modeController.ToggleDrawing,
            _modeController.ReturnToPointer,
            _overlayManager.DeleteSelectionOrClearAll,
            _overlayManager.UndoLastStroke,
            _overlayManager.RedoLastAction,
            _inkStyleController.SelectColor,
            _logger);
        _toggleHotkeyAvailable = _hotkeys.RegisterToggleDrawing(_appPreferences.ToggleHotkey);

        _trayIcon = new TrayIconService(
            ShowMainWindow,
            _modeController.ToggleDrawing,
            _overlayManager.ClearAll,
            _overlayManager.UndoLastStroke,
            _overlayManager.RedoLastAction,
            ExitApplication);
        _trayIcon.Show();

        var placementStore = new ToolbarPlacementStore(
            Path.Combine(dataDirectory, "settings", "toolbar-placement.json"),
            _logger);
        _toolbarWindow = new ToolbarWindow(
            _modeController.ReturnToPointer,
            () => _modeController.SetMode(InteractionMode.Draw),
            _overlayManager.UndoLastStroke,
            _overlayManager.RedoLastAction,
            _overlayManager.ClearAll,
            _screenCaptureService.CopyVirtualDesktopToClipboard,
            _screenCaptureService.SaveVirtualDesktopPng,
            ExitApplication,
            _inkStyleController,
            placementStore,
            preferencesStore,
            preferences.Theme,
            _logger);
        _toolbarWindow.Show();

        _mouseShortcuts = new MouseShortcutService(
            _overlayManager.CycleTool,
            _overlayManager.AdjustCurrentToolSize,
            (screenX, screenY) => _toolbarWindow?.CollapseIfOutside(screenX, screenY),
            _overlayManager.SetWheelPassThrough,
            _logger);
        _mouseShortcuts.SetMode(_modeController.Current);

        if (e.Args.Contains("--show", StringComparer.Ordinal))
        {
            ShowMainWindow();
        }

        if (e.Args.Contains("--smoke-test", StringComparer.Ordinal))
        {
            Dispatcher.BeginInvoke(ExitApplication);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info("Screen Ink stopped.");
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        if (_modeController is not null)
        {
            _modeController.ModeChanged -= OnInteractionModeChanged;
        }

        if (_overlayManager is not null)
        {
            _overlayManager.StrokeStarted -= OnStrokeStarted;
        }

        _mouseShortcuts?.Dispose();
        _hotkeys?.Dispose();
        _toolbarWindow?.Close();
        _overlayManager?.Dispose();
        _strokeQualityRecorder?.Dispose();
        _displayTopology?.Dispose();
        _trayIcon?.Dispose();
        _sessionMarker?.CompleteSession();
        _logger?.Dispose();
        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void ShowMainWindow()
    {
        _mainWindow ??= new MainWindow(
            _appPreferences,
            TryChangeToggleHotkey,
            TrySetStartWithWindows);
        if (_modeController is not null)
        {
            _mainWindow.SetStatus(
                _modeController.Current,
                _toggleHotkeyAvailable,
                _appPreferences.ToggleHotkey.GetDefinition().DisplayName);
        }

        _mainWindow.Show();

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    private void ExitApplication()
    {
        _mainWindow?.AllowClose();
        Shutdown();
    }

    private void OnInteractionModeChanged(object? sender, InteractionModeChangedEventArgs e)
    {
        _logger?.Info($"Interaction mode changed: {e.Previous} -> {e.Current}.");
        _trayIcon?.SetDrawingActive(e.Current == InteractionMode.Draw);
        _hotkeys?.SetDrawingShortcutsEnabled(e.Current == InteractionMode.Draw);
        _mouseShortcuts?.SetMode(e.Current);
        _toolbarWindow?.SetMode(e.Current);
        _mainWindow?.SetStatus(
            e.Current,
            _toggleHotkeyAvailable,
            _appPreferences.ToggleHotkey.GetDefinition().DisplayName);
    }

    private void OnStrokeStarted(object? sender, EventArgs e)
    {
        _toolbarWindow?.Collapse();
    }

    private bool TryChangeToggleHotkey(ToggleHotkeyPreset preset)
    {
        if (_hotkeys is null || !_hotkeys.TryChangeToggleDrawing(preset))
        {
            return false;
        }

        _toggleHotkeyAvailable = true;
        _appPreferences = _appPreferences with { ToggleHotkey = preset };
        if (_appPreferencesStore is not null)
        {
            _ = _appPreferencesStore.SaveAsync(_appPreferences);
        }

        return true;
    }

    private bool TrySetStartWithWindows(bool enabled)
    {
        if (_startupRegistration is null || !_startupRegistration.SetEnabled(enabled))
        {
            return false;
        }

        _appPreferences = _appPreferences with { StartWithWindows = enabled };
        if (_appPreferencesStore is not null)
        {
            _ = _appPreferencesStore.SaveAsync(_appPreferences);
        }

        return true;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error("Unhandled UI exception; the session will be marked unclean.", e.Exception);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            _logger?.Error("Unhandled process exception; the session will be marked unclean.", exception);
        }
    }

    private static string ResolveDataDirectory()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("SCREENINK_DATA_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return Path.GetFullPath(overrideDirectory);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenInk");
    }
}

