using AsisyaApi.Domain.Entities;

namespace AsisyaApi.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(User user);
}
