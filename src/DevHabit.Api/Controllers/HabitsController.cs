using System.Dynamic;
using System.Net.Mime;
using Asp.Versioning;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("habits")]
// [Route("v.{version:apiVersion}/habits")]
[ApiVersion(1.0)] // default
// [ApiVersion(2.0)]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.JsonV2,
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.HateoasJsonV1,
    CustomMediaTypeNames.Application.HateoasJsonV2
)] // can be set here instead of inside API
public sealed class HabitsController(
    ApplicationDbContext dbContext,
    LinkService linkService
) : ControllerBase
{
    //! GET: habits
    [HttpGet]
    // Use Produces custom media types then we can omit formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJson);
    // From DependencyInjection.cs globally supported media type
    // [Produces(MediaTypeNames.Application.Json, CustomMediaTypeNames.Application.HateoasJson)]
    public async Task<IActionResult> GetHabits(
        // http://localhost:6000/habits?q=keyword
        [FromQuery] HabitsQueryParameters query,
        SortMappingProvider sortMappingProvider,
        DataShapingService dataShapingService
    )
    {
        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter isn't valid: '{query.Sort}'");
        }

        // this will validate if the data shaping fields are valid
        if (!dataShapingService.Validate<HabitDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided fields parameter isn't valid: '{query.Fields}'");
        }

        // Normalize search query: trim whitespace and convert to lowercase for case-insensitive comparison
        string? searchQuery = query.Search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        // IQueryable<Habit> query = dbContext.Habits;

        IQueryable<HabitDto> habitsQuery = dbContext
            .Habits.Where(h => searchQuery == null ||
                               h.Name.ToLower().Contains(searchQuery) ||
                               h.Description != null && h.Description.ToLower().Contains(searchQuery))
            .Where(h => query.Type == null || h.Type == query.Type)
            .Where(h => query.Status == null || h.Status == query.Status)
            .ApplySort(query.Sort, sortMappings)
            .Select(HabitQueries.ProjectToDto());


        int totalCount = await habitsQuery.CountAsync();
        List<HabitDto> habits = await habitsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize).ToListAsync();


        // This is needed if HATEOAS links are requested
        // Accept application/vnd.dev-habit.hateoas+json
        bool includeLinks = query.Accept == CustomMediaTypeNames.Application.HateoasJson;

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            // CreateLinksForHabit will allow to include HATEOAS links for data shaping responses
            Items = dataShapingService.ShapeCollectionData(
                habits,
                query.Fields,
                includeLinks ? h => CreateLinksForHabit(h.Id, query.Fields) : null
            ),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };


        if (includeLinks)
        {
            paginationResult.Links = CreateLinksForHabits(
                query,
                paginationResult.HasNextPage,
                paginationResult.HasPreviousPage);
        }


        return Ok(paginationResult);
    }

    //! GET: habits/{id}
    [HttpGet("{id}")]
    [MapToApiVersion(1.0)]
    public async Task<IActionResult> GetHabit(
        string id,
        string? fields,
        [FromQuery] HabitQueryParameters query,
        DataShapingService dataShapingService
        // [FromHeader(Name = "Accept")] string? accept
    )
    {
        if (!dataShapingService.Validate<HabitDto>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided fields parameter isn't valid: '{fields}'");
        }


        HabitWithTagsDto? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTags())
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, fields);

        if (query.IncludeLinks)
        {
            List<LinkDto> links = CreateLinksForHabit(id, query.Fields);
            shapedHabitDto.TryAdd("links", links);
        }

        return Ok(shapedHabitDto);
    }


    /// <summary>
    ///     Retrieves a specific habit by ID with version 2 of the API
    /// </summary>
    /// <param name="id">The habit ID</param>
    /// <param name="query">Query parameters for data shaping</param>
    /// <param name="dataShapingService">Service for data shaping</param>
    /// <returns>The requested habit</returns>
    [HttpGet("{id}")]
    [ApiVersion(2.0)] // Asp.Versioning;
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHabitV2(
        string id,
        [FromQuery] HabitQueryParameters query,
        DataShapingService dataShapingService)
    {
        if (!dataShapingService.Validate<HabitWithTagsDtoV2>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields aren't valid: '{query.Fields}'");
        }

        HabitWithTagsDtoV2? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTagsV2())
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, query.Fields);

        if (query.IncludeLinks)
        {
            ((IDictionary<string, object?>)shapedHabitDto)[nameof(ILinksResponse.Links)] =
                CreateLinksForHabit(id, query.Fields);
        }

        return Ok(shapedHabitDto);
    }


    //! POST: habits
    [HttpPost]
    // Use Consumes to define media types for different API requests that you want to support on API
    // [Consumes(CustomMediaTypeNames.Application.HateoasJson)]
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
        habitDto.Links = CreateLinksForHabit(habitDto.Id, null);

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


    private List<LinkDto> CreateLinksForHabits(
        HabitsQueryParameters parameters,
        bool hasNextPage,
        bool hasPreviousPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabits), "self", HttpMethods.Get, new
            {
                page = parameters.Page,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status
            }),
            linkService.Create(nameof(CreateHabit), "create", HttpMethods.Post)
        ];

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetHabits), "next-page", HttpMethods.Get, new
            {
                page = parameters.Page + 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status
            }));
        }

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetHabits), "previous-page", HttpMethods.Get, new
            {
                page = parameters.Page - 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status
            }));
        }

        return links;
    }


    private List<LinkDto> CreateLinksForHabit(string id, string? fields)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabit), "self", HttpMethods.Get, new { id, fields }),
            linkService.Create(nameof(UpdateHabit), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(PatchHabit), "patch", HttpMethods.Patch, new { id }),
            linkService.Create(nameof(DeleteHabit), "delete", HttpMethods.Delete, new { id }),
            linkService.Create(
                nameof(HabitTagsController.UpsertHabitTags),
                "upsert-tags",
                HttpMethods.Put, new { habitId = id },
                HabitTagsController.Name
            )
        ];

        return links;
    }
}
