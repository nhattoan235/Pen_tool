using ScreenInk.Core.Ink;

namespace ScreenInk.App.Diagnostics;

internal sealed record StrokeQualitySample(
    Guid StrokeId,
    DateTimeOffset CapturedAtUtc,
    string RendererCandidate,
    string DisplayId,
    InkTool Tool,
    double Size,
    double DpiScaleX,
    double DpiScaleY,
    bool IsPersistent,
    long DurationMilliseconds,
    double ClosureDistance,
    int GeometryBuildCount,
    double GeometryBuildTotalMilliseconds,
    double GeometryBuildMaximumMilliseconds,
    StrokeQualityBounds GeometryBounds,
    IReadOnlyList<StrokePoint> RawPoints,
    IReadOnlyList<StrokePoint> FilteredPoints);

internal readonly record struct StrokeQualityBounds(
    double X,
    double Y,
    double Width,
    double Height);
