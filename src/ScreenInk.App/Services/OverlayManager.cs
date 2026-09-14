using System.Diagnostics;
using System.Windows.Threading;
using ScreenInk.App.Diagnostics;
using ScreenInk.App.Overlay;
using ScreenInk.Core.Diagnostics;
using ScreenInk.Core.Ink;
using ScreenInk.Core.Interaction;
using Forms = System.Windows.Forms;

namespace ScreenInk.App.Services;

internal sealed class OverlayManager : IDisposable
{
    private readonly DisplayTopologyService _displayTopology;
    private readonly InteractionModeController _modeController;
    private readonly IAppLogger _logger;
    private readonly InkStyleController _inkStyleController;
    private readonly StrokeQualityRecorder _strokeQualityRecorder;
    private readonly Stopwatch _lifetimeClock = Stopwatch.StartNew();
    private readonly TemporaryInkLifetimeManager _lifetimeManager = new();
    private readonly DispatcherTimer _lifetimeTimer;
    private readonly List<OverlayWindow> _windows = [];
    private readonly Stack<IHistoryAction> _undoHistory = [];
    private readonly Stack<IHistoryAction> _redoHistory = [];
    private readonly Dictionary<Guid, OverlayWindow> _strokeOwners = [];
    private readonly HashSet<Guid> _persistentStrokeIds = [];
    private readonly HashSet<Guid> _selectionPausedStrokeIds = [];
    private bool _disposed;

    public event EventHandler? StrokeStarted;

    public OverlayManager(
        DisplayTopologyService displayTopology,
        InteractionModeController modeController,
        InkStyleController inkStyleController,
        StrokeQualityRecorder strokeQualityRecorder,
        IAppLogger logger)
    {
        _displayTopology = displayTopology ?? throw new ArgumentNullException(nameof(displayTopology));
        _modeController = modeController ?? throw new ArgumentNullException(nameof(modeController));
        _inkStyleController = inkStyleController ?? throw new ArgumentNullException(nameof(inkStyleController));
        _strokeQualityRecorder = strokeQualityRecorder ?? throw new ArgumentNullException(nameof(strokeQualityRecorder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _lifetimeTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16),
        };
        _lifetimeTimer.Tick += OnLifetimeTick;

        _displayTopology.DisplaysChanged += OnDisplaysChanged;
        _modeController.ModeChanged += OnModeChanged;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RebuildOverlays();
    }

    public void ClearAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entries = new List<ClearedStrokeEntry>();
        foreach (var overlay in _windows)
        {
            var snapshots = overlay.GetStrokeSnapshots();
            foreach (var snapshot in snapshots.OrderByDescending(stroke => stroke.CanvasIndex))
            {
                var wasPersistent = _persistentStrokeIds.Contains(snapshot.StrokeId);
                if (DetachTrackedStroke(overlay, snapshot.StrokeId) is { } detached)
                {
                    entries.Add(new ClearedStrokeEntry(overlay, detached, wasPersistent));
                }
            }
        }

        if (entries.Count == 0)
        {
            return;
        }

