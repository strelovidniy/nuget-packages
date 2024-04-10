namespace BackgroundTaskExecutor.Settings;

internal class ExecutorSettings : CoreExecutorSettings, IExecutorSettings
{
    public Dictionary<string, CoreExecutorSettings> Profiles { get; set; } = [];
}