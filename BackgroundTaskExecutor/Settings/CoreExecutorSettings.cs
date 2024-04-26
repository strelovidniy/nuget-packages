using BackgroundTaskExecutor.Constants;
using BackgroundTaskExecutor.Enums;

namespace BackgroundTaskExecutor.Settings;

internal class CoreExecutorSettings
{
    public double Interval { get; set; } = Defaults.DefaultInterval;

    public TimeUnit IntervalTimeUnit { get; set; } = Defaults.DefaultIntervalTimeUnit;

    public double FirstRunAfter { get; set; } = Defaults.DefaultFirstRunDelay;

    public TimeUnit FirstRunAfterTimeUnit { get; set; } = Defaults.DefaultFirstRunAfterTimeUnit;
}