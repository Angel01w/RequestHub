using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;
using RequestHub.Infrastructure.Persistence;
using System.Security.Claims;

namespace RequestHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
        public object? Id { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public object? AreaId { get; set; }
    }

    bool IsAdmin()
    {
        var role =
            User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ??
            User.Claims.FirstOrDefault(c => c.Type.Equals("role", StringComparison.OrdinalIgnoreCase))?.Value ??
            User.Claims.FirstOrDefault(c => c.Type.Equals("Role", StringComparison.OrdinalIgnoreCase))?.Value ??
            "";

        role = role.Trim().Trim('"');
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
    }

    static object? GetProp(object target, params string[] names)
    {
        foreach (var n in names)
        {
            var p = target.GetType().GetProperty(n);
            if (p == null) continue;
            return p.GetValue(target);
        }
        return null;
    }

    static void SetProp(object target, string propName, object? value)
    {
        var p = target.GetType().GetProperty(propName);
        if (p == null || !p.CanWrite) return;

        if (value == null)
        {
            if (!p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) != null)
                p.SetValue(target, null);
            return;
        }

        var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

        try
        {
            if (t.IsEnum)
            {
                if (value is string s) p.SetValue(target, Enum.Parse(t, s, true));
                else p.SetValue(target, Enum.ToObject(t, value));
                return;
            }

            if (t == typeof(string))
            {
                p.SetValue(target, Convert.ToString(value));
                return;
            }

            p.SetValue(target, Convert.ChangeType(value, t));
        }
        catch { }
    }

    static int? GetInt(object target, params string[] names)
    {
        var v = GetProp(target, names);
        if (v == null) return null;
        if (v is int i) return i;
        return int.TryParse(v.ToString(), out var x) ? x : null;
    }

    static string GetString(object target, params string[] names)
    {
        var v = GetProp(target, names);
        return v == null ? "" : Convert.ToString(v) ?? "";
    }

    static UserItemDto ToItem(User u)
    {
        return new UserItemDto
        {
            Id = GetProp(u, "Id", "UserId", "IdUsuario"),
            Username = GetString(u, "Username", "UserName", "Email"),
            FullName = GetString(u, "FullName", "Name", "NombreCompleto"),
            Role = GetString(u, "Role", "Rol"),
            AreaId = GetProp(u, "AreaId", "IdArea")
        };
    }

    static void ApplyCommon(User u, string username, string fullName, string role, int? areaId)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            u.Username = username;
            SetProp(u, "UserName", username);
            if (username.Contains("@")) SetProp(u, "Email", username);
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            SetProp(u, "FullName", fullName);
            SetProp(u, "Name", fullName);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            try { u.Role = Enum.Parse<UserRole>(role, true); } catch { }
            SetProp(u, "Rol", role);
        }

        if (areaId.HasValue)
        {
            SetProp(u, "AreaId", areaId.Value);
            SetProp(u, "IdArea", areaId.Value);
        }
    }

    static void SetPassword(User u, string password)
    {
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
    }

    [HttpGet]
    public async Task<ActionResult<List<UserItemDto>>> GetAll()
    {
        if (!IsAdmin()) return Forbid();

        var list = await dbContext.Users.AsNoTracking().ToListAsync();
        var res = new List<UserItemDto>(list.Count);
        foreach (var u in list) res.Add(ToItem(u));
        return Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<UserItemDto>> Create([FromBody] CreateUserDto request)
    {
        if (!IsAdmin()) return Forbid();

        var username = (request.Username ?? "").Trim();
        var password = request.Password ?? "";
        var fullName = (request.FullName ?? "").Trim();
        var role = (request.Role ?? "").Trim();

        if (string.IsNullOrWhiteSpace(username)) return BadRequest("Username requerido");
        if (string.IsNullOrWhiteSpace(password)) return BadRequest("Password requerido");
        if (string.IsNullOrWhiteSpace(role)) return BadRequest("Role requerido");

        var exists = await dbContext.Users.AnyAsync(u => u.Username == username);
        if (exists) return Conflict("El usuario ya existe");

        var u = new User();
        ApplyCommon(u, username, fullName, role, request.AreaId);
        SetPassword(u, password);

        dbContext.Users.Add(u);
        await dbContext.SaveChangesAsync();

        return Ok(ToItem(u));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserItemDto>> Update(int id, [FromBody] UpdateUserDto request)
    {
        if (!IsAdmin()) return Forbid();

        var user = await dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        var username = (request.Username ?? "").Trim();
        var fullName = (request.FullName ?? "").Trim();
        var role = (request.Role ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(username))
        {
            var other = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == username);
            if (other != null)
            {
                var otherId = GetInt(other, "Id", "UserId", "IdUsuario");
                if (otherId.HasValue && otherId.Value != id) return Conflict("El usuario ya existe");
            }
        }

        ApplyCommon(user, username, fullName, role, request.AreaId);
        await dbContext.SaveChangesAsync();

        return Ok(ToItem(user));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin()) return Forbid();

        var user = await dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto request)
    {
        if (!IsAdmin()) return Forbid();

        var user = await dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        var pass = request.NewPassword ?? "";
        if (string.IsNullOrWhiteSpace(pass)) return BadRequest("NewPassword requerido");

        SetPassword(user, pass);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}