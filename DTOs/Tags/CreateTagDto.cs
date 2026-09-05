using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace DevHabit.Api.DTOs.Tags;

public sealed record CreateTagDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(10)]
    public required string Name { get; set; }
    public string? Description { get; set; }
}

