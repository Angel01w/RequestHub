using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;
using RequestHub.Infrastructure.Persistence;

namespace RequestHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController(AppDbContext dbContext) : ControllerBase
{
    public sealed class CreateUserDto
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public int? AreaId { get; set; }
    }

    public sealed class UpdateUserDto
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public int? AreaId { get; set; }
    }

    public sealed class ResetPasswordDto
    {
        public string NewPassword { get; set; } = "";
    }

    public sealed class UserItemDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public int? AreaId { get; set; }
    }

    static UserItemDto ToItem(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        FullName = u.FullName,
        Role = u.Role.ToString(),
        AreaId = u.AreaId
    };

    [HttpGet]
    public async Task<ActionResult<List<UserItemDto>>> GetAll()
    {
        var list = await dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .ToListAsync();

        return Ok(list.Select(ToItem).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<UserItemDto>> Create([FromBody] CreateUserDto request)
    {
        var username = (request.Username ?? "").Trim();
        var password = request.Password ?? "";
        var fullName = (request.FullName ?? "").Trim();
        var roleText = (request.Role ?? "").Trim();

        if (string.IsNullOrWhiteSpace(username)) return BadRequest("Username requerido");
        if (string.IsNullOrWhiteSpace(password)) return BadRequest("Password requerido");
        if (string.IsNullOrWhiteSpace(fullName)) return BadRequest("FullName requerido");
        if (string.IsNullOrWhiteSpace(roleText)) return BadRequest("Role requerido");

        if (!Enum.TryParse<UserRole>(roleText, true, out var role))
            return BadRequest("Role inválido");

        var exists = await dbContext.Users.AnyAsync(x => x.Username == username);
        if (exists) return Conflict("El usuario ya existe");

        var user = new User
        {
            Username = username,
            FullName = fullName,
            Role = role,
            AreaId = request.AreaId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return Ok(ToItem(user));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserItemDto>> Update(int id, [FromBody] UpdateUserDto request)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        var username = (request.Username ?? "").Trim();
        var fullName = (request.FullName ?? "").Trim();
        var roleText = (request.Role ?? "").Trim();

        if (string.IsNullOrWhiteSpace(username)) return BadRequest("Username requerido");
        if (string.IsNullOrWhiteSpace(fullName)) return BadRequest("FullName requerido");
        if (string.IsNullOrWhiteSpace(roleText)) return BadRequest("Role requerido");

        if (!Enum.TryParse<UserRole>(roleText, true, out var role))
            return BadRequest("Role inválido");

        var exists = await dbContext.Users.AnyAsync(x => x.Username == username && x.Id != id);
        if (exists) return Conflict("El usuario ya existe");

        user.Username = username;
        user.FullName = fullName;
        user.Role = role;
        user.AreaId = request.AreaId;

        await dbContext.SaveChangesAsync();

        return Ok(ToItem(user));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto request)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        var pass = request.NewPassword ?? "";
        if (string.IsNullOrWhiteSpace(pass)) return BadRequest("NewPassword requerido");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}