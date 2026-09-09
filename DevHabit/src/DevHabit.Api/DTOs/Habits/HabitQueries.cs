using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Habits;

internal static class HabitQueries
{
    // Benefit of using EF Core Projection
    // Instead of pulling entire Habit and connected Tables into memory and then mapping to DTO,
    // we can use EF Core projection to only select the fields we need for the DTO.
    // Avoids unnecessary data transfer and improves performance.
    public static Expression<Func<Habit, HabitDto>> ProjectToDto()
    {
        return h => new HabitDto
        {
            Id = h.Id,
            Name = h.Name,
            Description = h.Description,
            Type = h.Type,
            Frequency = new FrequencyDto
            {
                Type = h.Frequency.Type,
                TimesPerPeriod = h.Frequency.TimesPerPeriod
            },
            Target = new TargetDto
            {
                Value = h.Target.Value,
                Unit = h.Target.Unit
            },
            Status = h.Status,
            IsArchived = h.IsArchived,
            EndDate = h.EndDate,
            Milestone = h.Milestone == null
                ? null
                : new MilestoneDto
                {
                    Target = h.Milestone.Target,
                    Current = h.Milestone.Current
                },
            CreatedAtUtc = h.CreatedAtUtc,
            UpdatedAtUtc = h.UpdatedAtUtc,
            LastCompletedAtUtc = h.LastCompletedAtUtc
        };
    }

    public static Expression<Func<Habit, HabitWithTagsDto>> ProjectToDtoWithTags()
    {
        return h => new HabitWithTagsDto
        {
            Id = h.Id,
            Name = h.Name,
            Description = h.Description,
            Type = h.Type,
            Frequency = new FrequencyDto
            {
                Type = h.Frequency.Type,
                TimesPerPeriod = h.Frequency.TimesPerPeriod
            },
            Target = new TargetDto
            {
                Value = h.Target.Value,
                Unit = h.Target.Unit
            },
            Status = h.Status,
            IsArchived = h.IsArchived,
            EndDate = h.EndDate,
            Milestone = h.Milestone == null
                ? null
                : new MilestoneDto
                {
                    Target = h.Milestone.Target,
                    Current = h.Milestone.Current
                },
            CreatedAtUtc = h.CreatedAtUtc,
            UpdatedAtUtc = h.UpdatedAtUtc,
            LastCompletedAtUtc = h.LastCompletedAtUtc,
            Tags = h.Tags.Select(t => t.Name).ToArray()
        };
    }

    public static Expression<Func<Habit, HabitWithTagsDtoV2>> ProjectToDtoWithTagsV2()
    {
        return h => new HabitWithTagsDtoV2
        {
            Id = h.Id,
            Name = h.Name,
            Description = h.Description,
            Type = h.Type,
            Frequency = new FrequencyDto
            {
                Type = h.Frequency.Type,
                TimesPerPeriod = h.Frequency.TimesPerPeriod
            },
            Target = new TargetDto
            {
                Value = h.Target.Value,
                Unit = h.Target.Unit
            },
            Status = h.Status,
            IsArchived = h.IsArchived,
            EndDate = h.EndDate,
            Milestone = h.Milestone == null
                ? null
                : new MilestoneDto
                {
                    Target = h.Milestone.Target,
                    Current = h.Milestone.Current
                },
            CreatedAt = h.CreatedAtUtc,
            UpdatedAt = h.UpdatedAtUtc,
            LastCompletedAt = h.LastCompletedAtUtc,
            Tags = h.Tags.Select(t => t.Name).ToArray()
        };
    }
}