        RecordAction(new ClearHistoryAction(entries, "clear all"));
        _logger.Info("Cleared all strokes.");
    }

    public void DeleteSelectionOrClearAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entries = new List<ClearedStrokeEntry>();
        foreach (var overlay in _windows)
        {
            foreach (var strokeId in overlay.SelectedStrokeIds.ToArray())
            {
                var wasPersistent = _persistentStrokeIds.Contains(strokeId);
                if (DetachTrackedStroke(overlay, strokeId) is { } detached)
                {
                    entries.Add(new ClearedStrokeEntry(overlay, detached, wasPersistent));
                }
            }
        }

        if (entries.Count == 0)
        {
            ClearAll();
            return;
        }

        RecordAction(new ClearHistoryAction(entries, "selection delete"));
        _logger.Info($"Deleted {entries.Count} selected stroke(s).");
    }

    public void CycleTool(int direction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var previousTool = _inkStyleController.CurrentTool;
        _inkStyleController.CycleTool(direction);
        if (_inkStyleController.CurrentTool == previousTool)
        {
            return;
        }

        GetOverlayAtCursor()?.ShowCurrentToolFeedback();
    }

    public void AdjustCurrentToolSize(int direction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var previousSize = _inkStyleController.CurrentSizeIndex;
        _inkStyleController.AdjustSize(direction);
        if (_inkStyleController.CurrentSizeIndex == previousSize)
        {
            return;
        }

        GetOverlayAtCursor()?.ShowCurrentSizeFeedback();
    }

    private OverlayWindow? GetOverlayAtCursor()
    {
        var cursor = Forms.Cursor.Position;
        return _windows.FirstOrDefault(overlay => overlay.ContainsScreenPoint(cursor.X, cursor.Y));
    }

    public void UndoLastStroke()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (_undoHistory.TryPop(out var action))
        {
            if (!action.Undo(this))
            {
                continue;
            }

            _redoHistory.Push(action);
            _logger.Info($"Undid {action.Description}.");
            return;
        }
    }

    public void RedoLastAction()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (_redoHistory.TryPop(out var action))
        {
            if (!action.Redo(this))
            {
                continue;
            }

            _undoHistory.Push(action);
            _logger.Info($"Redid {action.Description}.");
            return;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _displayTopology.DisplaysChanged -= OnDisplaysChanged;
        _modeController.ModeChanged -= OnModeChanged;
        _lifetimeTimer.Stop();
        _lifetimeTimer.Tick -= OnLifetimeTick;
        CloseOverlays();
        _disposed = true;
    }

    private void RebuildOverlays()
    {
        _modeController.ReturnToPointer();
        CloseOverlays();

        foreach (var display in _displayTopology.GetDisplays())
        {
            var overlay = new OverlayWindow(
                display,
                _modeController.Current,
                _inkStyleController,
                _strokeQualityRecorder,
                _logger);
            overlay.PointerModeRequested += OnPointerModeRequested;
            overlay.StrokeCompleted += OnStrokeCompleted;
            overlay.StrokeStarted += OnStrokeStarted;
            overlay.StrokeEraseRequested += OnStrokeEraseRequested;
            overlay.StrokeGeometryChanged += OnStrokeGeometryChanged;
            overlay.SelectionChanged += OnSelectionChanged;
            _windows.Add(overlay);
            overlay.Show();
        }

        _logger.Info($"Created {_windows.Count} overlay window(s).");
    }

    private void CloseOverlays()
    {
        foreach (var overlay in _windows)
        {
            overlay.PointerModeRequested -= OnPointerModeRequested;
            overlay.StrokeCompleted -= OnStrokeCompleted;
            overlay.StrokeStarted -= OnStrokeStarted;
            overlay.StrokeEraseRequested -= OnStrokeEraseRequested;
            overlay.StrokeGeometryChanged -= OnStrokeGeometryChanged;
            overlay.SelectionChanged -= OnSelectionChanged;
            overlay.Close();
        }

        _windows.Clear();
        ClearTrackingAndHistory();
    }

    private void ClearTrackingAndHistory()
    {
        _undoHistory.Clear();
        _redoHistory.Clear();
        _strokeOwners.Clear();
        _persistentStrokeIds.Clear();
        _selectionPausedStrokeIds.Clear();
        _lifetimeManager.Clear();
        _lifetimeTimer.Stop();
    }

    private void OnDisplaysChanged(object? sender, EventArgs e)
    {
        RebuildOverlays();
    }

    private void OnModeChanged(object? sender, InteractionModeChangedEventArgs e)
    {
        foreach (var overlay in _windows)
        {
            try
            {
                overlay.SetMode(e.Current);
            }
            catch (Exception exception)
            {
                _logger.Error($"Unable to apply {e.Current} to overlay {overlay.DisplayId}.", exception);
                _modeController.ReturnToPointer();
                break;
            }
        }
    }

    private void OnPointerModeRequested(object? sender, EventArgs e)
    {
        _modeController.ReturnToPointer();
    }

    private void OnStrokeCompleted(object? sender, StrokeCompletedEventArgs e)
    {
        if (sender is not OverlayWindow overlay || overlay.GetStrokeSnapshot(e.StrokeId) is not { } stroke)
        {
            return;
        }

        TrackStroke(overlay, stroke.StrokeId, e.IsPersistent);
        RecordAction(new StrokePresenceHistoryAction(
            overlay,
            stroke,
            e.IsPersistent,
            presentAfterAction: true,
            description: "stroke"));

        if (e.IsPersistent)
        {
            _logger.Info("Created a persistent stroke.");
        }
    }

    private void OnStrokeEraseRequested(object? sender, StrokeEraseRequestedEventArgs e)
    {
        if (sender is not OverlayWindow overlay)
        {
            return;
        }

        var wasPersistent = _persistentStrokeIds.Contains(e.StrokeId);
        var detached = DetachTrackedStroke(overlay, e.StrokeId);
        if (detached is null)
        {
            return;
        }

        RecordAction(new StrokePresenceHistoryAction(
            overlay,
            detached,
            wasPersistent,
            presentAfterAction: false,
            description: "object erase"));
    }

    private void OnStrokeGeometryChanged(object? sender, StrokeGeometryChangedEventArgs e)
    {
        if (sender is OverlayWindow overlay && e.Changes.Count > 0)
        {
            RecordAction(new GeometryHistoryAction(overlay, e.Changes, e.Description));
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedStrokeIds = _windows
            .SelectMany(overlay => overlay.SelectedStrokeIds)
            .ToHashSet();

        foreach (var strokeId in _selectionPausedStrokeIds
            .Where(strokeId => !selectedStrokeIds.Contains(strokeId))
            .ToArray())
        {
            _selectionPausedStrokeIds.Remove(strokeId);
            if (!_strokeOwners.TryGetValue(strokeId, out var owner) ||
                _persistentStrokeIds.Contains(strokeId))
            {
                continue;
            }

            var group = _lifetimeManager.AddStroke(strokeId, _lifetimeClock.ElapsedMilliseconds);
            foreach (var groupedStrokeId in group.StrokeIds)
            {
                if (_strokeOwners.TryGetValue(groupedStrokeId, out var groupedOwner))
                {
                    groupedOwner.SetStrokeOpacity(groupedStrokeId, 1);
                }
            }

            _lifetimeTimer.Start();
        }

        foreach (var strokeId in selectedStrokeIds)
        {
            if (_persistentStrokeIds.Contains(strokeId) ||
                !_strokeOwners.TryGetValue(strokeId, out var owner) ||
                !_selectionPausedStrokeIds.Add(strokeId))
            {
                continue;
            }

            _lifetimeManager.RemoveStroke(strokeId);
            owner.SetStrokeOpacity(strokeId, 1);
        }

        if (!_lifetimeManager.HasActiveGroups)
        {
            _lifetimeTimer.Stop();
        }
    }

    private void OnStrokeStarted(object? sender, EventArgs e)
    {
        StrokeStarted?.Invoke(this, EventArgs.Empty);
    }

    private void OnLifetimeTick(object? sender, EventArgs e)
    {
        var snapshots = _lifetimeManager.Evaluate(_lifetimeClock.ElapsedMilliseconds);
        foreach (var snapshot in snapshots)
        {
            foreach (var strokeId in snapshot.StrokeIds)
            {
                if (!_strokeOwners.TryGetValue(strokeId, out var owner))
                {
                    continue;
                }

                if (snapshot.IsExpired)
                {
                    owner.RemoveStroke(strokeId);
                    _strokeOwners.Remove(strokeId);
                }
                else
                {
                    owner.SetStrokeOpacity(strokeId, snapshot.Opacity);
                }
            }
        }

        if (!_lifetimeManager.HasActiveGroups)
        {
            _lifetimeTimer.Stop();
        }
    }

    private void RecordAction(IHistoryAction action)
    {
        _undoHistory.Push(action);
        _redoHistory.Clear();
    }

    private void TrackStroke(OverlayWindow overlay, Guid strokeId, bool isPersistent)
    {
        _strokeOwners[strokeId] = overlay;
        if (isPersistent)
        {
            _persistentStrokeIds.Add(strokeId);
            overlay.SetStrokeOpacity(strokeId, 1);
            return;
        }

        var group = _lifetimeManager.AddStroke(strokeId, _lifetimeClock.ElapsedMilliseconds);
        foreach (var groupedStrokeId in group.StrokeIds)
        {
            if (_strokeOwners.TryGetValue(groupedStrokeId, out var owner))
            {
                owner.SetStrokeOpacity(groupedStrokeId, 1);
            }
        }

        _lifetimeTimer.Start();
    }

    private DetachedStroke? DetachTrackedStroke(OverlayWindow overlay, Guid strokeId)
    {
        var detached = overlay.DetachStroke(strokeId);
        if (detached is null)
        {
            return null;
        }

        _strokeOwners.Remove(strokeId);
        _persistentStrokeIds.Remove(strokeId);
        _selectionPausedStrokeIds.Remove(strokeId);
        _lifetimeManager.RemoveStroke(strokeId);
        if (!_lifetimeManager.HasActiveGroups)
        {
            _lifetimeTimer.Stop();
        }

        return detached;
    }

    private bool RestoreTrackedStroke(OverlayWindow overlay, DetachedStroke stroke, bool isPersistent)
    {
        if (!overlay.RestoreStroke(stroke))
        {
            return false;
        }

        TrackStroke(overlay, stroke.StrokeId, isPersistent);
        return true;
    }

    private interface IHistoryAction
    {
        string Description { get; }

        bool Undo(OverlayManager manager);

        bool Redo(OverlayManager manager);
    }

    private sealed class StrokePresenceHistoryAction : IHistoryAction
    {
        private readonly OverlayWindow _overlay;
        private readonly bool _isPersistent;
        private readonly bool _presentAfterAction;
        private DetachedStroke _stroke;

        public StrokePresenceHistoryAction(
            OverlayWindow overlay,
            DetachedStroke stroke,
            bool isPersistent,
            bool presentAfterAction,
            string description)
        {
            _overlay = overlay;
            _stroke = stroke;
            _isPersistent = isPersistent;
            _presentAfterAction = presentAfterAction;
            Description = description;
        }

        public string Description { get; }

        public bool Undo(OverlayManager manager)
        {
            return _presentAfterAction ? Detach(manager) : Restore(manager);
        }

        public bool Redo(OverlayManager manager)
        {
            return _presentAfterAction ? Restore(manager) : Detach(manager);
        }

        private bool Detach(OverlayManager manager)
        {
            var detached = manager.DetachTrackedStroke(_overlay, _stroke.StrokeId);
            if (detached is null)
            {
                return false;
            }

            _stroke = detached;
            return true;
        }

        private bool Restore(OverlayManager manager)
        {
            return manager.RestoreTrackedStroke(_overlay, _stroke, _isPersistent);
        }
    }

    private sealed class GeometryHistoryAction : IHistoryAction
    {
        private readonly OverlayWindow _overlay;
        private readonly IReadOnlyList<StrokeGeometryChange> _changes;
        private readonly string _description;

        public GeometryHistoryAction(
            OverlayWindow overlay,
            IReadOnlyList<StrokeGeometryChange> changes,
            string description)
        {
            _overlay = overlay;
            _changes = changes;
            _description = description;
        }

        public string Description => _description;

        public bool Undo(OverlayManager manager) => Apply(useAfter: false);

        public bool Redo(OverlayManager manager) => Apply(useAfter: true);

        private bool Apply(bool useAfter)
        {
            var changed = false;
            foreach (var change in _changes)
            {
                changed |= _overlay.SetStrokeGeometry(
                    change.StrokeId,
                    useAfter ? change.After : change.Before,
                    useAfter ? change.InkCoreAfter : change.InkCoreBefore);
            }

            return changed;
        }
    }

    private sealed class ClearHistoryAction : IHistoryAction
    {
        private readonly List<ClearedStrokeEntry> _entries;
        private readonly string _description;

        public ClearHistoryAction(List<ClearedStrokeEntry> entries, string description)
        {
            _entries = entries;
            _description = description;
        }

        public string Description => _description;

        public bool Undo(OverlayManager manager)
        {
            var restored = false;
            foreach (var entry in _entries
                .OrderBy(item => item.Stroke.CanvasIndex))
            {
                restored |= manager.RestoreTrackedStroke(
                    entry.Overlay,
                    entry.Stroke,
                    entry.IsPersistent);
            }

            return restored;
        }

        public bool Redo(OverlayManager manager)
        {
            var removed = false;
            foreach (var entry in _entries
                .OrderByDescending(item => item.Stroke.CanvasIndex))
            {
                if (manager.DetachTrackedStroke(entry.Overlay, entry.Stroke.StrokeId) is not { } detached)
                {
                    continue;
                }

                entry.Stroke = detached;
                removed = true;
            }

            return removed;
        }
    }

    private sealed class ClearedStrokeEntry(
        OverlayWindow overlay,
        DetachedStroke stroke,
        bool isPersistent)
    {
        public OverlayWindow Overlay { get; } = overlay;

        public DetachedStroke Stroke { get; set; } = stroke;

        public bool IsPersistent { get; } = isPersistent;
    }
}
