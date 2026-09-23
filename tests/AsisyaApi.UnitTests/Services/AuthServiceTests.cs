using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;
using AsisyaApi.Application.Services;
using AsisyaApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AsisyaApi.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IJwtTokenGenerator> _tokens = new();
    private readonly PasswordHasher<User> _hasher = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users.Object, _hasher, _tokens.Object);
    }

    private User UserWithPassword(string password)
    {
        var user = new User { Id = 1, Username = "admin" };
        user.PasswordHash = _hasher.HashPassword(user, password);
        return user;
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenPasswordMatches()
    {
        var user = UserWithPassword("Secreta123!");
        var expires = DateTime.UtcNow.AddHours(1);
        _users.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokens.Setup(t => t.Generate(user)).Returns(("jwt-token", expires));

        var result = await _sut.LoginAsync(new LoginRequest { Username = "admin", Password = "Secreta123!" });

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal(expires, result.ExpiresAt);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenPasswordIsWrong()
    {
        _users.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(UserWithPassword("Secreta123!"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "admin", Password = "otra" }));
        _tokens.Verify(t => t.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserDoesNotExist()
    {
        _users.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "nadie", Password = "x" }));
    }

    [Fact]
    public async Task EnsureUserAsync_StoresHashNotPlainText()
    {
        _users.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        User? saved = null;
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => saved = u)
            .Returns(Task.CompletedTask);

        var created = await _sut.EnsureUserAsync("admin", "Secreta123!");

        Assert.True(created);
        Assert.NotNull(saved);
        Assert.NotEqual("Secreta123!", saved!.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, _hasher.VerifyHashedPassword(saved, saved.PasswordHash, "Secreta123!"));
    }

    [Fact]
    public async Task EnsureUserAsync_DoesNothing_WhenUserExists()
    {
        _users.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(UserWithPassword("x"));

        var created = await _sut.EnsureUserAsync("admin", "Secreta123!");

        Assert.False(created);
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
