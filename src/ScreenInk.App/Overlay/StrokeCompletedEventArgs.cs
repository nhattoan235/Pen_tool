namespace ScreenInk.App.Overlay;

internal sealed class StrokeCompletedEventArgs : EventArgs
{
    public StrokeCompletedEventArgs(Guid strokeId, bool isPersistent)
    {
        StrokeId = strokeId;
        IsPersistent = isPersistent;
    }

    public Guid StrokeId { get; }

    public bool IsPersistent { get; }
}
