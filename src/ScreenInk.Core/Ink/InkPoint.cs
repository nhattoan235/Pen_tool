namespace ScreenInk.Core.Ink;

public readonly record struct InkPoint(double X, double Y)
{
    public double DistanceTo(InkPoint other)
    {
        var deltaX = other.X - X;
        var deltaY = other.Y - Y;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }
}
