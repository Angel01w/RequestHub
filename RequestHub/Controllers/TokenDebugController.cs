using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RequestHub.Infrastructure.Auth;

namespace RequestHub.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class TokenDebugController(IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public sealed class ValidateTokenRequest
    {
        public string Token { get; set; } = "";
    }

    [HttpPost("inspect")]
    public IActionResult Inspect([FromBody] ValidateTokenRequest request)
    {
        var rawToken = NormalizeToken(request.Token);

        if (string.IsNullOrWhiteSpace(rawToken))
            return BadRequest(new { message = "Token requerido." });

        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(rawToken))
            return BadRequest(new { message = "Token inválido o no legible." });

        var jwt = handler.ReadJwtToken(rawToken);

        return Ok(new
        {
            tokenLength = rawToken.Length,
            tokenPrefix = rawToken.Length >= 40 ? rawToken[..40] : rawToken,
            sha256 = ComputeSha256(rawToken),
            issuer = jwt.Issuer,
            audiences = jwt.Audiences.ToList(),
            validFrom = jwt.ValidFrom,
            validTo = jwt.ValidTo,
            rawPayload = jwt.Payload.SerializeToJson(),
            claims = jwt.Claims.Select(x => new
            {
                type = x.Type,
                value = x.Value
            }).ToList()
        });
    }

    [HttpPost("validate")]
    public IActionResult Validate([FromBody] ValidateTokenRequest request)
    {
        var rawToken = NormalizeToken(request.Token);

        if (string.IsNullOrWhiteSpace(rawToken))
            return BadRequest(new { message = "Token requerido." });

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "username",
                RoleClaimType = "role"
            };

            var principal = handler.ValidateToken(rawToken, parameters, out var validatedToken);
            var jwt = validatedToken as JwtSecurityToken;

            return Ok(new
            {
                valid = true,
                tokenLength = rawToken.Length,
                tokenPrefix = rawToken.Length >= 40 ? rawToken[..40] : rawToken,
                sha256 = ComputeSha256(rawToken),
                issuer = jwt?.Issuer,
                audiences = jwt?.Audiences?.ToList(),
                validFrom = jwt?.ValidFrom,
                validTo = jwt?.ValidTo,
                claims = principal.Claims.Select(x => new
                {
                    type = x.Type,
                    value = x.Value
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                valid = false,
                tokenLength = rawToken.Length,
                tokenPrefix = rawToken.Length >= 40 ? rawToken[..40] : rawToken,
                sha256 = ComputeSha256(rawToken),
                error = ex.GetType().Name,
                message = ex.Message,
                config = new
                {
                    issuer = _jwtOptions.Issuer,
                    audience = _jwtOptions.Audience,
                    keyLength = _jwtOptions.Key?.Length ?? 0
                }
            });
        }
    }

    private static string NormalizeToken(string? token)
    {
        var rawToken = (token ?? string.Empty).Trim();

        if (rawToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            rawToken = rawToken["Bearer ".Length..].Trim();

        return rawToken;
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}