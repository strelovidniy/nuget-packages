namespace BackgroundTaskExecutor.Settings;

internal interface IExecutorSettings
{
    public Dictionary<string, CoreExecutorSettings> Profiles { get; set; }
}