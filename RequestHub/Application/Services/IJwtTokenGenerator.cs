using RequestHub.Domain.Entities;

namespace RequestHub.Application.Services;

public interface IJwtTokenGenerator
{
    (string token, DateTime expiresAtUtc) Generate(User user);
}