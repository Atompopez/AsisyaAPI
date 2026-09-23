using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;
using AsisyaApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AsisyaApi.Application.Services;

public class AuthService(IUserRepository users, IPasswordHasher<User> passwordHasher, IJwtTokenGenerator tokenGenerator)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByUsernameAsync(request.Username.Trim(), ct) ?? throw new InvalidCredentialsException();

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        var (token, expiresAt) = tokenGenerator.Generate(user);
        return new LoginResponse(token, expiresAt, user.Username);
    }

    /// <summary>
    /// Crea el usuario si todavía no existe. Se usa al arrancar para garantizar un usuario
    /// administrador cuyas credenciales llegan por configuración (variables de entorno).
    /// </summary>
    /// <returns><c>true</c> si el usuario fue creado.</returns>
    public async Task<bool> EnsureUserAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new BusinessValidationException("Usuario y contraseña son obligatorios.");
        }

        if (await users.GetByUsernameAsync(username.Trim(), ct) is not null)
        {
            return false;
        }

        var user = new User { Username = username.Trim() };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        await users.AddAsync(user, ct);
        return true;
    }
}
