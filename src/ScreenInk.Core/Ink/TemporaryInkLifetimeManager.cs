namespace ScreenInk.Core.Ink;

public sealed class TemporaryInkLifetimeManager
{
    private readonly AnnotationLifetimeOptions _options;
    private readonly List<TemporaryGroup> _groups = [];
    private TemporaryGroup? _currentGroup;

    public TemporaryInkLifetimeManager(AnnotationLifetimeOptions? options = null)
    {
        _options = options ?? new AnnotationLifetimeOptions();
        _options.Validate();
    }

    public bool HasActiveGroups => _groups.Count > 0;

    public AnnotationLifetimeSnapshot AddStroke(Guid strokeId, long nowMilliseconds)
    {
        if (strokeId == Guid.Empty)
        {
            throw new ArgumentException("Stroke ID cannot be empty.", nameof(strokeId));
        }

        if (nowMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        }

        var canJoinCurrentGroup = _currentGroup is not null &&
            nowMilliseconds >= _currentGroup.LastUpdatedMilliseconds &&
            nowMilliseconds - _currentGroup.LastUpdatedMilliseconds < _options.GroupingWindowMilliseconds &&
            nowMilliseconds < _currentGroup.LastUpdatedMilliseconds + _options.TotalDurationMilliseconds;

        if (!canJoinCurrentGroup)
        {
            _currentGroup = new TemporaryGroup(Guid.NewGuid(), nowMilliseconds);
            _groups.Add(_currentGroup);
        }

        _currentGroup!.StrokeIds.Add(strokeId);
        _currentGroup.LastUpdatedMilliseconds = nowMilliseconds;

        return CreateSnapshot(_currentGroup, nowMilliseconds);
    }

    public IReadOnlyList<AnnotationLifetimeSnapshot> Evaluate(long nowMilliseconds)
    {
        if (nowMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        }

        var snapshots = new AnnotationLifetimeSnapshot[_groups.Count];
        for (var index = 0; index < _groups.Count; index++)
        {
            snapshots[index] = CreateSnapshot(_groups[index], nowMilliseconds);
        }

        _groups.RemoveAll(group =>
            nowMilliseconds >= group.LastUpdatedMilliseconds + _options.TotalDurationMilliseconds);

        if (_currentGroup is not null && !_groups.Contains(_currentGroup))
        {
            _currentGroup = null;
        }

        return snapshots;
    }

    public void RemoveStroke(Guid strokeId)
    {
        for (var index = _groups.Count - 1; index >= 0; index--)
        {
            var group = _groups[index];
            if (!group.StrokeIds.Remove(strokeId))
            {
                continue;
            }

            if (group.StrokeIds.Count == 0)
            {
                _groups.RemoveAt(index);
                if (ReferenceEquals(_currentGroup, group))
                {
                    _currentGroup = null;
                }
            }

            return;
        }
    }

    public void Clear()
    {
        _groups.Clear();
        _currentGroup = null;
    }

    private AnnotationLifetimeSnapshot CreateSnapshot(TemporaryGroup group, long nowMilliseconds)
    {
        var elapsed = Math.Max(0, nowMilliseconds - group.LastUpdatedMilliseconds);
        var isExpired = elapsed >= _options.TotalDurationMilliseconds;
        double opacity;

        if (isExpired)
        {
            opacity = 0;
        }
        else if (elapsed <= _options.VisibleDurationMilliseconds)
        {
            opacity = 1;
        }
        else
        {
            var fadeProgress = (double)(elapsed - _options.VisibleDurationMilliseconds) /
                _options.FadeDurationMilliseconds;
            opacity = 1 - SmoothStep(Math.Clamp(fadeProgress, 0, 1));
        }

        return new AnnotationLifetimeSnapshot(
            group.Id,
            group.StrokeIds.ToArray(),
            opacity,
            isExpired);
    }

    private static double SmoothStep(double value) => value * value * (3 - (2 * value));

    private sealed class TemporaryGroup
    {
        public TemporaryGroup(Guid id, long lastUpdatedMilliseconds)
        {
            Id = id;
            LastUpdatedMilliseconds = lastUpdatedMilliseconds;
        }

        public Guid Id { get; }

        public List<Guid> StrokeIds { get; } = [];

        public long LastUpdatedMilliseconds { get; set; }
    }
}
