using Microsoft.OpenApi;

namespace AsisyaApi.Api.Extensions;

public static class SwaggerExtensions
{
    private const string SchemeId = "Bearer";

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Asisya API",
                Version = "v1",
                Description = "Productos y categorías. Obtén un token en POST /api/auth/login y pulsa Authorize."
            });

            options.AddSecurityDefinition(SchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Pega solo el token JWT (sin el prefijo 'Bearer')."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeId, document)] = []
            });
        });

        return services;
    }
}
