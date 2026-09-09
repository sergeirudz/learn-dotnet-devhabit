using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Api.Database.Configurations;

public sealed class HabitTagConfiguration : IEntityTypeConfiguration<HabitTag>
{
    // Allow EF to understand the many-to-many relationship 
    // Take string values and connect them to the Habit and Tag entities
    public void Configure(EntityTypeBuilder<HabitTag> builder)
    {
        // Composite key for many-to-many relationship
        builder.HasKey(ht => new { ht.HabitId, ht.TagId });

        // Already applied by the FK definition (Habit, Tag)
        builder.Property(ht => ht.HabitId).HasMaxLength(500);
        builder.Property(ht => ht.TagId).HasMaxLength(500);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(ht => ht.TagId);
        // Skip navigation property
        // Habit -> HabitTag -> Tag (skip to TAG directly)
        builder.HasOne<Habit>()
            .WithMany(h => h.HabitTags)
            .HasForeignKey(ht => ht.HabitId);
    }
}
