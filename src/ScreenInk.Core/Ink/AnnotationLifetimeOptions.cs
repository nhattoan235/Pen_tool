namespace ScreenInk.Core.Ink;

public sealed record AnnotationLifetimeOptions
{
    public long GroupingWindowMilliseconds { get; init; } = 400;

    public long VisibleDurationMilliseconds { get; init; } = 1_650;

    public long FadeDurationMilliseconds { get; init; } = 350;

    public long TotalDurationMilliseconds => checked(VisibleDurationMilliseconds + FadeDurationMilliseconds);

    public void Validate()
    {
        if (GroupingWindowMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(GroupingWindowMilliseconds));
        }

        if (VisibleDurationMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(VisibleDurationMilliseconds));
        }

        if (FadeDurationMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(FadeDurationMilliseconds));
        }
    }
}
