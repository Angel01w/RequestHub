using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RequestHub.Domain.Enums;

namespace RequestHub.Application.Services;

public interface ICurrentUserAccessor
{
    int UserId { get; }
    UserRole Role { get; }
    int? AreaId { get; }
}

public class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? throw new UnauthorizedAccessException();

    public int UserId
    {
        get
        {
            var value =
                User.FindFirstValue("userId") ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                throw new UnauthorizedAccessException("No user id claim found.");

            return int.Parse(value);
        }
    }

    public UserRole Role
    {
        get
        {
            var value =
                User.FindFirstValue("role") ??
                User.FindFirstValue(ClaimTypes.Role) ??
                throw new UnauthorizedAccessException("No role claim found.");

            return Enum.Parse<UserRole>(value, true);
        }
    }

    public int? AreaId
    {
        get
        {
            var value = User.FindFirstValue("areaId");
            return int.TryParse(value, out var areaId) ? areaId : null;
        }
    }
}