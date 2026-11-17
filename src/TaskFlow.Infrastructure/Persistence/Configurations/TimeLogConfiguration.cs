using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class TimeLogConfiguration : IEntityTypeConfiguration<TimeLog>
{
    public void Configure(EntityTypeBuilder<TimeLog> builder)
    {
        builder.ToTable("TimeLogs");
        builder.HasKey(l => l.Id);

        builder.Ignore(l => l.IsRunning);

        builder.Property(l => l.UserId).IsRequired().HasMaxLength(450);
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.StartTime).IsRequired();
        builder.Property(l => l.DurationInMinutes).IsRequired();

        builder.HasIndex(l => new { l.UserId, l.StartTime });
        builder.HasIndex(l => l.TaskItemId);

        // Database-level guarantee: at most ONE running timer (EndTime IS NULL) per user,
        // even if two requests race each other.
        builder.HasIndex(l => l.UserId)
            .IsUnique()
            .HasFilter("\"EndTime\" IS NULL")
            .HasDatabaseName("UX_TimeLogs_UserId_ActiveTimer");
    }
}
