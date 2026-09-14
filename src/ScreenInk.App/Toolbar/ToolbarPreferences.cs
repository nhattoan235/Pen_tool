using ScreenInk.Core.Ink;

namespace ScreenInk.App.Toolbar;

internal sealed record ToolbarPreferences
{
    public static ToolbarPreferences Default { get; } = new();

    public int ColorIndex { get; init; }

    public ToolbarTheme Theme { get; init; } = ToolbarTheme.Dark;

    public InkTool Tool { get; init; } = InkTool.Pen;

    public int PenSizeIndex { get; init; } = 1;

    public int HighlighterSizeIndex { get; init; } = 1;

    public int PixelEraserSizeIndex { get; init; } = 1;

    public int ObjectEraserSizeIndex { get; init; } = 1;
}
