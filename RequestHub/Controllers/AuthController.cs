using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Application.DTOs;
using RequestHub.Infrastructure.Auth;
using RequestHub.Infrastructure.Persistence;

namespace RequestHub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext dbContext, IJwtTokenGenerator tokenGenerator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized();
        }

        var (token, expiresAtUtc) = tokenGenerator.Generate(user);
        return Ok(new AuthResponse(token, expiresAtUtc, user.Username, user.Role.ToString()));
    }
}