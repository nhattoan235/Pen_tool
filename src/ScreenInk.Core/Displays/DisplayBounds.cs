namespace ScreenInk.Core.Displays;

public readonly record struct DisplayBounds(int X, int Y, int Width, int Height)
{
    public int Right => checked(X + Width);

    public int Bottom => checked(Y + Height);
}
