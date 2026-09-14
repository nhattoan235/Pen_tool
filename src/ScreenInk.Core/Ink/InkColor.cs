namespace ScreenInk.Core.Ink;

public readonly record struct InkColor(byte Red, byte Green, byte Blue)
{
    public string Hex => $"#{Red:X2}{Green:X2}{Blue:X2}";
}
