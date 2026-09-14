namespace ScreenInk.App.Overlay;

internal sealed class StrokeEraseRequestedEventArgs(Guid strokeId) : EventArgs
{
    public Guid StrokeId { get; } = strokeId;
}
