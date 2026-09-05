using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
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
    public async Task<ActionResult<PaginationResult<HabitDto>>> GetHabits(
        // http://localhost:6000/habits?q=keyword
        [FromQuery] HabitsQueryParameters query,
        SortMappingProvider sortMappingProvider
    )
    {
        // Validate the sort parameter is correct
        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter isn't valid: '{query.Sort}'");
        }

        query.Search ??= query.Search?.Trim().ToLower();

        SortMapping[] sortMapping = sortMappingProvider.GetMappings<HabitDto, Habit>();

        IQueryable<HabitDto> habitsQuery = dbContext
            .Habits
            .Where(h => query.Search == null ||
                        h.Name.ToLower().Contains(query.Search) ||
                        h.Description != null && h.Description.ToLower().Contains(query.Search))
            .Where(h => query.Type == null || h.Type == query.Type)
            .Where(h => query.Status == null || h.Status == query.Status)
            .ApplySort(query.Sort, sortMapping)
            .Select(HabitQueries.ProjectToDto());

        Task<PaginationResult<HabitDto>> paginationResult =
            PaginationResult<HabitDto>.CreateAsync(habitsQuery, query.Page, query.PageSize);

        return Ok(paginationResult);
    }

    //! GET: habits/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsDto>> GetHabit(string id)
    {
        HabitWithTagsDto? habit = await dbContext
            .Habits
            .Select(HabitQueries.ProjectToHabitWithTagsDto())
            .FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        return Ok(habit);
    }


    //! POST: habits
    [HttpPost]
    public async Task<ActionResult> CreateHabit(
        [FromBody] CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator
    )
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

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

    //! DELETE: habits/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit(string id)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
            // return StatusCode(StatusCodes.Status410Gone); // Idempotent delete
        }

        dbContext.Habits.Remove(habit);
        await dbContext.SaveChangesAsync();

        return NoContent(); // 204 No Content
    }
}
