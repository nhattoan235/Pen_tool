namespace ScreenInk.Core.Ink;

public sealed class ShapeRecognizer
{
    public ShapeRecognitionResult? Recognize(IReadOnlyList<StrokePoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
        {
            return null;
        }

        var bounds = GetBounds(points);
        var diagonal = Math.Sqrt((bounds.Width * bounds.Width) + (bounds.Height * bounds.Height));
        var totalLength = GetTotalLength(points);
        if (diagonal < 12 || totalLength < 12)
        {
            return null;
        }

        var start = ToInkPoint(points[0]);
        var end = ToInkPoint(points[^1]);
        var directDistance = start.DistanceTo(end);
        var lineRatio = directDistance / totalLength;
        var lineError = GetMaximumLineError(points, start, end);
        var allowedLineError = Math.Max(4, diagonal * 0.055);
        if (lineRatio >= 0.92 && lineError <= allowedLineError)
        {
            var confidence = Math.Clamp(
                ((lineRatio - 0.9) / 0.1) * (1 - (lineError / (allowedLineError * 1.5))),
                0,
                1);
            return new ShapeRecognitionResult(
                RecognizedShapeKind.Line,
                start,
                end,
                bounds,
                confidence);
        }

        if (TryRecognizeArrow(points, bounds, start) is { } arrow)
        {
            return arrow;
        }

        var closureDistance = directDistance;
        var isClosed = closureDistance <= Math.Max(20, totalLength * 0.12);
        if (!isClosed || points.Count < 8 || bounds.Width < 12 || bounds.Height < 12)
        {
            return null;
        }

        var rectangleScore = GetRectangleScore(points, bounds);
        if (rectangleScore >= 0.72)
        {
            return new ShapeRecognitionResult(
                RecognizedShapeKind.Rectangle,
                start,
                end,
                bounds,
                rectangleScore);
        }

        var ellipseScore = GetEllipseScore(points, bounds);
        if (ellipseScore >= 0.7)
        {
            return new ShapeRecognitionResult(
                RecognizedShapeKind.Ellipse,
                start,
                end,
                bounds,
                ellipseScore);
        }

        return null;
    }

    private static ShapeRecognitionResult? TryRecognizeArrow(
        IReadOnlyList<StrokePoint> points,
        ShapeBounds bounds,
        InkPoint start)
    {
        if (points.Count < 7)
        {
            return null;
        }

        var lastPossibleTip = points.Count - 4;
        var tipIndex = 1;
        var tipDistance = 0d;
        for (var index = 1; index <= lastPossibleTip; index++)
        {
            var distance = start.DistanceTo(ToInkPoint(points[index]));
            if (distance > tipDistance)
            {
                tipDistance = distance;
                tipIndex = index;
            }
        }

        if (tipIndex < Math.Max(2, points.Count / 4) || tipDistance < 28)
        {
            return null;
        }

        var tip = ToInkPoint(points[tipIndex]);
        var shaftPoints = points.Take(tipIndex + 1).ToArray();
        var shaftLength = GetTotalLength(shaftPoints);
        var shaftLineRatio = tipDistance / Math.Max(shaftLength, 0.001);
        var shaftLineError = GetMaximumLineError(shaftPoints, start, tip);
        var allowedShaftError = Math.Max(5, tipDistance * 0.07);
        if (shaftLineRatio < 0.88 || shaftLineError > allowedShaftError)
        {
            return null;
        }

        var minimumArmLength = Math.Max(10, tipDistance * 0.11);
        var maximumArmLength = tipDistance * 0.48;
        var maximumReturnDistance = Math.Max(8, tipDistance * 0.1);
        var returnIndex = -1;

        for (var candidate = tipIndex + 2; candidate <= points.Count - 2; candidate++)
        {
            var firstArmLength = 0d;
            for (var index = tipIndex + 1; index < candidate; index++)
            {
                firstArmLength = Math.Max(
                    firstArmLength,
                    tip.DistanceTo(ToInkPoint(points[index])));
            }

            if (firstArmLength >= minimumArmLength &&
                tip.DistanceTo(ToInkPoint(points[candidate])) <= maximumReturnDistance)
            {
                returnIndex = candidate;
                break;
            }
        }

        if (returnIndex < 0)
        {
            return null;
        }

        var firstWing = points
            .Skip(tipIndex + 1)
            .Take(returnIndex - tipIndex - 1)
            .Select(ToInkPoint)
            .MaxBy(point => tip.DistanceTo(point));
        var secondWing = points
            .Skip(returnIndex + 1)
            .Select(ToInkPoint)
            .MaxBy(point => tip.DistanceTo(point));

        var firstArmLengthFinal = tip.DistanceTo(firstWing);
        var secondArmLength = tip.DistanceTo(secondWing);
        if (firstArmLengthFinal < minimumArmLength || secondArmLength < minimumArmLength ||
            firstArmLengthFinal > maximumArmLength || secondArmLength > maximumArmLength)
        {
            return null;
        }

        var armRatio = Math.Min(firstArmLengthFinal, secondArmLength) /
            Math.Max(firstArmLengthFinal, secondArmLength);
        if (armRatio < 0.45)
        {
            return null;
        }

        var reverseX = (start.X - tip.X) / tipDistance;
        var reverseY = (start.Y - tip.Y) / tipDistance;
        var firstArmX = (firstWing.X - tip.X) / firstArmLengthFinal;
        var firstArmY = (firstWing.Y - tip.Y) / firstArmLengthFinal;
        var secondArmX = (secondWing.X - tip.X) / secondArmLength;
        var secondArmY = (secondWing.Y - tip.Y) / secondArmLength;
        var firstDot = (reverseX * firstArmX) + (reverseY * firstArmY);
        var secondDot = (reverseX * secondArmX) + (reverseY * secondArmY);
        var firstCross = (reverseX * firstArmY) - (reverseY * firstArmX);
        var secondCross = (reverseX * secondArmY) - (reverseY * secondArmX);

        if (firstDot is < 0.35 or > 0.97 || secondDot is < 0.35 or > 0.97 ||
            Math.Abs(firstCross) < 0.22 || Math.Abs(secondCross) < 0.22 ||
            firstCross * secondCross >= 0)
        {
            return null;
        }

        var straightness = Math.Clamp(
            (shaftLineRatio - 0.86) / 0.14,
            0,
            1);
        var symmetry = armRatio;
        var returnScore = 1 - Math.Clamp(
            tip.DistanceTo(ToInkPoint(points[returnIndex])) / maximumReturnDistance,
            0,
            1);
        var confidence = (straightness * 0.45) + (symmetry * 0.35) + (returnScore * 0.2);

        return new ShapeRecognitionResult(
            RecognizedShapeKind.Arrow,
            start,
            tip,
            bounds,
            confidence);
    }

