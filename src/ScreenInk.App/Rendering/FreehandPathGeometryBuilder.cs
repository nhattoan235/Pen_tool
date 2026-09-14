using System.Windows.Media;
using ScreenInk.Core.Ink;

namespace ScreenInk.App.Rendering;

internal static class FreehandPathGeometryBuilder
{
    public static Geometry Create(IReadOnlyList<InkPoint> outline)
    {
        ArgumentNullException.ThrowIfNull(outline);

        if (outline.Count < 3)
        {
            return Geometry.Empty;
        }

        var geometry = new StreamGeometry
        {
            FillRule = FillRule.Nonzero,
        };

        using (var context = geometry.Open())
        {
            var start = Midpoint(outline[^1], outline[0]);
            context.BeginFigure(start, isFilled: true, isClosed: true);

            for (var index = 0; index < outline.Count; index++)
            {
                var current = outline[index];
                var next = outline[(index + 1) % outline.Count];

                context.QuadraticBezierTo(
                    ToPoint(current),
                    Midpoint(current, next),
                    isStroked: true,
                    isSmoothJoin: true);
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private static System.Windows.Point Midpoint(InkPoint first, InkPoint second) =>
        new((first.X + second.X) / 2, (first.Y + second.Y) / 2);

    private static System.Windows.Point ToPoint(InkPoint point) => new(point.X, point.Y);
}
