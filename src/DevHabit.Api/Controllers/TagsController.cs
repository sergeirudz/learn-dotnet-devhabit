using System.Net.Mime;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Settings;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("tags")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.HateoasJsonV1
)]
public sealed class TagsController(
    ApplicationDbContext dbContext,
    LinkService linkService,
    IOptions<TagsOptions> tagsOptions,
    UserContext userContext
) : ControllerBase
{
    //! GET: tags
    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags(
        [FromHeader(Name = "Accept")] AcceptHeaderDto acceptHeader
    )
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        List<TagDto> tags = await dbContext
            .Tags
            .Where(t => t.UserId == userId)
            .Select(TagQueries.ProjectToDto())
            .ToListAsync();

        var habitsCollectionDto = new TagsCollectionDto
        {
            Items = tags
        };

        if (acceptHeader.IncludeLinks)
        {
            habitsCollectionDto.Links = CreateLinksForTags(tags.Count);
        }


        return Ok(habitsCollectionDto);
    }


    //! GET: tags/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(string id,
        [FromHeader] AcceptHeaderDto acceptHeader
    )
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        TagDto? tag = await dbContext
            .Tags
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(TagQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (tag is null)
        {
            return NotFound();
        }

        if (acceptHeader.IncludeLinks)
        {
            tag.Links = CreateLinksForTag(id);
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
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        ValidationResult validationResult = await validator.ValidateAsync(createTagDto);
        if (!validationResult.IsValid)
        {
            // Create a ProblemDetails response with validation errors
            ProblemDetails problem =
                problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest);

            problem.Extensions.Add("errors", validationResult.ToDictionary());

            return BadRequest(problem);
        }

        Tag tag = createTagDto.ToEntity(userId);

        if (await dbContext.Tags.AnyAsync(t => t.UserId == userId && t.Name == tag.Name))
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
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

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
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (tag is null)
        {
            return NotFound();
        }

        dbContext.Tags.Remove(tag);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }


    private List<LinkDto> CreateLinksForTags(int tagsCount)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetTags), "self", HttpMethods.Get)
        ];

        if (tagsCount < tagsOptions.Value.MaxAllowedTags)
        {
            links.Add(linkService.Create(nameof(CreateTag), "create", HttpMethods.Post));
        }

        return links;
    }

    private List<LinkDto> CreateLinksForTag(string id)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetTag), "self", HttpMethods.Get, new { id }),
            linkService.Create(nameof(UpdateTag), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(DeleteTag), "delete", HttpMethods.Delete, new { id })
        ];

        return links;
    }
}
