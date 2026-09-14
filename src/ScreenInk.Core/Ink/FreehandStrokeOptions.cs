namespace ScreenInk.Core.Ink;

public sealed record FreehandStrokeOptions
{
    public double Size { get; init; } = 5;

    public double Thinning { get; init; } = 0.48;

    public double StartTaperLength { get; init; } = 0;

    public double EndTaperLength { get; init; } = 0;

    public double MaximumSpeed { get; init; } = 0.85;

    public double PressureSmoothing { get; init; } = 0.36;

    public void Validate()
    {
        if (Size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Size));
        }

        if (Thinning is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Thinning));
        }

        if (StartTaperLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StartTaperLength));
        }

        if (EndTaperLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(EndTaperLength));
        }

        if (MaximumSpeed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumSpeed));
        }

        if (PressureSmoothing is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(PressureSmoothing));
        }
    }
}
