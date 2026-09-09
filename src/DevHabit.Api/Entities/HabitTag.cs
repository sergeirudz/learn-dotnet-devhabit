namespace DevHabit.Api.Entities;

// Join Entity for many-to-many relationship between Habit and Tag
public sealed class HabitTag
{
    public string HabitId { get; set; }
    public string TagId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
