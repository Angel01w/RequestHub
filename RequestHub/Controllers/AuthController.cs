using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RequestHub.Infrastructure.Auth;
using RequestHub.Infrastructure.Persistence;

namespace RequestHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext dbContext, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public sealed class LoginModel
    {
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel request)
    {
        if (request is null)
            return BadRequest(new { message = "Datos de login requeridos" });

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var username = (request.Username ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;

        if ((string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(username)) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Email o username y password requeridos" });

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                (!string.IsNullOrWhiteSpace(email) && u.Email.ToLower() == email) ||
                (!string.IsNullOrWhiteSpace(username) && u.Username.ToLower() == username));

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas" });

        var normalizedRole = NormalizeRole(user.Role);
        var normalizedAreaId = RequiresArea(normalizedRole) ? user.AreaId : null;

        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Iss, _jwtOptions.Issuer),
            new(JwtRegisteredClaimNames.Aud, _jwtOptions.Audience),
            new(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(now).ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds().ToString()),
            new("userId", user.Id.ToString()),
            new("username", user.Username ?? string.Empty),
            new("email", user.Email ?? string.Empty),
            new("fullName", user.FullName ?? string.Empty),
            new("role", normalizedRole),
            new(ClaimTypes.Role, normalizedRole),
            new(ClaimTypes.Name, user.Username ?? string.Empty)
        };

        if (normalizedAreaId.HasValue)
            claims.Add(new Claim("areaId", normalizedAreaId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials
        );

        var handler = new JwtSecurityTokenHandler();
        var rawToken = handler.WriteToken(token);
        var tokenSha256 = ComputeSha256(rawToken);

        return Ok(new
        {
            token = rawToken,
            tokenSha256,
            expiresAtUtc,
            user = new
            {
                id = user.Id,
                email = user.Email,
                username = user.Username,
                fullName = user.FullName,
                role = normalizedRole,
                areaId = normalizedAreaId
            },
            email = user.Email,
            username = user.Username,
            fullName = user.FullName,
            role = normalizedRole,
            areaId = normalizedAreaId,
            message = "Login final funcionando"
        });
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeRole(string role)
    {
        var value = (role ?? string.Empty).Trim();

        if (value.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            return "Admin";

        if (value.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            return "SuperAdmin";

        return value;
    }

    private static bool RequiresArea(string role)
    {
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            || role.Equals("Gestor", StringComparison.OrdinalIgnoreCase);
    }
}