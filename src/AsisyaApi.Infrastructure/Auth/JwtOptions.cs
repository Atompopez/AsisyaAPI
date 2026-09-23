namespace AsisyaApi.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Mínimo 32 bytes (HMAC-SHA256). Se inyecta por variable de entorno o user-secrets.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AsisyaApi";
    public string Audience { get; set; } = "AsisyaClients";
    public int ExpirationMinutes { get; set; } = 60;
}
