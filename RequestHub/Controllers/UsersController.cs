using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;
using RequestHub.Infrastructure.Persistence;
using System.Security.Claims;

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

    [HttpGet("debug-claims")]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims
            .Select(x => new
            {
                type = x.Type,
                value = x.Value
            })
            .ToList();

        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            authenticationType = User.Identity?.AuthenticationType ?? "",
            name = User.Identity?.Name ?? "",
            currentRole = GetCurrentRole(),
            currentAreaId = GetCurrentAreaId(),
            claims
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var currentRole = GetCurrentRole();

        if (!CanManageUsers(currentRole))
            return Unauthorized(new { message = "No autorizado" });

        var currentAreaId = GetCurrentAreaId();

        var query = dbContext.Users
            .AsNoTracking()
            .AsQueryable();

        if (!IsSuperAdmin(currentRole) && currentAreaId.HasValue)
            query = query.Where(x => x.AreaId == currentAreaId.Value);

        var users = await query
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
    public async Task<IActionResult> GetById(int id)
    {
        var currentRole = GetCurrentRole();

        if (!CanManageUsers(currentRole))
            return Unauthorized(new { message = "No autorizado" });

        var currentAreaId = GetCurrentAreaId();

        var query = dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == id)
            .AsQueryable();

        if (!IsSuperAdmin(currentRole) && currentAreaId.HasValue)
            query = query.Where(x => x.AreaId == currentAreaId.Value);

        var user = await query
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
            return NotFound(new { message = "Usuario no encontrado" });

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto request)
    {
        var currentRole = GetCurrentRole();

        if (!CanManageUsers(currentRole))
            return Unauthorized(new { message = "No autorizado" });

        var currentAreaId = GetCurrentAreaId();

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

        if (!CanAssignRole(currentRole, roleText))
            return Forbid();

        var requiresArea = RoleRequiresArea(roleText);
        var areaId = ResolveAreaIdForWrite(currentRole, currentAreaId, request.AreaId, requiresArea);

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
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto request)
    {
        var currentRole = GetCurrentRole();

        if (!CanManageUsers(currentRole))
            return Unauthorized(new { message = "No autorizado" });

        var currentAreaId = GetCurrentAreaId();

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            return NotFound(new { message = "Usuario no encontrado" });

        if (!CanManageTargetUser(currentRole, currentAreaId, user))
            return NotFound(new { message = "Usuario no encontrado" });

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

        if (!CanAssignRole(currentRole, roleText))
            return Forbid();

        var requiresArea = RoleRequiresArea(roleText);
        var areaId = ResolveAreaIdForWrite(currentRole, currentAreaId, request.AreaId, requiresArea);

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
    public async Task<IActionResult> Delete(int id)
    {
        var currentRole = GetCurrentRole();

        if (!IsSuperAdmin(currentRole))
            return Unauthorized(new { message = "No autorizado" });

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            return NotFound(new { message = "Usuario no encontrado" });

        var currentUsername =
            User.FindFirst("username")?.Value ??
            User.FindFirst(ClaimTypes.Name)?.Value ??
            string.Empty;

        var currentEmail =
            User.FindFirst("email")?.Value ??
            User.FindFirst(ClaimTypes.Email)?.Value ??
            string.Empty;

        if ((!string.IsNullOrWhiteSpace(currentUsername) && user.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(currentEmail) && user.Email.Equals(currentEmail, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { message = "No puedes eliminar tu propio usuario" });
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        return Ok(new { message = "Usuario eliminado correctamente" });
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto request)
    {
        var currentRole = GetCurrentRole();

        if (!CanManageUsers(currentRole))
            return Unauthorized(new { message = "No autorizado" });

        var currentAreaId = GetCurrentAreaId();

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            return NotFound(new { message = "Usuario no encontrado" });

        if (!CanManageTargetUser(currentRole, currentAreaId, user))
            return NotFound(new { message = "Usuario no encontrado" });

        var newPassword = request.NewPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newPassword))
            return BadRequest(new { message = "NewPassword requerido" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        await dbContext.SaveChangesAsync();

        return Ok(new { message = "Contraseña actualizada correctamente" });
    }

    private string GetCurrentRole()
    {
        var rawRole =
            User.FindFirst("role")?.Value ??
            User.FindFirst("Role")?.Value ??
            User.FindFirst(ClaimTypes.Role)?.Value ??
            User.Claims.FirstOrDefault(x => x.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))?.Value ??
            string.Empty;

        return NormalizeRole(rawRole);
    }

    private int? GetCurrentAreaId()
    {
        var rawValue =
            User.FindFirst("areaId")?.Value ??
            User.FindFirst("AreaId")?.Value ??
            User.FindFirst("IdArea")?.Value;

        if (int.TryParse(rawValue, out var areaId) && areaId > 0)
            return areaId;

        return null;
    }

    private static bool CanManageUsers(string role)
    {
        return IsAdmin(role) || IsSuperAdmin(role);
    }

    private static bool IsSuperAdmin(string role)
    {
        return NormalizeRole(role) == "SuperAdmin";
    }

    private static bool IsAdmin(string role)
    {
        return NormalizeRole(role) == "Admin";
    }

    private static bool CanAssignRole(string currentRole, string targetRole)
    {
        var normalizedCurrentRole = NormalizeRole(currentRole);
        var normalizedTargetRole = NormalizeRole(targetRole);

        if (normalizedCurrentRole == "SuperAdmin")
            return true;

        if (normalizedCurrentRole == "Admin" && normalizedTargetRole != "SuperAdmin")
            return true;

        return false;
    }

    private static bool CanManageTargetUser(string currentRole, int? currentAreaId, User targetUser)
    {
        var normalizedCurrentRole = NormalizeRole(currentRole);
        var normalizedTargetRole = NormalizeRole(targetUser.Role);

        if (normalizedCurrentRole == "SuperAdmin")
            return true;

        if (normalizedCurrentRole != "Admin")
            return false;

        if (!currentAreaId.HasValue)
            return false;

        if (normalizedTargetRole == "SuperAdmin")
            return false;

        return targetUser.AreaId == currentAreaId.Value;
    }

    private static int? ResolveAreaIdForWrite(string currentRole, int? currentAreaId, int? requestedAreaId, bool requiresArea)
    {
        var normalizedCurrentRole = NormalizeRole(currentRole);

        if (normalizedCurrentRole == "SuperAdmin")
            return requiresArea ? requestedAreaId : null;

        if (normalizedCurrentRole == "Admin")
            return requiresArea ? currentAreaId : null;

        return null;
    }

    private static bool RoleRequiresArea(string role)
    {
        var normalizedRole = NormalizeRole(role);

        return normalizedRole == "Admin" || normalizedRole == "Gestor";
    }

    private static string NormalizeRole(string role)
    {
        var value = (role ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var compact = value.Replace("_", "").Replace(" ", "").Trim().ToLowerInvariant();

        return compact switch
        {
            "superadmin" => "SuperAdmin",
            "admin" => "Admin",
            "administrador" => "Admin",
            "gestor" => "Gestor",
            "solicitante" => "Solicitante",
            _ => value
        };
    }
}