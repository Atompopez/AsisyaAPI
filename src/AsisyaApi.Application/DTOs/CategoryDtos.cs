using System.ComponentModel.DataAnnotations;

namespace AsisyaApi.Application.DTOs;

public sealed class CreateCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required, Url, StringLength(500)]
    public string PhotoUrl { get; init; } = string.Empty;
}

public sealed record CategoryResponse(int Id, string Name, string PhotoUrl);
