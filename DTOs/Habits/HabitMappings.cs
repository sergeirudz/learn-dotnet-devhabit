using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Habits;

internal static class HabitMappings
{
    // This will allow to turn entity into DTO
    // Entity -> DTO
    public static HabitDto ToDto(this Habit habit)
    {
        return new HabitDto
        {
            Id = habit.Id,
            Name = habit.Name,
            Description = habit.Description,
            Type = habit.Type,
            Frequency = new FrequencyDto
            {
                Type = habit.Frequency.Type,
                TimesPerPeriod = habit.Frequency.TimesPerPeriod
            },
            Target = new TargetDto
            {
                Value = habit.Target.Value,
                Unit = habit.Target.Unit
            },
            Status = habit.Status,
            IsArchived = habit.IsArchived,
            EndDate = habit.EndDate,
            Milestone = habit.Milestone == null
                ? null
                : new MilestoneDto
                {
                    Target = habit.Milestone.Target,
                    Current = habit.Milestone.Current
                },
            CreatedAtUtc = habit.CreatedAtUtc,
            UpdatedAtUtc = habit.UpdatedAtUtc,
            LastCompletedAtUtc = habit.LastCompletedAtUtc
        };
    }

    // This will allow to set initial fields here
    // DTO -> Entity
    public static Habit ToEntity(this CreateHabitDto dto)
    {
        Habit habit = new()
        {
            Id = $"h_{Guid.CreateVersion7()}",
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            Frequency = new Frequency
            {
                Type = dto.Frequency.Type,
                TimesPerPeriod = dto.Frequency.TimesPerPeriod
            },
            Target = new Target
            {
                Value = dto.Target.Value,
                Unit = dto.Target.Unit
            },
            Status = HabitStatus.Ongoing,
            IsArchived = false,
            EndDate = dto.EndDate,
            Milestone = dto.Milestone is not null
                ? new Milestone
                {
                    Target = dto.Milestone.Target,
                    Current = 0 // Initialize current progress to 0
                }
                : null,
            CreatedAtUtc = DateTime.UtcNow
        };

        return habit;
    }

    // This will allow to update an existing entity from a DTO, and DRY code
    public static void UpdateFromDto(this Habit habit, UpdateHabitDto dto)
    {
        // basic fields
        habit.Name = dto.Name;
        habit.Description = dto.Description;
        habit.Type = dto.Type;
        habit.EndDate = dto.EndDate;

        // nested fields. Assuming immutable
        // habit.Frequency.Type = dto.Frequency.Type;
        // habit.Frequency.TimesPerPeriod = dto.Frequency.TimesPerPeriod;

        habit.Frequency = new Frequency
        {
            Type = dto.Frequency.Type,
            TimesPerPeriod = dto.Frequency.TimesPerPeriod
        };

        // target
        habit.Target = new Target
        {
            Value = dto.Target.Value,
            Unit = dto.Target.Unit
        };


        // habit.IsArchive d = dto.IsArchived;

        if (dto.Milestone is not null)
        {
            if (habit.Milestone is null)
            {
                habit.Milestone = new Milestone
                {
                    Target = dto.Milestone.Target,
                    Current = 0 // Explicitly set Current to 0 for new milestones
                };
            }
            else
            {
                habit.Milestone.Target = dto.Milestone.Target;
                // Current is preserved from existing milestone
            }
        }

        habit.UpdatedAtUtc = DateTime.UtcNow;
    }
}
