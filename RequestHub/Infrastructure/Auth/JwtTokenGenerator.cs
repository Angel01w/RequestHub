using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RequestHub.Application.Services;
using RequestHub.Domain.Entities;

namespace RequestHub.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public (string token, DateTime expiresAtUtc) Generate(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("username", user.Username),
            new("role", user.Role.ToString())
        };

        if (user.AreaId.HasValue)
            claims.Add(new Claim("areaId", user.AreaId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return (token, expiresAtUtc);
    }
}