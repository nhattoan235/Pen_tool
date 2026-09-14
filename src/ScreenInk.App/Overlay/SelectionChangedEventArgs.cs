namespace ScreenInk.App.Overlay;

internal sealed class SelectionChangedEventArgs : EventArgs
{
    public SelectionChangedEventArgs(IReadOnlyCollection<Guid> strokeIds)
    {
        StrokeIds = strokeIds ?? throw new ArgumentNullException(nameof(strokeIds));
    }

    public IReadOnlyCollection<Guid> StrokeIds { get; }
}
