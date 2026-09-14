namespace ScreenInk.Core.Ink;

public sealed record ShapeRecognitionResult(
    RecognizedShapeKind Kind,
    InkPoint Start,
    InkPoint End,
    ShapeBounds Bounds,
    double Confidence);
