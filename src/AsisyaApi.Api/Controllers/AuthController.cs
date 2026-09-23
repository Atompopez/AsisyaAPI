using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsisyaApi.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Valida usuario y contraseña y devuelve un JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await authService.LoginAsync(request, ct));
}
