namespace ScreenInk.Core.Ink;

public sealed class StrokePointBuffer
{
    private readonly StrokeSmoothingOptions _options;
    private readonly List<StrokePoint> _points = [];
    private readonly List<StrokePoint> _rawPoints = [];
    private StrokePoint? _lastRawPoint;
    private bool _completed;

    public StrokePointBuffer(StrokeSmoothingOptions? options = null)
    {
        _options = options ?? new StrokeSmoothingOptions();
        _options.Validate();
    }

    public IReadOnlyList<StrokePoint> Points => _points;

    public IReadOnlyList<StrokePoint> RawPoints => _rawPoints;

    public IReadOnlyList<StrokePoint> GetRenderablePoints()
    {
        if (_completed || _lastRawPoint is null || _points.Count == 0)
        {
            return _points;
        }

        var rawPoint = _lastRawPoint.Value;
        if (_points[^1].DistanceTo(rawPoint) < 0.1)
        {
            return _points;
        }

        var renderPoints = new StrokePoint[_points.Count + 1];
        _points.CopyTo(renderPoints, 0);
        renderPoints[^1] = rawPoint;
        return renderPoints;
    }

    public bool Add(StrokePoint rawPoint)
    {
        if (_completed)
        {
            throw new InvalidOperationException("A completed stroke cannot accept more points.");
        }

        _rawPoints.Add(rawPoint);

        if (_lastRawPoint is null)
        {
            _lastRawPoint = rawPoint;
            _points.Add(rawPoint);
            return true;
        }

        var previousRaw = _lastRawPoint.Value;
        var rawDistance = previousRaw.DistanceTo(rawPoint);
        if (rawDistance < _options.MinimumDistance)
        {
            return false;
        }

        var elapsedMilliseconds = Math.Max(1, rawPoint.TimestampMilliseconds - previousRaw.TimestampMilliseconds);
        var speed = rawDistance / elapsedMilliseconds;
        var speedRatio = Math.Clamp(speed / _options.FastMovementSpeed, 0, 1);
        var alpha = Lerp(_options.SlowMovementAlpha, _options.FastMovementAlpha, speedRatio);

        var previousFiltered = _points[^1];
        var filteredPoint = new StrokePoint(
            Lerp(previousFiltered.X, rawPoint.X, alpha),
            Lerp(previousFiltered.Y, rawPoint.Y, alpha),
            rawPoint.TimestampMilliseconds);

        _lastRawPoint = rawPoint;
        _points.Add(filteredPoint);
        return true;
    }

    public void Complete(StrokePoint finalPoint)
    {
        if (_completed)
        {
            return;
        }

        // Mouse-up is a distinct input sample even when it shares the last move position.
        _rawPoints.Add(finalPoint);

        if (_points.Count == 0)
        {
            _points.Add(finalPoint);
        }
        else
        {
            var lastFiltered = _points[^1];
            if (lastFiltered.DistanceTo(finalPoint) >= 0.1)
            {
                _points.Add(finalPoint);
            }
            else
            {
                _points[^1] = finalPoint;
            }
        }

        _lastRawPoint = finalPoint;
        _completed = true;
    }

    private static double Lerp(double from, double to, double amount)
    {
        return from + ((to - from) * amount);
    }
}