    private static ShapeBounds GetBounds(IReadOnlyList<StrokePoint> points)
    {
        var minimumX = points.Min(point => point.X);
        var minimumY = points.Min(point => point.Y);
        var maximumX = points.Max(point => point.X);
        var maximumY = points.Max(point => point.Y);
        return new ShapeBounds(minimumX, minimumY, maximumX - minimumX, maximumY - minimumY);
    }

    private static double GetTotalLength(IReadOnlyList<StrokePoint> points)
    {
        var result = 0d;
        for (var index = 1; index < points.Count; index++)
        {
            result += points[index - 1].DistanceTo(points[index]);
        }

        return result;
    }

    private static double GetMaximumLineError(
        IReadOnlyList<StrokePoint> points,
        InkPoint start,
        InkPoint end)
    {
        var deltaX = end.X - start.X;
        var deltaY = end.Y - start.Y;
        var length = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        if (length <= double.Epsilon)
        {
            return double.MaxValue;
        }

        var maximum = 0d;
        foreach (var point in points)
        {
            var distance = Math.Abs(
                (deltaY * point.X) -
                (deltaX * point.Y) +
                (end.X * start.Y) -
                (end.Y * start.X)) / length;
            maximum = Math.Max(maximum, distance);
        }

        return maximum;
    }

    private static double GetRectangleScore(
        IReadOnlyList<StrokePoint> points,
        ShapeBounds bounds)
    {
        var edgeError = 0d;
        foreach (var point in points)
        {
            var distance = Math.Min(
                Math.Min(Math.Abs(point.X - bounds.X), Math.Abs(point.X - (bounds.X + bounds.Width))),
                Math.Min(Math.Abs(point.Y - bounds.Y), Math.Abs(point.Y - (bounds.Y + bounds.Height))));
            edgeError += distance;
        }

        var normalizedEdgeError = edgeError / points.Count / Math.Max(1, Math.Min(bounds.Width, bounds.Height));
        var corners = new[]
        {
            new InkPoint(bounds.X, bounds.Y),
            new InkPoint(bounds.X + bounds.Width, bounds.Y),
            new InkPoint(bounds.X + bounds.Width, bounds.Y + bounds.Height),
            new InkPoint(bounds.X, bounds.Y + bounds.Height),
        };
        var diagonal = Math.Sqrt((bounds.Width * bounds.Width) + (bounds.Height * bounds.Height));
        var allowedCornerDistance = Math.Max(15, diagonal * 0.16);
        var visitedCorners = corners.Count(corner =>
            points.Min(point => corner.DistanceTo(ToInkPoint(point))) <= allowedCornerDistance);
        var cornerScore = visitedCorners / 4d;
        var edgeScore = 1 - Math.Clamp(normalizedEdgeError / 0.12, 0, 1);
        return (edgeScore * 0.7) + (cornerScore * 0.3);
    }

    private static double GetEllipseScore(
        IReadOnlyList<StrokePoint> points,
        ShapeBounds bounds)
    {
        var centerX = bounds.X + (bounds.Width / 2);
        var centerY = bounds.Y + (bounds.Height / 2);
        var radiusX = bounds.Width / 2;
        var radiusY = bounds.Height / 2;
        var radialError = 0d;

        foreach (var point in points)
        {
            var normalizedX = (point.X - centerX) / radiusX;
            var normalizedY = (point.Y - centerY) / radiusY;
            radialError += Math.Abs(Math.Sqrt(
                (normalizedX * normalizedX) +
                (normalizedY * normalizedY)) - 1);
        }

        var averageError = radialError / points.Count;
        return 1 - Math.Clamp(averageError / 0.32, 0, 1);
    }

    private static InkPoint ToInkPoint(StrokePoint point) => new(point.X, point.Y);
}
