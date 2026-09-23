using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AsisyaApi.Infrastructure.Auth;

public static class AuthenticationExtensions
{
    public const int MinimumKeyBytes = 32;

    /// <summary>
    /// Configura la validación de JWT Bearer. Falla al arrancar si la clave no está configurada:
    /// el secreto nunca vive en el repositorio (variable de entorno Jwt__Key o user-secrets).
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= MinimumKeyBytes,
                $"Jwt:Key debe configurarse (variable de entorno Jwt__Key o user-secrets) con al menos {MinimumKeyBytes} bytes.")
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Se configura de forma diferida (al resolver las opciones) para que respete overrides de
        // configuración aplicados después del registro, p. ej. en WebApplicationFactory.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenGenerator.CreateSigningKey(jwt.Key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "unique_name"
                };
            });

        services.AddAuthorization();
        return services;
    }
}
