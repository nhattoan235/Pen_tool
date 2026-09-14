namespace ScreenInk.Core.Displays;

public sealed record DisplayDescriptor(
    string Id,
    DisplayBounds Bounds,
    bool IsPrimary);
