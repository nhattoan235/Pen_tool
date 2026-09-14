namespace ScreenInk.Core.Ink;

public sealed record FreehandStrokeOptions
{
    public double Size { get; init; } = 5;

    public double Thinning { get; init; } = 0.48;

    public double StartTaperLength { get; init; } = 0;

    public double EndTaperLength { get; init; } = 0;

    public double MaximumSpeed { get; init; } = 0.85;

    public double SpeedResponseExponent { get; init; } = 1;

    public bool UseOpenEndedSpeedResponse { get; init; }

    public double PressureSmoothing { get; init; } = 0.36;

    public double CornerPoolingStrength { get; init; }

    public double CornerPoolingSmoothing { get; init; } = 0.45;

    public double DotThresholdLength { get; init; }

    public double CoverageVariationStrength { get; init; }

    public double CoverageVariationWavelength { get; init; } = 18;

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

        if (SpeedResponseExponent is < 0.25 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(SpeedResponseExponent));
        }

        if (PressureSmoothing is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(PressureSmoothing));
        }

        if (CornerPoolingStrength is < 0 or > 0.25)
        {
            throw new ArgumentOutOfRangeException(nameof(CornerPoolingStrength));
        }

        if (CornerPoolingSmoothing is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(CornerPoolingSmoothing));
        }

        if (DotThresholdLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DotThresholdLength));
        }

        if (CoverageVariationStrength is < 0 or > 0.05)
        {
            throw new ArgumentOutOfRangeException(nameof(CoverageVariationStrength));
        }

        if (CoverageVariationWavelength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CoverageVariationWavelength));
        }
    }
}
