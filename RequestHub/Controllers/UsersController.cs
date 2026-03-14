using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;
using RequestHub.Infrastructure.Persistence;

namespace RequestHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController(AppDbContext dbContext) : ControllerBase
{
    public sealed class CreateUserDto
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public int? AreaId { get; set; }
    }

    public sealed class UpdateUserDto
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public int? AreaId { get; set; }
    }

    public sealed class ResetPasswordDto
    {
        public string NewPassword { get; set; } = "";
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.Email,
                x.FullName,
                role = x.Role,
                x.AreaId
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.Email,
                x.FullName,
                role = x.Role,
                x.AreaId
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound(new
            {
                message = "Usuario no encontrado"
            });
        }

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto request)
    {
        var username = (request.Username ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;
        var fullName = (request.FullName ?? string.Empty).Trim();
        var roleText = NormalizeRole(request.Role);

        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Username requerido" });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email requerido" });

        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Password requerido" });

        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { message = "FullName requerido" });

        if (string.IsNullOrWhiteSpace(roleText))
            return BadRequest(new { message = "Role requerido" });

        if (!Enum.TryParse<UserRole>(roleText, true, out _))
            return BadRequest(new { message = "Role inválido" });

        var requiresArea = RoleRequiresArea(roleText);
        var areaId = requiresArea ? request.AreaId : null;

        if (requiresArea && (!areaId.HasValue || areaId.Value <= 0))
            return BadRequest(new { message = "Debe seleccionar un área para este rol" });

        if (areaId.HasValue)
        {
            var areaExists = await dbContext.Areas
                .AsNoTracking()
                .AnyAsync(x => x.Id == areaId.Value);

            if (!areaExists)
                return NotFound(new { message = "Área no encontrada" });
        }

        var usernameLower = username.ToLowerInvariant();

        var usernameExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Username.ToLower() == usernameLower);

        if (usernameExists)
            return Conflict(new { message = "El username ya existe" });

        var emailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email.ToLower() == email);

        if (emailExists)
            return Conflict(new { message = "El email ya existe" });

        var user = new User
        {
            Username = username,
            Email = email,
            FullName = fullName,
            Role = roleText,
            AreaId = areaId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
        {
            message = "Usuario creado correctamente",
            user = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FullName,
                role = user.Role,
                user.AreaId
            }
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto request)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Usuario no encontrado"
            });
        }

        var username = (request.Username ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var fullName = (request.FullName ?? string.Empty).Trim();
        var roleText = NormalizeRole(request.Role);

        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Username requerido" });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email requerido" });

        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { message = "FullName requerido" });

        if (string.IsNullOrWhiteSpace(roleText))
            return BadRequest(new { message = "Role requerido" });

        if (!Enum.TryParse<UserRole>(roleText, true, out _))
            return BadRequest(new { message = "Role inválido" });

        var requiresArea = RoleRequiresArea(roleText);
        var areaId = requiresArea ? request.AreaId : null;

        if (requiresArea && (!areaId.HasValue || areaId.Value <= 0))
            return BadRequest(new { message = "Debe seleccionar un área para este rol" });

        if (areaId.HasValue)
        {
            var areaExists = await dbContext.Areas
                .AsNoTracking()
                .AnyAsync(x => x.Id == areaId.Value);

            if (!areaExists)
                return NotFound(new { message = "Área no encontrada" });
        }

        var usernameLower = username.ToLowerInvariant();

        var usernameExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.Username.ToLower() == usernameLower);

        if (usernameExists)
            return Conflict(new { message = "El username ya existe" });

        var emailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.Email.ToLower() == email);

        if (emailExists)
            return Conflict(new { message = "El email ya existe" });

        user.Username = username;
        user.Email = email;
        user.FullName = fullName;
        user.Role = roleText;
        user.AreaId = areaId;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Usuario actualizado correctamente",
            user = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FullName,
                role = user.Role,
                user.AreaId
            }
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Usuario no encontrado"
            });
        }

        var currentUsername = User.FindFirst("username")?.Value ?? string.Empty;
        var currentEmail = User.FindFirst("email")?.Value ?? string.Empty;

        if ((!string.IsNullOrWhiteSpace(currentUsername) && user.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(currentEmail) && user.Email.Equals(currentEmail, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new
            {
                message = "No puedes eliminar tu propio usuario"
            });
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Usuario eliminado correctamente"
        });
    }

    [HttpPost("{id:int}/reset-password")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto request)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Usuario no encontrado"
            });
        }

        var newPassword = request.NewPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newPassword))
            return BadRequest(new { message = "NewPassword requerido" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Contraseña actualizada correctamente"
        });
    }

    private static bool RoleRequiresArea(string role)
    {
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            || role.Equals("Gestor", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRole(string role)
    {
        var value = (role ?? string.Empty).Trim();

        if (value.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            return "Admin";

        return value;
    }
}