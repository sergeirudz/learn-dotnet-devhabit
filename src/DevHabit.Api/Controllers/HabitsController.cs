using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("habits")]
public sealed class HabitsController(
    ApplicationDbContext dbContext
) : ControllerBase
{
    //! GET: habits
    [HttpGet]
    public async Task<ActionResult<HabitDto>> GetHabits()
    {
        List<HabitDto> habits = await dbContext
            .Habits
            .Select(HabitQueries.ProjectToDto())
            .ToListAsync();


        return Ok(habits);
    }

    //! GET: habits/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<HabitDto>> GetHabit(string id)
    {
        HabitDto? habit = await dbContext
            .Habits
            .Select(HabitQueries.ProjectToDto())
            .FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        return Ok(habit);
    }


    //! POST: habits
    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        [FromBody] CreateHabitDto createHabitDto)
    {
        Habit habit = createHabitDto.ToEntity();

        dbContext.Habits.Add(habit);

        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    //! PUT: habits/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id,
        [FromBody] UpdateHabitDto updateHabitDto // Model binding source. APS.NET will infer it from the body
    )
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromDto(updateHabitDto);
        await dbContext.SaveChangesAsync();

        return NoContent(); // 204 No Content
    }


    //! JSON PATCH: habits/{id}
    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id,
        [FromBody] JsonPatchDocument<HabitDto> patchDocument
    )
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null)
        {
            return NotFound();
        }

        HabitDto habitDto = habit.ToDto();

        // ModelState validation
        patchDocument.ApplyTo(habitDto, ModelState); 

        // Additional validation after applying the patch
        if (!TryValidateModel(habitDto))
        {
            return ValidationProblem(ModelState);
        }

        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent(); // 204 No Content
    }
}
