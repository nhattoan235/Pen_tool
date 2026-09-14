namespace ScreenInk.Core.Ink;

public readonly record struct StrokePoint(
    double X,
    double Y,
    long TimestampMilliseconds)
{
    public double DistanceTo(StrokePoint other)
    {
        var deltaX = other.X - X;
        var deltaY = other.Y - Y;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }
}
