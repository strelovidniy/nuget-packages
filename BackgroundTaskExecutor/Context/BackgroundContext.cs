using BackgroundTaskExecutor.Entities;
using BackgroundTaskExecutor.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace BackgroundTaskExecutor.Context;

internal class BackgroundContext(DbContextOptions<BackgroundContext> options) : DbContext(options)
{
    public DbSet<SyncEntry> SyncEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SyncEntryConfiguration());
    }
}