namespace ScreenInk.Core.Ink;

public sealed class FreehandStrokeOutlineBuilder
{
    private const int DotSegmentCount = 16;
    private const int RoundCapSegmentCount = 6;

    private readonly FreehandStrokeOptions _options;

    public FreehandStrokeOutlineBuilder(FreehandStrokeOptions? options = null)
    {
        _options = options ?? new FreehandStrokeOptions();
        _options.Validate();
    }

    public IReadOnlyList<InkPoint> Build(IReadOnlyList<StrokePoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count == 0)
        {
            return [];
        }

        var totalLength = GetTotalLength(points);
        var dotThreshold = _options.DotThresholdLength > 0
            ? _options.DotThresholdLength
            : _options.Size;
        if (points.Count == 1 || totalLength < dotThreshold)
        {
            return BuildDot(points, _options.Size / 2);
        }

        var distances = new double[points.Count];
        for (var index = 1; index < points.Count; index++)
        {
            distances[index] = distances[index - 1] + points[index - 1].DistanceTo(points[index]);
        }

        var left = new List<InkPoint>(points.Count);
        var right = new List<InkPoint>(points.Count);
        var simulatedPressure = 0.5;
        var pooledCornerInk = 0d;
        var previousNormalX = 0d;
        var previousNormalY = -1d;

        for (var index = 0; index < points.Count; index++)
        {
            var previousIndex = Math.Max(0, index - 1);
            var nextIndex = Math.Min(points.Count - 1, index + 1);
            var previous = points[previousIndex];
            var current = points[index];
            var next = points[nextIndex];

            var tangentX = next.X - previous.X;
            var tangentY = next.Y - previous.Y;
            var tangentLength = Math.Sqrt((tangentX * tangentX) + (tangentY * tangentY));

            double normalX;
            double normalY;
            if (tangentLength <= double.Epsilon)
            {
                normalX = previousNormalX;
                normalY = previousNormalY;
            }
            else
            {
                normalX = -tangentY / tangentLength;
                normalY = tangentX / tangentLength;
                previousNormalX = normalX;
                previousNormalY = normalY;
            }

            if (index > 0)
            {
                var segmentDistance = points[index - 1].DistanceTo(current);
                var elapsed = Math.Max(1, current.TimestampMilliseconds - points[index - 1].TimestampMilliseconds);
                var speed = segmentDistance / elapsed;
                var speedRatio = Math.Max(0, speed / _options.MaximumSpeed);
                var shapedSpeed = Math.Pow(
                    _options.UseOpenEndedSpeedResponse
                        ? speedRatio
                        : Math.Clamp(speedRatio, 0, 1),
                    _options.SpeedResponseExponent);
                var normalizedSpeed = _options.UseOpenEndedSpeedResponse
                    ? shapedSpeed / (1 + shapedSpeed)
                    : shapedSpeed;
                var targetPressure = 1 - normalizedSpeed;
                simulatedPressure = Lerp(simulatedPressure, targetPressure, _options.PressureSmoothing);
            }

            var pressureScale = 1 + (_options.Thinning * (simulatedPressure - 0.5));
            var cornerScale = 1d;
            if (_options.CornerPoolingStrength > 0)
            {
                var turnAmount = GetTurnAmount(points, index);
                var slowMotionWeight = 0.25 + (0.75 * simulatedPressure);
                var targetCornerInk = turnAmount * slowMotionWeight;
                pooledCornerInk = Lerp(
                    pooledCornerInk,
                    targetCornerInk,
                    _options.CornerPoolingSmoothing);
                cornerScale += _options.CornerPoolingStrength * pooledCornerInk;
            }

            var variationScale = 1d;
            if (_options.CoverageVariationStrength > 0)
            {
                var phase = (points[0].X * 0.071) + (points[0].Y * 0.113);
                var distancePhase = (distances[index] / _options.CoverageVariationWavelength) *
                    Math.PI * 2;
                var variation = (0.65 * Math.Sin(distancePhase + phase)) +
                    (0.35 * Math.Sin((distancePhase / 1.7) + (phase * 0.61)));
                variationScale += _options.CoverageVariationStrength * variation;
            }

            var startTaper = _options.StartTaperLength <= 0
                ? 1
                : SmoothStep(Math.Clamp(distances[index] / _options.StartTaperLength, 0, 1));
            var endDistance = totalLength - distances[index];
            var endTaper = _options.EndTaperLength <= 0
                ? 1
                : SmoothStep(Math.Clamp(endDistance / _options.EndTaperLength, 0, 1));
            var taper = Math.Max(0.16, Math.Min(startTaper, endTaper));
            var radius = Math.Max(
                0.45,
                (_options.Size / 2) * pressureScale * taper * cornerScale * variationScale);

            left.Add(new InkPoint(
                current.X + (normalX * radius),
                current.Y + (normalY * radius)));
            right.Add(new InkPoint(
                current.X - (normalX * radius),
                current.Y - (normalY * radius)));
        }

