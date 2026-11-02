using BackgroundTaskExecutor.Enums;

namespace BackgroundTaskExecutor.Constants;

internal static class Defaults
{
    public const double DefaultFirstRunDelay = 0;
    public const double DefaultInterval = 5;
    public const double DefaultLeaseDuration = 2;

    public const TimeUnit DefaultFirstRunAfterTimeUnit = TimeUnit.Minute;
    public const TimeUnit DefaultIntervalTimeUnit = TimeUnit.Minute;
    public const TimeUnit DefaultLeaseDurationTimeUnit = TimeUnit.Minute;
}
