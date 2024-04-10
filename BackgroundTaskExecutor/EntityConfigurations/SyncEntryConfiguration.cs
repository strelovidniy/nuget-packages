using BackgroundTaskExecutor.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackgroundTaskExecutor.EntityConfigurations;

internal class SyncEntryConfiguration : IEntityTypeConfiguration<SyncEntry>
{
    public void Configure(EntityTypeBuilder<SyncEntry> builder)
    {
        builder
            .HasKey(syncEntry => syncEntry.Id);

        builder
            .Property(syncEntry => syncEntry.Id)
            .IsRequired();

        builder
            .Property(syncEntry => syncEntry.MachineName)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(syncEntry => syncEntry.Profile)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(syncEntry => syncEntry.TaskName)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(syncEntry => syncEntry.LastRun)
            .IsRequired();
    }
}