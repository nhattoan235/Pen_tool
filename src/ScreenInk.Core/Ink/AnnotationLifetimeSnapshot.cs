namespace ScreenInk.Core.Ink;

public sealed record AnnotationLifetimeSnapshot(
    Guid GroupId,
    IReadOnlyList<Guid> StrokeIds,
    double Opacity,
    bool IsExpired);
