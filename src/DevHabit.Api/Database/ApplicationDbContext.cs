using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Habit> Habits { get; set; }
    public DbSet<Tag> Tags { get; set; }

    public DbSet<HabitTag> HabitTags { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<GitHubAccessToken> GitHubAccessTokens { get; set; }
    // public DbSet<Entry> Entries { get; set; }
    // public DbSet<EntryImportJob> EntryImportJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Application);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext)
            // this will pick up EF core config src/DevHabit.Api/Database/Configurations/HabitConfiguration.cs
            /*
             ApplyConfigurationsFromAssembly scans the entire assembly (DLL)
             that contains ApplicationDbContext and automatically discovers all
             classes that implement IEntityTypeConfiguration<TEntity>
             — like HabitConfiguration : IEntityTypeConfiguration<Habit>.
             */
            .Assembly);
    }
}
