using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ScreenInk.App.Diagnostics;
using ScreenInk.App.Interop;
using ScreenInk.App.Rendering;
using ScreenInk.Core.Diagnostics;
using ScreenInk.Core.Displays;
using ScreenInk.Core.Ink;
using ScreenInk.Core.Interaction;

namespace ScreenInk.App.Overlay;

public partial class OverlayWindow : Window
{
    private const double NaturalInkCoreSizeRatio = 0.64;
    private const double NaturalInkCoreOpacity = 0.42;
    private readonly DisplayDescriptor _display;
    private readonly IAppLogger _logger;
    private readonly InkStyleController _inkStyleController;
    private readonly StrokeQualityRecorder _strokeQualityRecorder;
    private readonly Stopwatch _inputClock = Stopwatch.StartNew();
    private readonly Dictionary<Guid, StrokeVisual> _strokeElements = [];
    private readonly DispatcherTimer _toolFeedbackTimer;
    private readonly ShapeRecognizer _shapeRecognizer = new();
    private InteractionMode _mode;
    private StrokePointBuffer? _activeStroke;
    private FreehandStrokeOutlineBuilder? _activeOutlineBuilder;
    private FreehandStrokeOutlineBuilder? _activeInkCoreOutlineBuilder;
    private Path? _activePath;
    private Path? _activeInkCorePath;
    private Guid _activeStrokeId;
    private bool _activeStrokeIsPersistent;
    private InkTool _activeInkTool;
    private double _activeStrokeSize;
    private int _activeGeometryBuildCount;
    private double _activeGeometryBuildTotalMilliseconds;
    private double _activeGeometryBuildMaximumMilliseconds;
    private long _lastAcceptedPointMilliseconds;
    private System.Windows.Point _lastAcceptedInputPoint;
    private readonly Dictionary<Guid, StrokeGeometryPair> _eraserOriginalGeometries = [];
    private readonly HashSet<Guid> _selectedStrokeIds = [];
    private readonly List<System.Windows.Point> _lassoPoints = [];
    private readonly Dictionary<Guid, StrokeGeometryPair> _selectionMoveOriginalGeometries = [];
    private Path? _lassoPath;
    private bool _isMovingSelection;
    private System.Windows.Point _selectionDragStart;
    private System.Windows.Rect _selectionBounds = System.Windows.Rect.Empty;
    private bool _isErasing;
    private bool _cursorStyleDirty;
    private nint _windowHandle;

    internal OverlayWindow(
        DisplayDescriptor display,
        InteractionMode initialMode,
        InkStyleController inkStyleController,
        StrokeQualityRecorder strokeQualityRecorder,
        IAppLogger logger)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _inkStyleController = inkStyleController ?? throw new ArgumentNullException(nameof(inkStyleController));
        _strokeQualityRecorder = strokeQualityRecorder ?? throw new ArgumentNullException(nameof(strokeQualityRecorder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mode = initialMode;
        InitializeComponent();
        _toolFeedbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(650),
        };
        _toolFeedbackTimer.Tick += OnToolFeedbackTimerTick;
        _inkStyleController.StyleChanged += OnInkStyleChanged;
        UpdateCursorPreviewStyle();
    }

    public event EventHandler? PointerModeRequested;

    internal event EventHandler<StrokeCompletedEventArgs>? StrokeCompleted;

    internal event EventHandler? StrokeStarted;

    internal event EventHandler<StrokeEraseRequestedEventArgs>? StrokeEraseRequested;

    internal event EventHandler<StrokeGeometryChangedEventArgs>? StrokeGeometryChanged;

    internal event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    public string DisplayId => _display.Id;

    internal IReadOnlyCollection<Guid> SelectedStrokeIds => _selectedStrokeIds;

    internal bool ContainsScreenPoint(int x, int y)
    {
        var bounds = _display.Bounds;
        return x >= bounds.X && x < bounds.Right && y >= bounds.Y && y < bounds.Bottom;
    }

    internal void ShowCurrentToolFeedback()
    {
        ShowToolFeedback();
    }

    internal void ShowCurrentSizeFeedback()
    {
        ShowToolFeedback($"{_inkStyleController.CurrentSize:0} px");
    }

    public void ClearStrokes()
    {
        CancelLasso();
        CancelSelectionMove(restoreOriginals: false);
        _selectedStrokeIds.Clear();
        UpdateSelectionAdorner();
        CancelActiveStroke();
        _strokeElements.Clear();
        InkSurface.Children.Clear();
    }

