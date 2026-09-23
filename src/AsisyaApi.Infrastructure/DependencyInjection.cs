using AsisyaApi.Application.Interfaces;
using AsisyaApi.Infrastructure.Auth;
using AsisyaApi.Infrastructure.BulkLoad;
using AsisyaApi.Infrastructure.Persistence;
using AsisyaApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AsisyaApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Un único NpgsqlDataSource (pool de conexiones) compartido por EF Core y por el COPY binario.
        // La cadena se lee al resolver el servicio para respetar overrides tardíos de configuración.
        services.AddSingleton(sp =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection.");
            return new NpgsqlDataSourceBuilder(connectionString).Build();
        });
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBulkJobRepository, BulkJobRepository>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddJwtAuthentication(configuration);

        // Carga masiva: channel singleton compartido entre productor (API) y consumidor (hosted service).
        services.AddSingleton<BulkLoadChannel>();
        services.AddSingleton<IBulkLoadQueue>(sp => sp.GetRequiredService<BulkLoadChannel>());
        services.AddScoped<IBulkProductWriter, BulkProductWriter>();
        services.AddHostedService<BulkLoadBackgroundService>();

        return services;
    }
}
