using BackgroundTaskExecutor.Entities;
using BackgroundTaskExecutor.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BackgroundTaskExecutor.Context;

internal class BackgroundContext(DbContextOptions<BackgroundContext> options) : DbContext(options)
{
    public DbSet<SyncEntry> SyncEntries { get; set; } = null!;

    public DbSet<SyncLease> SyncLeases { get; set; } = null!;

    // Two instances starting together both create a job's lease row and one loses on the key, which is expected and
    // handled; EF would log it as an error. Every failure here still reaches the executor's own log with its cause.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ConfigureWarnings(warnings => warnings.Log(
            (CoreEventId.SaveChangesFailed, LogLevel.Debug),
            (RelationalEventId.CommandError, LogLevel.Debug)
        ));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SyncEntryConfiguration());
        modelBuilder.ApplyConfiguration(new SyncLeaseConfiguration());
    }
}
