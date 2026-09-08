using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Api.Database.Configuration;

public sealed class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        // Good alternative to using data annotations on the entity classes themselves
        // [MaxLength(500)]
        // public string Name { get; set; }
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasMaxLength(500);
        builder.Property(h => h.UserId).HasMaxLength(500);
        builder.Property(h => h.Name).HasMaxLength(500);
        builder.Property(h => h.Description).HasMaxLength(500);

        // Configure owned types
        builder.OwnsOne(h => h.Frequency);
        builder.OwnsOne(h => h.Target, targetBuilder =>
        {
            targetBuilder.Property(t => t.Unit).HasMaxLength(100);
        });
        // Habit owns ONE type of Milestone
        builder.OwnsOne(h => h.Milestone);

        builder.HasMany(h => h.Tags)
            .WithMany()
            .UsingEntity<HabitTag>();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(h => h.UserId);
    }
}
