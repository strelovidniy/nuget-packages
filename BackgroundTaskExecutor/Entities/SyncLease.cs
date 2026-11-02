using BackgroundTaskExecutor.Constants;

namespace BackgroundTaskExecutor.Entities;

/// <summary>
///     Who may run a job right now, shared by every instance of the application. One row per job and profile; an
///     instance runs the job only after taking the lease with one conditional update, keeps it alive while the job
///     runs, and gives it back when the job ends.
/// </summary>
internal class SyncLease
{
    public Guid Id { get; set; }

    public string TaskName { get; set; } = null!;

    public string Profile { get; set; } = Profiles.Default;

    public Guid? Token { get; set; }

    public string? Owner { get; set; }

    public DateTime? LeasedUntil { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }
}
