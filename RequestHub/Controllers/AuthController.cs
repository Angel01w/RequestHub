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
        public string Password { get; set; } = "";
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel request)
    {
        var email = (request.Email ?? "").Trim().ToLower();
        var password = request.Password ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest("Email y password requeridos");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Unauthorized();

        var (token, expiresAtUtc) = tokenGenerator.Generate(user);

        return Ok(new
        {
            token,
            expiresAtUtc,
            email = user.Email,
            role = user.Role.ToString()
        });
    }
}