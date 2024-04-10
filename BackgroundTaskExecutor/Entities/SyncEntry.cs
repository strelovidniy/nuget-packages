using BackgroundTaskExecutor.Constants;

namespace BackgroundTaskExecutor.Entities;

internal class SyncEntry
{
    public Guid Id { get; set; }

    public string MachineName { get; set; } = null!;

    public string TaskName { get; set; } = null!;

    public DateTime LastRun { get; set; }

    public string Profile { get; set; } = Profiles.Default;
}