        var outline = new List<InkPoint>(left.Count + right.Count);
        outline.AddRange(left);
        AppendRoundCap(outline, points[^1], left[^1]);

        for (var index = right.Count - 1; index >= 0; index--)
        {
            outline.Add(right[index]);
        }

        AppendRoundCap(outline, points[0], right[0]);

        return outline;
    }

    private static void AppendRoundCap(
        ICollection<InkPoint> outline,
        StrokePoint center,
        InkPoint capStart)
    {
        var radiusX = capStart.X - center.X;
        var radiusY = capStart.Y - center.Y;
        var radius = Math.Sqrt((radiusX * radiusX) + (radiusY * radiusY));
        if (radius <= double.Epsilon)
        {
            return;
        }

        var startAngle = Math.Atan2(radiusY, radiusX);
        for (var step = 1; step < RoundCapSegmentCount; step++)
        {
            var angle = startAngle - ((Math.PI * step) / RoundCapSegmentCount);
            outline.Add(new InkPoint(
                center.X + (Math.Cos(angle) * radius),
                center.Y + (Math.Sin(angle) * radius)));
        }
    }

    private static double GetTotalLength(IReadOnlyList<StrokePoint> points)
    {
        var length = 0d;
        for (var index = 1; index < points.Count; index++)
        {
            length += points[index - 1].DistanceTo(points[index]);
        }

        return length;
    }

    private static double GetTurnAmount(IReadOnlyList<StrokePoint> points, int index)
    {
        if (index <= 0 || index >= points.Count - 1)
        {
            return 0;
        }

        var incomingX = points[index].X - points[index - 1].X;
        var incomingY = points[index].Y - points[index - 1].Y;
        var outgoingX = points[index + 1].X - points[index].X;
        var outgoingY = points[index + 1].Y - points[index].Y;
        var incomingLength = Math.Sqrt((incomingX * incomingX) + (incomingY * incomingY));
        var outgoingLength = Math.Sqrt((outgoingX * outgoingX) + (outgoingY * outgoingY));
        if (incomingLength <= double.Epsilon || outgoingLength <= double.Epsilon)
        {
            return 0;
        }

        var dot = ((incomingX * outgoingX) + (incomingY * outgoingY)) /
            (incomingLength * outgoingLength);
        var normalizedAngle = Math.Acos(Math.Clamp(dot, -1, 1)) / Math.PI;
        return SmoothStep(normalizedAngle);
    }

    private static IReadOnlyList<InkPoint> BuildDot(IReadOnlyList<StrokePoint> points, double radius)
    {
        var first = points[0];
        var last = points[^1];
        var centerX = (first.X + last.X) / 2;
        var centerY = (first.Y + last.Y) / 2;
        var result = new InkPoint[DotSegmentCount];

        for (var index = 0; index < DotSegmentCount; index++)
        {
            var angle = (Math.PI * 2 * index) / DotSegmentCount;
            result[index] = new InkPoint(
                centerX + (Math.Cos(angle) * radius),
                centerY + (Math.Sin(angle) * radius));
        }

        return result;
    }

    private static double SmoothStep(double value) => value * value * (3 - (2 * value));

    private static double Lerp(double from, double to, double amount)
    {
        return from + ((to - from) * amount);
    }
}
