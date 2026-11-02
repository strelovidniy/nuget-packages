using BackgroundTaskExecutor.Constants;
using BackgroundTaskExecutor.Enums;

namespace BackgroundTaskExecutor.Settings;

internal class CoreExecutorSettings
{
    public double Interval { get; set; } = Defaults.DefaultInterval;

    public TimeUnit IntervalTimeUnit { get; set; } = Defaults.DefaultIntervalTimeUnit;

    public double FirstRunAfter { get; set; } = Defaults.DefaultFirstRunDelay;

    public TimeUnit FirstRunAfterTimeUnit { get; set; } = Defaults.DefaultFirstRunAfterTimeUnit;

    // How long a run holds its job without renewing. The run renews it every quarter of this while it works, so it
    // is how long a job waits for an instance that died mid-run, not how long a run may take.
    public double LeaseDuration { get; set; } = Defaults.DefaultLeaseDuration;

    public TimeUnit LeaseDurationTimeUnit { get; set; } = Defaults.DefaultLeaseDurationTimeUnit;
}
