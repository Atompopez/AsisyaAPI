using AsisyaApi.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AsisyaApi.Api.Middleware;

/// <summary>Traduce las excepciones de Application a respuestas ProblemDetails (RFC 7807).</summary>
public class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            BusinessValidationException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "No autorizado"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Excepción no controlada");
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // No se filtran detalles internos en errores 500.
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message
            }
        });
    }
}
