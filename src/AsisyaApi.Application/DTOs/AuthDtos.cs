using System.ComponentModel.DataAnnotations;

namespace AsisyaApi.Application.DTOs;

public sealed class LoginRequest
{
    [Required, StringLength(100)]
    public string Username { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponse(string Token, DateTime ExpiresAt, string Username);
