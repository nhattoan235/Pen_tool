namespace ScreenInk.Core.Ink;

public sealed record StrokeSmoothingOptions
{
    public double MinimumDistance { get; init; } = 0.65;

    // Mouse input needs more response than a pressure stylus: retain enough
    // damping for low-speed hand jitter without visibly dragging behind.
    public double SlowMovementAlpha { get; init; } = 0.30;

    public double FastMovementAlpha { get; init; } = 0.85;

    // Pixels per millisecond. The measured mouse corpus has a 0.25 px/ms
    // median and a 1.15 px/ms p95, so 0.65 reaches the responsive branch
    // during ordinary deliberate strokes instead of only during flicks.
    public double FastMovementSpeed { get; init; } = 0.65;

    public void Validate()
    {
        if (MinimumDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumDistance));
        }

        if (SlowMovementAlpha is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(SlowMovementAlpha));
        }

        if (FastMovementAlpha is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(FastMovementAlpha));
        }

        if (FastMovementAlpha < SlowMovementAlpha)
        {
            throw new ArgumentException("Fast movement alpha must be greater than or equal to slow movement alpha.");
        }

        if (FastMovementSpeed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(FastMovementSpeed));
        }
    }
}
