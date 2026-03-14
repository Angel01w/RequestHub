using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Application.Services;
using RequestHub.Infrastructure.Persistence;

namespace RequestHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext dbContext, IJwtTokenGenerator tokenGenerator) : ControllerBase
{
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
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var username = (request.Username ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;

        if ((string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(username)) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new
            {
                message = "Email o username y password requeridos"
            });
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                (!string.IsNullOrWhiteSpace(email) && u.Email.ToLower() == email) ||
                (!string.IsNullOrWhiteSpace(username) && u.Username.ToLower() == username));

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Credenciales inválidas"
            });
        }

        var passwordOk = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!passwordOk)
        {
            return Unauthorized(new
            {
                message = "Credenciales inválidas"
            });
        }

        var (token, expiresAtUtc) = tokenGenerator.Generate(user);

        return Ok(new
        {
            token,
            expiresAtUtc,
            email = user.Email,
            username = user.Username,
            fullName = user.FullName,
            role = user.Role,
            areaId = user.AreaId,
            message = "Login exitoso"
        });
    }
}