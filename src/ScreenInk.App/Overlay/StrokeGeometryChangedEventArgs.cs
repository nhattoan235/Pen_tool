using System.Windows.Media;

namespace ScreenInk.App.Overlay;

internal sealed record StrokeGeometryChange(
    Guid StrokeId,
    Geometry Before,
    Geometry After,
    Geometry? InkCoreBefore = null,
    Geometry? InkCoreAfter = null);

internal sealed class StrokeGeometryChangedEventArgs(
    IReadOnlyList<StrokeGeometryChange> changes,
    string description = "geometry change") : EventArgs
{
    public IReadOnlyList<StrokeGeometryChange> Changes { get; } = changes;

    public string Description { get; } = description;
}
