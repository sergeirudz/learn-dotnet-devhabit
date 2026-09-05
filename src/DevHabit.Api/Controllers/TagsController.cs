using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("tags")]
public sealed class TagsController(
    ApplicationDbContext dbContext
) : ControllerBase
{
    //! GET: tags
    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags(
        // [FromHeader(Name = "Accept")] AcceptHeaderDto acceptHeader
    )
    {
        List<TagDto> tags = await dbContext
            .Tags
            .Select(TagQueries.ProjectToDto())
            .ToListAsync();

        var habitsCollectionDto = new TagsCollectionDto
        {
            Items = tags
        };

        // if (acceptHeader.IncludeLinks)
        // {
        //     habitsCollectionDto.Links = CreateLinksForTags();
        // }


        return Ok(habitsCollectionDto);
    }


    //! GET: tags/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(string id)
    {
        TagDto? tag = await dbContext
            .Tags
            .Where(t => t.Id == id)
            .Select(TagQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (tag is null)
        {
            return NotFound();
        }


        return Ok(tag);
    }

    //! POST: tags
    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag(
        CreateTagDto createTagDto,
        // Method injection of the validator
        IValidator<CreateTagDto> validator,
        // This will allow us to create ProblemDetails responses
        ProblemDetailsFactory problemDetailsFactory
    )
    {
        ValidationResult validationResult = await validator.ValidateAsync(createTagDto);
        if (!validationResult.IsValid)
        {
            // Create a ProblemDetails response with validation errors
            ProblemDetails problem =
                problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest);

            problem.Extensions.Add("errors", validationResult.ToDictionary());

            return BadRequest(problem);
        }

        Tag tag = createTagDto.ToEntity();

        if (await dbContext.Tags.AnyAsync(t => t.Name == tag.Name))
        {
            return Problem( // this will return as a problem details response
                $"The tag '{tag.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        dbContext.Tags.Add(tag);

        // Race condition possible here. Duplicate tags
        await dbContext.SaveChangesAsync();

        TagDto tagDto = tag.ToDto();


        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    //! PUT: tags/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTag(string id, UpdateTagDto updateTagDto)
    {
        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            return NotFound();
        }

        tag.UpdateFromDto(updateTagDto);

        await dbContext.SaveChangesAsync();


        return NoContent();
    }

    //! DELETE: tags/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(string id)
    {
        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            return NotFound();
        }

        dbContext.Tags.Remove(tag);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
