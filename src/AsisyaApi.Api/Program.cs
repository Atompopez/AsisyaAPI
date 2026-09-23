using AsisyaApi.Api.Extensions;
using AsisyaApi.Api.Middleware;
using AsisyaApi.Application;
using AsisyaApi.Infrastructure;
using AsisyaApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddHealthChecks();

const string FrontendCorsPolicy = "Frontend";
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Falla de inmediato si falta el secreto JWT, antes de tocar la base de datos.
_ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

// Esquema de BD mediante EF Core Migrations, aplicadas al arrancar (nunca EnsureCreated).
await app.ApplyMigrationsAsync();
await app.EnsureAdminUserAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger se expone en todos los entornos para facilitar la evaluación de la prueba.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Expuesto para WebApplicationFactory en las pruebas de integración.</summary>
public partial class Program;
