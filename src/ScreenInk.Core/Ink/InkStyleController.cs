namespace ScreenInk.Core.Ink;

public sealed class InkStyleController
{
    private static readonly InkColor[] Palette =
    [
        new(255, 74, 82),
        new(255, 190, 44),
        new(64, 145, 255),
        new(54, 202, 132),
        new(245, 245, 247),
    ];

    private int _paletteIndex;
    private readonly Dictionary<InkTool, int> _sizeIndices = new()
    {
        [InkTool.Pen] = 1,
        [InkTool.Highlighter] = 1,
        [InkTool.PixelEraser] = 1,
        [InkTool.ObjectEraser] = 1,
        [InkTool.Lasso] = 1,
    };
    private InkTool _currentTool = InkTool.Pen;
    private bool _isPersistent;

    private static readonly IReadOnlyDictionary<InkTool, double[]> SizePresets =
        new Dictionary<InkTool, double[]>
        {
            [InkTool.Pen] = [3, 5, 8],
            [InkTool.Highlighter] = [12, 20, 32],
            [InkTool.PixelEraser] = [12, 20, 32],
            [InkTool.ObjectEraser] = [12, 20, 32],
            [InkTool.Lasso] = [1, 1, 1],
        };

    public event EventHandler? StyleChanged;

    public int ColorCount => Palette.Length;

    public int CurrentColorIndex => _paletteIndex;

    public InkColor CurrentColor => Palette[_paletteIndex];

    public InkTool CurrentTool => _currentTool;

    public int CurrentSizeIndex => _sizeIndices[_currentTool];

    public double CurrentSize => SizePresets[_currentTool][CurrentSizeIndex];

    public double CurrentOpacity => _currentTool == InkTool.Highlighter ? 0.34 : 1;

    public bool IsPersistent => _isPersistent;

    public InkColor GetColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= Palette.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(colorIndex));
        }

        return Palette[colorIndex];
    }

    public void CycleColor()
    {
        SelectColor((_paletteIndex + 1) % Palette.Length);
    }

    public void SelectColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= Palette.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(colorIndex));
        }

        if (_paletteIndex == colorIndex)
        {
            return;
        }

        _paletteIndex = colorIndex;
        StyleChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SelectTool(InkTool tool)
    {
        if (!Enum.IsDefined(tool))
        {
            throw new ArgumentOutOfRangeException(nameof(tool));
        }

        if (_currentTool == tool)
        {
            return;
        }

        _currentTool = tool;
        StyleChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CycleTool(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        var tools = Enum.GetValues<InkTool>();
        var currentIndex = Array.IndexOf(tools, _currentTool);
        var offset = direction % tools.Length;
        var nextIndex = (currentIndex + offset + tools.Length) % tools.Length;
        SelectTool(tools[nextIndex]);
    }

    public void SelectSize(int sizeIndex)
    {
        SelectSize(_currentTool, sizeIndex);
    }

    public int GetSizeIndex(InkTool tool)
    {
        if (!Enum.IsDefined(tool))
        {
            throw new ArgumentOutOfRangeException(nameof(tool));
        }

        return _sizeIndices[tool];
    }

    public void SelectSize(InkTool tool, int sizeIndex)
    {
        if (!Enum.IsDefined(tool))
        {
            throw new ArgumentOutOfRangeException(nameof(tool));
        }

        var presets = SizePresets[tool];
        if (sizeIndex < 0 || sizeIndex >= presets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeIndex));
        }

        if (_sizeIndices[tool] == sizeIndex)
        {
            return;
        }

        _sizeIndices[tool] = sizeIndex;
        StyleChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AdjustSize(int direction)
    {
        if (direction == 0 || _currentTool == InkTool.Lasso)
        {
            return;
        }

        SelectSize(Math.Clamp(CurrentSizeIndex + direction, 0, 2));
    }

    public void SetPersistent(bool isPersistent)
    {
        if (_isPersistent == isPersistent)
        {
            return;
        }

        _isPersistent = isPersistent;
        StyleChanged?.Invoke(this, EventArgs.Empty);
    }
}
