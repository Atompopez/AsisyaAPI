using AsisyaApi.Application.Services;
using AsisyaApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AsisyaApi.Api.Extensions;

public static class StartupExtensions
{
    private const int MaxMigrationAttempts = 10;

    /// <summary>
    /// Aplica las migraciones pendientes. Reintenta unos segundos porque en docker-compose la BD
    /// puede tardar en aceptar conexiones aunque el contenedor ya esté arriba.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Migraciones aplicadas");
                return;
            }
            catch (Exception ex) when (attempt < MaxMigrationAttempts && ex is Npgsql.NpgsqlException or TimeoutException)
            {
                logger.LogWarning("Base de datos no disponible (intento {Attempt}/{Max}): {Message}",
                    attempt, MaxMigrationAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    /// <summary>
    /// Crea el usuario administrador si no existe. Las credenciales llegan por configuración
    /// (SeedAdmin__Username / SeedAdmin__Password), nunca desde el repositorio.
    /// </summary>
    public static async Task EnsureAdminUserAsync(this WebApplication app)
    {
        var username = app.Configuration["SeedAdmin:Username"];
        var password = app.Configuration["SeedAdmin:Password"];
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SeedAdmin:Username/Password no configurados: no se crea el usuario administrador.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        if (await auth.EnsureUserAsync(username, password))
        {
            logger.LogInformation("Usuario administrador '{Username}' creado", username);
        }
    }
}