    public bool RemoveStroke(Guid strokeId)
    {
        return DetachStroke(strokeId) is not null;
    }

    internal DetachedStroke? GetStrokeSnapshot(Guid strokeId)
    {
        if (!_strokeElements.TryGetValue(strokeId, out var stroke))
        {
            return null;
        }

        return new DetachedStroke(
            strokeId,
            stroke.Element,
            stroke.InkCoreElement,
            stroke.BaseOpacity,
            stroke.InkCoreBaseOpacity,
            InkSurface.Children.IndexOf(stroke.Element));
    }

    internal IReadOnlyList<DetachedStroke> GetStrokeSnapshots()
    {
        return _strokeElements.Keys
            .Select(GetStrokeSnapshot)
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => snapshot!)
            .OrderBy(snapshot => snapshot.CanvasIndex)
            .ToArray();
    }

    internal DetachedStroke? DetachStroke(Guid strokeId)
    {
        var snapshot = GetStrokeSnapshot(strokeId);
        if (snapshot is null)
        {
            return null;
        }

        _strokeElements.Remove(strokeId);
        InkSurface.Children.Remove(snapshot.Element);
        if (snapshot.InkCoreElement is not null)
        {
            InkSurface.Children.Remove(snapshot.InkCoreElement);
        }
        if (_selectedStrokeIds.Remove(strokeId))
        {
            UpdateSelectionAdorner();
        }

        return snapshot;
    }

    internal bool RestoreStroke(DetachedStroke stroke)
    {
        if (_strokeElements.ContainsKey(stroke.StrokeId))
        {
            return false;
        }

        _strokeElements.Add(stroke.StrokeId, new StrokeVisual(
            stroke.Element,
            stroke.InkCoreElement,
            stroke.BaseOpacity,
            stroke.InkCoreBaseOpacity));
        var index = Math.Clamp(stroke.CanvasIndex, 0, InkSurface.Children.Count);
        InkSurface.Children.Insert(index, stroke.Element);
        if (stroke.InkCoreElement is not null)
        {
            InkSurface.Children.Insert(Math.Min(index + 1, InkSurface.Children.Count), stroke.InkCoreElement);
        }
        return true;
    }

    internal bool SetStrokeGeometry(Guid strokeId, Geometry geometry, Geometry? inkCoreGeometry)
    {
        if (!_strokeElements.TryGetValue(strokeId, out var stroke))
        {
            return false;
        }

        stroke.Element.Data = geometry;
        if (stroke.InkCoreElement is not null)
        {
            stroke.InkCoreElement.Data = inkCoreGeometry ?? Geometry.Empty;
        }
        if (_selectedStrokeIds.Contains(strokeId))
        {
            UpdateSelectionAdorner();
        }

        return true;
    }

    public bool SetStrokeOpacity(Guid strokeId, double opacity)
    {
        if (!_strokeElements.TryGetValue(strokeId, out var stroke))
        {
            return false;
        }

        stroke.Element.Opacity = stroke.BaseOpacity * Math.Clamp(opacity, 0, 1);
        if (stroke.InkCoreElement is not null)
        {
            stroke.InkCoreElement.Opacity = stroke.InkCoreBaseOpacity * Math.Clamp(opacity, 0, 1);
        }
        return true;
    }

    public void SetMode(InteractionMode mode)
    {
        if (mode == InteractionMode.Pointer)
        {
            CancelLasso();
            CancelSelectionMove(restoreOriginals: true);
            ClearSelection();
        }

        _mode = mode;
        Cursor = mode == InteractionMode.Draw
            ? System.Windows.Input.Cursors.None
            : System.Windows.Input.Cursors.Arrow;
        InputSurface.IsHitTestVisible = mode == InteractionMode.Draw;
        CursorPreview.Visibility = Visibility.Collapsed;

        if (_windowHandle != nint.Zero)
        {
            OverlayWindowStyles.SetInputMode(_windowHandle, mode == InteractionMode.Draw);
            _logger.Info($"Overlay {_display.Id} applied {mode} mode.");
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        SetMode(_mode);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        var bounds = _display.Bounds;
        OverlayWindowStyles.Place(
            _windowHandle,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height);

        _logger.Info(
            $"Overlay {_display.Id} placed at physical " +
            $"({bounds.X},{bounds.Y}) {bounds.Width}x{bounds.Height}; " +
            $"WPF actual {ActualWidth:F1}x{ActualHeight:F1}.");
    }

    protected override void OnClosed(EventArgs e)
    {
        _inkStyleController.StyleChanged -= OnInkStyleChanged;
        _toolFeedbackTimer.Stop();
        _toolFeedbackTimer.Tick -= OnToolFeedbackTimerTick;
        base.OnClosed(e);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_mode != InteractionMode.Draw)
        {
            return;
        }

        var position = e.GetPosition(InputSurface);
        if (_inkStyleController.CurrentTool == InkTool.Lasso)
        {
            BeginLassoOrSelectionMove(position);
            e.Handled = true;
            return;
        }

        if (_inkStyleController.CurrentTool is InkTool.PixelEraser or InkTool.ObjectEraser)
        {
            BeginErasing(position);
            e.Handled = true;
            return;
        }

        _activeStrokeId = Guid.NewGuid();
        _activeStrokeIsPersistent = _inkStyleController.IsPersistent ||
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        _activeStroke = new StrokePointBuffer();
        _activeInkTool = _inkStyleController.CurrentTool;
        _activeStrokeSize = _inkStyleController.CurrentSize;
        _activeGeometryBuildCount = 0;
        _activeGeometryBuildTotalMilliseconds = 0;
        _activeGeometryBuildMaximumMilliseconds = 0;
        _activeOutlineBuilder = CreateOutlineBuilder(
            _activeInkTool,
            _activeStrokeSize);
        _activeInkCoreOutlineBuilder = _activeInkTool == InkTool.Pen
            ? CreateInkCoreOutlineBuilder(_activeStrokeSize)
            : null;

        var inkColor = _inkStyleController.CurrentColor;
        var baseColor = _activeInkTool == InkTool.Pen
            ? BlendColor(inkColor, new InkColor(255, 255, 255), 0.04)
            : inkColor;
        var brush = CreateBrush(0xFF, baseColor);

        var baseOpacity = _inkStyleController.CurrentOpacity;
        _activePath = new Path
        {
            Fill = brush,
            Opacity = baseOpacity,
            IsHitTestVisible = false,
            SnapsToDevicePixels = false,
        };
        var inkCoreBaseOpacity = 0d;
        if (_activeInkTool == InkTool.Pen)
        {
            var coreColor = BlendColor(inkColor, new InkColor(0, 0, 0), 0.28);
            inkCoreBaseOpacity = baseOpacity * NaturalInkCoreOpacity;
            _activeInkCorePath = new Path
            {
                Fill = CreateBrush(0xFF, coreColor),
                Opacity = inkCoreBaseOpacity,
                IsHitTestVisible = false,
                SnapsToDevicePixels = false,
            };
        }

        _lastAcceptedPointMilliseconds = _inputClock.ElapsedMilliseconds;
        _lastAcceptedInputPoint = e.GetPosition(InputSurface);
        AddPoint(_lastAcceptedInputPoint);
        _strokeElements.Add(_activeStrokeId, new StrokeVisual(
            _activePath,
            _activeInkCorePath,
            baseOpacity,
            inkCoreBaseOpacity));
        InkSurface.Children.Add(_activePath);
        if (_activeInkCorePath is not null)
        {
            InkSurface.Children.Add(_activeInkCorePath);
        }
        InputSurface.CaptureMouse();

        _logger.Info($"Mouse {_inkStyleController.CurrentTool} stroke started on overlay {_display.Id}.");
        StrokeStarted?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_cursorStyleDirty)
        {
            UpdateCursorPreviewStyle();
        }

        var position = e.GetPosition(InputSurface);
        PositionCursorPreview(position);

        if (_lassoPath is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            AddLassoPoint(position);
            e.Handled = true;
            return;
        }

        if (_isMovingSelection && e.LeftButton == MouseButtonState.Pressed)
        {
            ApplySelectionMove(position);
            e.Handled = true;
            return;
        }

        if (_isErasing && e.LeftButton == MouseButtonState.Pressed)
        {
            ApplyEraser(position);
            e.Handled = true;
            return;
        }

        if (_activeStroke is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        AddPoint(position);
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_lassoPath is not null)
        {
            AddLassoPoint(e.GetPosition(InputSurface));
            CompleteLasso();
            e.Handled = true;
            return;
        }

        if (_isMovingSelection)
        {
            ApplySelectionMove(e.GetPosition(InputSurface));
            CompleteSelectionMove();
            e.Handled = true;
            return;
        }

        if (_isErasing)
        {
            ApplyEraser(e.GetPosition(InputSurface));
            EndErasing();
            e.Handled = true;
            return;
        }

        if (_activeStroke is null || _activePath is null)
        {
            return;
        }

        var releasedAt = _inputClock.ElapsedMilliseconds;
        var releasePosition = e.GetPosition(InputSurface);
        var releaseDelta = releasePosition - _lastAcceptedInputPoint;
        var shouldRecognizeShape = _activeInkTool == InkTool.Pen &&
            releasedAt - _lastAcceptedPointMilliseconds >= 280 &&
            releaseDelta.Length <= 4;
        _activeStroke.Complete(CreateStrokePoint(releasePosition));
        UpdateActiveGeometry();

        var completedStrokeId = _activeStrokeId;
        var completedPath = _activePath;
        var completedInkCorePath = _activeInkCorePath;
        var freehandGeometry = completedPath.Data;
        var freehandInkCoreGeometry = completedInkCorePath?.Data;
        var completedPoints = _activeStroke.Points.ToArray();
        RecordStrokeQualitySample(
            completedStrokeId,
            _activeStroke,
            completedPath,
            _activeStrokeIsPersistent);
        var completedSize = _activeStrokeSize;
        _activeStroke = null;
        _activeOutlineBuilder = null;
        _activeInkCoreOutlineBuilder = null;
        _activePath = null;
        _activeInkCorePath = null;
        _activeStrokeId = Guid.Empty;
        InputSurface.ReleaseMouseCapture();

        var completedStrokeIsPersistent = _activeStrokeIsPersistent;
        _activeStrokeIsPersistent = false;
        StrokeCompleted?.Invoke(
            this,
            new StrokeCompletedEventArgs(completedStrokeId, completedStrokeIsPersistent));

        if (shouldRecognizeShape && _shapeRecognizer.Recognize(completedPoints) is { } shape)
        {
            var snappedGeometry = ShapePathGeometryBuilder.Create(shape, completedSize);
            var snappedInkCoreGeometry = completedInkCorePath is null
                ? null
                : ShapePathGeometryBuilder.Create(shape, completedSize * NaturalInkCoreSizeRatio);
            completedPath.Data = snappedGeometry;
            if (completedInkCorePath is not null && snappedInkCoreGeometry is not null)
            {
                completedInkCorePath.Data = snappedInkCoreGeometry;
            }
            StrokeGeometryChanged?.Invoke(
                this,
                new StrokeGeometryChangedEventArgs(
                [
                    new StrokeGeometryChange(
                        completedStrokeId,
                        freehandGeometry,
                        snappedGeometry,
                        freehandInkCoreGeometry,
                        snappedInkCoreGeometry),
                ],
                "shape snap"));
            ShowToolFeedback($"{shape.Kind} snapped");
        }

        e.Handled = true;
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        CancelLasso();
        CancelSelectionMove(restoreOriginals: true);
        CancelActiveStroke();
        PointerModeRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_mode == InteractionMode.Draw)
        {
            if (_cursorStyleDirty)
            {
                UpdateCursorPreviewStyle();
            }

            PositionCursorPreview(e.GetPosition(InputSurface));
            CursorPreview.Visibility = Visibility.Visible;
        }
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        CursorPreview.Visibility = Visibility.Collapsed;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_mode != InteractionMode.Draw)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.Shift)
        {
            _inkStyleController.CycleTool(e.Delta > 0 ? -1 : 1);
            ShowToolFeedback();
            e.Handled = true;
        }
        else if (modifiers == ModifierKeys.Control)
        {
            _inkStyleController.AdjustSize(e.Delta);
            e.Handled = true;
        }
    }

    private void AddPoint(System.Windows.Point point)
    {
        if (_activeStroke is not null && _activeStroke.Add(CreateStrokePoint(point)))
        {
            _lastAcceptedPointMilliseconds = _inputClock.ElapsedMilliseconds;
            _lastAcceptedInputPoint = point;
            UpdateActiveGeometry();
        }
    }

    private StrokePoint CreateStrokePoint(System.Windows.Point point)
    {
        return new StrokePoint(point.X, point.Y, _inputClock.ElapsedMilliseconds);
    }

    private void UpdateActiveGeometry()
    {
        if (_activeStroke is null || _activePath is null || _activeOutlineBuilder is null)
        {
            return;
        }

        var started = _strokeQualityRecorder.IsEnabled ? Stopwatch.GetTimestamp() : 0;
        var renderablePoints = _activeStroke.GetRenderablePoints();
        var outline = _activeOutlineBuilder.Build(renderablePoints);
        _activePath.Data = FreehandPathGeometryBuilder.Create(outline);
        if (_activeInkCorePath is not null && _activeInkCoreOutlineBuilder is not null)
        {
            var inkCoreOutline = _activeInkCoreOutlineBuilder.Build(renderablePoints);
            _activeInkCorePath.Data = FreehandPathGeometryBuilder.Create(inkCoreOutline);
        }
        if (_strokeQualityRecorder.IsEnabled)
        {
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            _activeGeometryBuildCount++;
            _activeGeometryBuildTotalMilliseconds += elapsed;
            _activeGeometryBuildMaximumMilliseconds = Math.Max(
                _activeGeometryBuildMaximumMilliseconds,
                elapsed);
        }
    }

    private void RecordStrokeQualitySample(
        Guid strokeId,
        StrokePointBuffer stroke,
        Path path,
        bool isPersistent)
    {
        if (!_strokeQualityRecorder.IsEnabled)
        {
            return;
        }

        var rawPoints = stroke.RawPoints.ToArray();
        var filteredPoints = stroke.Points.ToArray();
        var duration = rawPoints.Length < 2
            ? 0
            : rawPoints[^1].TimestampMilliseconds - rawPoints[0].TimestampMilliseconds;
        var closureDistance = rawPoints.Length < 2
            ? 0
            : rawPoints[0].DistanceTo(rawPoints[^1]);
        var bounds = path.Data.Bounds;
        var dpi = VisualTreeHelper.GetDpi(this);
        _strokeQualityRecorder.Record(new StrokeQualitySample(
            strokeId,
            DateTimeOffset.UtcNow,
            "9C-natural-ink-b",
            _display.Id,
            _activeInkTool,
            _activeStrokeSize,
            dpi.DpiScaleX,
            dpi.DpiScaleY,
            isPersistent,
            duration,
            closureDistance,
            _activeGeometryBuildCount,
            _activeGeometryBuildTotalMilliseconds,
            _activeGeometryBuildMaximumMilliseconds,
            new StrokeQualityBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            rawPoints,
            filteredPoints));
    }

    private void CancelActiveStroke()
    {
        if (_activeStroke is null || _activePath is null)
        {
            return;
        }

        _strokeElements.Remove(_activeStrokeId);
        InkSurface.Children.Remove(_activePath);
        if (_activeInkCorePath is not null)
        {
            InkSurface.Children.Remove(_activeInkCorePath);
        }
        _activeStroke = null;
        _activeOutlineBuilder = null;
        _activeInkCoreOutlineBuilder = null;
        _activePath = null;
        _activeInkCorePath = null;
        _activeStrokeId = Guid.Empty;
        _activeStrokeIsPersistent = false;
        InputSurface.ReleaseMouseCapture();
    }

    private void BeginErasing(System.Windows.Point point)
    {
        _isErasing = true;
        _eraserOriginalGeometries.Clear();
        InputSurface.CaptureMouse();
        ApplyEraser(point);
        StrokeStarted?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyEraser(System.Windows.Point point)
    {
        var radius = _inkStyleController.CurrentSize / 2;
        var eraserGeometry = new EllipseGeometry(point, radius, radius);

        if (_inkStyleController.CurrentTool == InkTool.ObjectEraser)
        {
            var hit = _strokeElements
                .Reverse()
                .FirstOrDefault(pair =>
                    pair.Value.Element.Data.FillContainsWithDetail(eraserGeometry) != IntersectionDetail.Empty);
            if (hit.Key != Guid.Empty)
            {
                StrokeEraseRequested?.Invoke(this, new StrokeEraseRequestedEventArgs(hit.Key));
            }

            return;
        }

        foreach (var pair in _strokeElements.ToArray())
        {
            var geometry = pair.Value.Element.Data;
            if (geometry.FillContainsWithDetail(eraserGeometry) == IntersectionDetail.Empty)
            {
                continue;
            }

            if (!_eraserOriginalGeometries.ContainsKey(pair.Key))
            {
                _eraserOriginalGeometries.Add(pair.Key, new StrokeGeometryPair(
                    geometry,
                    pair.Value.InkCoreElement?.Data));
            }

            var erased = Geometry.Combine(
                geometry,
                eraserGeometry,
                GeometryCombineMode.Exclude,
                transform: null);
            erased.Freeze();
            pair.Value.Element.Data = erased;
            if (pair.Value.InkCoreElement is not null)
            {
                var erasedInkCore = Geometry.Combine(
                    pair.Value.InkCoreElement.Data,
                    eraserGeometry,
                    GeometryCombineMode.Exclude,
                    transform: null);
                erasedInkCore.Freeze();
                pair.Value.InkCoreElement.Data = erasedInkCore;
            }
        }
    }

    private void EndErasing()
    {
        _isErasing = false;
        InputSurface.ReleaseMouseCapture();

        if (_eraserOriginalGeometries.Count == 0)
        {
            return;
        }

        var changes = new List<StrokeGeometryChange>();
        foreach (var pair in _eraserOriginalGeometries)
        {
            if (_strokeElements.TryGetValue(pair.Key, out var stroke))
            {
                changes.Add(new StrokeGeometryChange(
                    pair.Key,
                    pair.Value.Main,
                    stroke.Element.Data,
                    pair.Value.InkCore,
                    stroke.InkCoreElement?.Data));
            }
        }

        _eraserOriginalGeometries.Clear();
        if (changes.Count > 0)
        {
            StrokeGeometryChanged?.Invoke(
                this,
                new StrokeGeometryChangedEventArgs(changes, "pixel erase"));
        }
    }

    private void BeginLassoOrSelectionMove(System.Windows.Point point)
    {
        if (_selectedStrokeIds.Count > 0 && _selectionBounds.Contains(point))
        {
            _isMovingSelection = true;
            _selectionDragStart = point;
            _selectionMoveOriginalGeometries.Clear();
            foreach (var strokeId in _selectedStrokeIds)
            {
                if (_strokeElements.TryGetValue(strokeId, out var stroke))
                {
                    _selectionMoveOriginalGeometries[strokeId] = new StrokeGeometryPair(
                        stroke.Element.Data,
                        stroke.InkCoreElement?.Data);
                }
            }

            InputSurface.CaptureMouse();
            return;
        }

        ClearSelection();
        _lassoPoints.Clear();
        _lassoPoints.Add(point);
        var lassoStroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 64, 145, 255));
        lassoStroke.Freeze();
        var fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(18, 64, 145, 255));
        fill.Freeze();
        _lassoPath = new Path
        {
            Stroke = lassoStroke,
            StrokeThickness = 1.5,
            StrokeDashArray = [4, 3],
            Fill = fill,
            IsHitTestVisible = false,
            SnapsToDevicePixels = false,
        };
        InkSurface.Children.Add(_lassoPath);
        UpdateLassoGeometry(closeFigure: false);
        InputSurface.CaptureMouse();
    }

    private void AddLassoPoint(System.Windows.Point point)
    {
        if (_lassoPath is null ||
            (_lassoPoints.Count > 0 && (point - _lassoPoints[^1]).Length < 2))
        {
            return;
        }

        _lassoPoints.Add(point);
        UpdateLassoGeometry(closeFigure: false);
    }

    private void CompleteLasso()
    {
        if (_lassoPath is null)
        {
            return;
        }

        UpdateLassoGeometry(closeFigure: true);
        var lassoGeometry = _lassoPath.Data;
        InkSurface.Children.Remove(_lassoPath);
        _lassoPath = null;
        InputSurface.ReleaseMouseCapture();

        if (_lassoPoints.Count >= 3)
        {
            foreach (var pair in _strokeElements)
            {
                if (lassoGeometry.FillContainsWithDetail(pair.Value.Element.Data) != IntersectionDetail.Empty)
                {
                    _selectedStrokeIds.Add(pair.Key);
                }
            }
        }

        _lassoPoints.Clear();
        UpdateSelectionAdorner();
        RaiseSelectionChanged();
        if (_selectedStrokeIds.Count > 0)
        {
            ShowToolFeedback($"Selected {_selectedStrokeIds.Count}");
        }
    }

    private void CancelLasso()
    {
        if (_lassoPath is null)
        {
            return;
        }

        InkSurface.Children.Remove(_lassoPath);
        _lassoPath = null;
        _lassoPoints.Clear();
        InputSurface.ReleaseMouseCapture();
    }

    private void UpdateLassoGeometry(bool closeFigure)
    {
        if (_lassoPath is null || _lassoPoints.Count == 0)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(_lassoPoints[0], isFilled: closeFigure, isClosed: closeFigure);
            if (_lassoPoints.Count > 1)
            {
                context.PolyLineTo(_lassoPoints.Skip(1).ToArray(), isStroked: true, isSmoothJoin: true);
            }
        }

        geometry.Freeze();
        _lassoPath.Data = geometry;
    }

    private void ApplySelectionMove(System.Windows.Point point)
    {
        if (!_isMovingSelection)
        {
            return;
        }

        var offset = point - _selectionDragStart;
        foreach (var pair in _selectionMoveOriginalGeometries)
        {
            if (!_strokeElements.TryGetValue(pair.Key, out var stroke))
            {
                continue;
            }

            var moved = new GeometryGroup
            {
                Transform = new TranslateTransform(offset.X, offset.Y),
            };
            moved.Children.Add(pair.Value.Main);
            moved.Freeze();
            stroke.Element.Data = moved;
            if (stroke.InkCoreElement is not null && pair.Value.InkCore is not null)
            {
                var movedInkCore = new GeometryGroup
                {
                    Transform = new TranslateTransform(offset.X, offset.Y),
                };
                movedInkCore.Children.Add(pair.Value.InkCore);
                movedInkCore.Freeze();
                stroke.InkCoreElement.Data = movedInkCore;
            }
        }

        UpdateSelectionAdorner();
    }

    private void CompleteSelectionMove()
    {
        if (!_isMovingSelection)
        {
            return;
        }

        _isMovingSelection = false;
        InputSurface.ReleaseMouseCapture();
        var changes = _selectionMoveOriginalGeometries
            .Where(pair => _strokeElements.ContainsKey(pair.Key))
            .Select(pair => new StrokeGeometryChange(
                pair.Key,
                pair.Value.Main,
                _strokeElements[pair.Key].Element.Data,
                pair.Value.InkCore,
                _strokeElements[pair.Key].InkCoreElement?.Data))
            .Where(change => !change.Before.Bounds.Equals(change.After.Bounds))
            .ToArray();
        _selectionMoveOriginalGeometries.Clear();

        if (changes.Length > 0)
        {
            StrokeGeometryChanged?.Invoke(
                this,
                new StrokeGeometryChangedEventArgs(changes, "selection move"));
        }
    }

    private void CancelSelectionMove(bool restoreOriginals)
    {
        if (!_isMovingSelection)
        {
            return;
        }

        if (restoreOriginals)
        {
            foreach (var pair in _selectionMoveOriginalGeometries)
            {
                if (_strokeElements.TryGetValue(pair.Key, out var stroke))
                {
                    stroke.Element.Data = pair.Value.Main;
                    if (stroke.InkCoreElement is not null)
                    {
                        stroke.InkCoreElement.Data = pair.Value.InkCore ?? Geometry.Empty;
                    }
                }
            }
        }

        _isMovingSelection = false;
        _selectionMoveOriginalGeometries.Clear();
        InputSurface.ReleaseMouseCapture();
        UpdateSelectionAdorner();
    }

    internal void ClearSelection()
    {
        if (_selectedStrokeIds.Count == 0)
        {
            return;
        }

        _selectedStrokeIds.Clear();
        UpdateSelectionAdorner();
        RaiseSelectionChanged();
    }

    private void UpdateSelectionAdorner()
    {
        var bounds = System.Windows.Rect.Empty;
        foreach (var strokeId in _selectedStrokeIds)
        {
            if (_strokeElements.TryGetValue(strokeId, out var stroke))
            {
                bounds.Union(stroke.Element.Data.Bounds);
            }
        }

        if (bounds.IsEmpty)
        {
            _selectionBounds = System.Windows.Rect.Empty;
            SelectionAdorner.Visibility = Visibility.Collapsed;
            return;
        }

        bounds.Inflate(6, 6);
        _selectionBounds = bounds;
        SelectionAdorner.Width = Math.Max(1, bounds.Width);
        SelectionAdorner.Height = Math.Max(1, bounds.Height);
        SelectionAdorner.Margin = new Thickness(bounds.X, bounds.Y, 0, 0);
        SelectionAdorner.Visibility = Visibility.Visible;
    }

    private void RaiseSelectionChanged()
    {
        SelectionChanged?.Invoke(
            this,
            new SelectionChangedEventArgs(_selectedStrokeIds.ToArray()));
    }

    private void OnInkStyleChanged(object? sender, EventArgs e)
    {
        if (_inkStyleController.CurrentTool != InkTool.Lasso && _selectedStrokeIds.Count > 0)
        {
            ClearSelection();
        }

        if (_mode == InteractionMode.Draw && IsMouseOver)
        {
            UpdateCursorPreviewStyle();
            return;
        }

        _cursorStyleDirty = true;
    }

    private void UpdateCursorPreviewStyle()
    {
        _cursorStyleDirty = false;
        var size = _inkStyleController.CurrentSize;
        var color = _inkStyleController.CurrentColor;
        var isEraser = _inkStyleController.CurrentTool is InkTool.PixelEraser or InkTool.ObjectEraser;
        var isLasso = _inkStyleController.CurrentTool == InkTool.Lasso;
        var previewSize = isLasso
            ? 22
            : _inkStyleController.CurrentTool == InkTool.ObjectEraser
                ? size + 6
                : size;
        CursorPreview.Width = previewSize;
        CursorPreview.Height = previewSize;

        InkCursorPreview.Visibility = isEraser || isLasso ? Visibility.Collapsed : Visibility.Visible;
        EraserCursorPreview.Visibility = isEraser ? Visibility.Visible : Visibility.Collapsed;
        LassoCursorPreview.Visibility = isLasso ? Visibility.Visible : Visibility.Collapsed;

        if (isLasso)
        {
            return;
        }

        if (!isEraser)
        {
            InkCursorPreview.Fill = CreateBrush(
                _inkStyleController.CurrentTool == InkTool.Highlighter ? (byte)72 : (byte)92,
                color);
            InkCursorPreview.Stroke = CreateBrush(210, color);
            return;
        }

        EraserAccentBorder.BorderBrush = CreateBrush(255, color);
        EraserCursorPreview.CornerRadius = _inkStyleController.CurrentTool == InkTool.PixelEraser
            ? new CornerRadius(2)
            : new CornerRadius(Math.Max(5, previewSize * 0.28));
        EraserAccentBorder.CornerRadius = _inkStyleController.CurrentTool == InkTool.PixelEraser
            ? new CornerRadius(1)
            : new CornerRadius(Math.Max(3, previewSize * 0.2));
        EraserCursorGlyph.Text = _inkStyleController.CurrentTool == InkTool.PixelEraser ? "+" : "×";
        EraserCursorGlyph.FontSize = Math.Clamp(previewSize * 0.5, 10, 18);
    }

    private void PositionCursorPreview(System.Windows.Point point)
    {
        CursorPreview.Margin = new Thickness(
            point.X - (CursorPreview.Width / 2),
            point.Y - (CursorPreview.Height / 2),
            0,
            0);
        ToolFeedback.Margin = new Thickness(point.X + 14, point.Y + 14, 0, 0);
    }

    private void ShowToolFeedback()
    {
        ShowToolFeedback(_inkStyleController.CurrentTool switch
        {
            InkTool.Pen => "Pen",
            InkTool.Highlighter => "Highlighter",
            InkTool.PixelEraser => "Pixel Eraser",
            InkTool.ObjectEraser => "Object Eraser",
            InkTool.Lasso => "Lasso Selection",
            _ => "Tool",
        });
    }

    private void ShowToolFeedback(string text)
    {
        ToolFeedbackText.Text = text;
        ToolFeedback.Visibility = Visibility.Visible;
        _toolFeedbackTimer.Stop();
        _toolFeedbackTimer.Start();
    }

    private void OnToolFeedbackTimerTick(object? sender, EventArgs e)
    {
        _toolFeedbackTimer.Stop();
        ToolFeedback.Visibility = Visibility.Collapsed;
    }

    private static FreehandStrokeOutlineBuilder CreateOutlineBuilder(InkTool tool, double size)
    {
        return new FreehandStrokeOutlineBuilder(new FreehandStrokeOptions
        {
            Size = size,
            Thinning = tool == InkTool.Highlighter ? 0 : 0.48,
            StartTaperLength = 0,
            EndTaperLength = 0,
            MaximumSpeed = 0.85,
            PressureSmoothing = 0.36,
        });
    }

    private static FreehandStrokeOutlineBuilder CreateInkCoreOutlineBuilder(double size)
    {
        return new FreehandStrokeOutlineBuilder(new FreehandStrokeOptions
        {
            Size = size * NaturalInkCoreSizeRatio,
            Thinning = 0.76,
            StartTaperLength = 0,
            EndTaperLength = 0,
            MaximumSpeed = 0.85,
            PressureSmoothing = 0.40,
        });
    }

    private static InkColor BlendColor(InkColor from, InkColor toward, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return new InkColor(
            (byte)Math.Round(from.Red + ((toward.Red - from.Red) * amount)),
            (byte)Math.Round(from.Green + ((toward.Green - from.Green) * amount)),
            (byte)Math.Round(from.Blue + ((toward.Blue - from.Blue) * amount)));
    }

    private static SolidColorBrush CreateBrush(byte alpha, InkColor color)
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            alpha,
            color.Red,
            color.Green,
            color.Blue));
        brush.Freeze();
        return brush;
    }

    private sealed record StrokeVisual(
        Path Element,
        Path? InkCoreElement,
        double BaseOpacity,
        double InkCoreBaseOpacity);

    private sealed record StrokeGeometryPair(Geometry Main, Geometry? InkCore);
}
