namespace ScreenInk.Core.Interaction;

public sealed class InteractionModeController
{
    public event EventHandler<InteractionModeChangedEventArgs>? ModeChanged;

    public InteractionMode Current { get; private set; } = InteractionMode.Pointer;

    public void ToggleDrawing()
    {
        SetMode(Current == InteractionMode.Draw ? InteractionMode.Pointer : InteractionMode.Draw);
    }

    public void ReturnToPointer()
    {
        SetMode(InteractionMode.Pointer);
    }

    public void SetMode(InteractionMode mode)
    {
        if (mode == Current)
        {
            return;
        }

        var previous = Current;
        Current = mode;
        ModeChanged?.Invoke(this, new InteractionModeChangedEventArgs(previous, mode));
    }
}
