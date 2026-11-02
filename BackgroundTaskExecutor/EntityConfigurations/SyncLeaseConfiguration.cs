using BackgroundTaskExecutor.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackgroundTaskExecutor.EntityConfigurations;

// Every name is stated, as SyncEntries' are: a naming convention the host applies to its own context must not rename
// a table its migration created under these names.
internal class SyncLeaseConfiguration : IEntityTypeConfiguration<SyncLease>
{
    public void Configure(EntityTypeBuilder<SyncLease> builder)
    {
        builder.ToTable("SyncLeases");

        builder
            .HasKey(syncLease => syncLease.Id)
            .HasName("PK_SyncLeases");

        builder
            .Property(syncLease => syncLease.Id)
            .IsRequired()
            .ValueGeneratedNever()
            .HasColumnName("Id");

        builder
            .Property(syncLease => syncLease.TaskName)
            .HasMaxLength(400)
            .IsRequired()
            .HasColumnName("TaskName");

        builder
            .Property(syncLease => syncLease.Profile)
            .HasMaxLength(200)
            .IsRequired()
            .HasColumnName("Profile");

        builder
            .Property(syncLease => syncLease.Token)
            .HasColumnName("Token");

        builder
            .Property(syncLease => syncLease.Owner)
            .HasMaxLength(200)
            .HasColumnName("Owner");

        builder
            .Property(syncLease => syncLease.LeasedUntil)
            .HasColumnName("LeasedUntil");

        builder
            .Property(syncLease => syncLease.StartedAt)
            .HasColumnName("StartedAt");

        builder
            .Property(syncLease => syncLease.FinishedAt)
            .HasColumnName("FinishedAt");
    }
}
