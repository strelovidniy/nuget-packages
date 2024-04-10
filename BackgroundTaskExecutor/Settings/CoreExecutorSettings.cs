using BackgroundTaskExecutor.Constants;

namespace BackgroundTaskExecutor.Settings;

internal class CoreExecutorSettings
{
    public int IntervalInMinutes { get; set; } = Defaults.DefaultInterval;

    public int FirstRunAfterInMinutes { get; set; } = Defaults.DefaultFirstRunDelay;
}