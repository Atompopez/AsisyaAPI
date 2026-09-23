namespace AsisyaApi.Application.Common;

/// <summary>El recurso solicitado no existe (se traduce a HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Conflicto con el estado actual, p. ej. nombre duplicado (HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Regla de negocio o validación incumplida (HTTP 400).</summary>
public sealed class BusinessValidationException(string message) : Exception(message);

/// <summary>Credenciales inválidas (HTTP 401).</summary>
public sealed class InvalidCredentialsException() : Exception("Usuario o contraseña inválidos.");
