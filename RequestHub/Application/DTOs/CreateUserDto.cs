using RequestHub.Domain.Enums;

namespace RequestHub.Application.DTOs.Users;

public class CreateUserDto
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public UserRole Role { get; set; }
    public int? AreaId { get; set; }
}