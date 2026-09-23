using System.Net.Http.Headers;
using System.Net.Http.Json;
using AsisyaApi.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace AsisyaApi.IntegrationTests;

/// <summary>
/// Levanta la API real (Program) contra un PostgreSQL efímero en Docker vía Testcontainers.
/// Si se define TEST_POSTGRES_CONNECTION se usa esa base existente en su lugar (útil en máquinas sin Docker).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminUser = "admin";
    public const string AdminPassword = "IntegrationTest123!";
    private const string JwtKey = "integration-tests-signing-key-with-more-than-32-bytes";

    private readonly string? _externalConnection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION");
    private PostgreSqlContainer? _container;

    private string ConnectionString => _externalConnection ?? _container!.GetConnectionString();

    public async Task InitializeAsync()
    {
        if (_externalConnection is null)
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("SeedAdmin:Username", AdminUser);
        builder.UseSetting("SeedAdmin:Password", AdminPassword);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = AdminUser, password = AdminPassword });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
