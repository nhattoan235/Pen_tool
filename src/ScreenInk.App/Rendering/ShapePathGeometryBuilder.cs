using System.Windows.Media;
using ScreenInk.Core.Ink;

namespace ScreenInk.App.Rendering;

internal static class ShapePathGeometryBuilder
{
    public static Geometry Create(ShapeRecognitionResult shape, double size)
    {
        ArgumentNullException.ThrowIfNull(shape);
        return shape.Kind switch
        {
            RecognizedShapeKind.Line => CreateLine(shape, size),
            RecognizedShapeKind.Arrow => CreateArrow(shape, size),
            RecognizedShapeKind.Ellipse => CreateRing(shape.Bounds, size, isEllipse: true),
            RecognizedShapeKind.Rectangle => CreateRing(shape.Bounds, size, isEllipse: false),
            _ => Geometry.Empty,
        };
    }

    private static Geometry CreateArrow(ShapeRecognitionResult shape, double size)
    {
        var start = new System.Windows.Point(shape.Start.X, shape.Start.Y);
        var tip = new System.Windows.Point(shape.End.X, shape.End.Y);
        var delta = tip - start;
        var length = delta.Length;
        if (length <= double.Epsilon)
        {
            return Geometry.Empty;
        }

        var direction = delta / length;
        var perpendicular = new System.Windows.Vector(-direction.Y, direction.X);
        var headLength = Math.Clamp(length * 0.24, 14, 42);
        var halfSpread = headLength * 0.52;
        var headBase = tip - (direction * headLength);
        var firstWing = headBase + (perpendicular * halfSpread);
        var secondWing = headBase - (perpendicular * halfSpread);
        var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, size)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };

        var shaft = new LineGeometry(start, tip).GetWidenedPathGeometry(pen);
        var firstArm = new LineGeometry(tip, firstWing).GetWidenedPathGeometry(pen);
        var secondArm = new LineGeometry(tip, secondWing).GetWidenedPathGeometry(pen);
        var withFirstArm = Geometry.Combine(
            shaft,
            firstArm,
            GeometryCombineMode.Union,
            transform: null);
        var arrow = Geometry.Combine(
            withFirstArm,
            secondArm,
            GeometryCombineMode.Union,
            transform: null);
        arrow.Freeze();
        return arrow;
    }

    private static Geometry CreateLine(ShapeRecognitionResult shape, double size)
    {
        var line = new LineGeometry(
            new System.Windows.Point(shape.Start.X, shape.Start.Y),
            new System.Windows.Point(shape.End.X, shape.End.Y));
        var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, size)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
        var geometry = line.GetWidenedPathGeometry(pen);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry CreateRing(ShapeBounds bounds, double size, bool isEllipse)
    {
        var halfSize = size / 2;
        var outerRect = new System.Windows.Rect(
            bounds.X - halfSize,
            bounds.Y - halfSize,
            bounds.Width + size,
            bounds.Height + size);
        var innerRect = new System.Windows.Rect(
            bounds.X + halfSize,
            bounds.Y + halfSize,
            Math.Max(0.1, bounds.Width - size),
            Math.Max(0.1, bounds.Height - size));

        Geometry outer = isEllipse
            ? new EllipseGeometry(outerRect)
            : new RectangleGeometry(outerRect, 4, 4);
        Geometry inner = isEllipse
            ? new EllipseGeometry(innerRect)
            : new RectangleGeometry(innerRect, Math.Max(0, 4 - halfSize), Math.Max(0, 4 - halfSize));
        var ring = Geometry.Combine(outer, inner, GeometryCombineMode.Exclude, transform: null);
        ring.Freeze();
        return ring;
    }
}
