namespace ScreenInk.Core.Interaction;

public sealed class InteractionModeChangedEventArgs : EventArgs
{
    public InteractionModeChangedEventArgs(InteractionMode previous, InteractionMode current)
    {
        Previous = previous;
        Current = current;
    }

    public InteractionMode Previous { get; }

    public InteractionMode Current { get; }
}
