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

    public int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue(ClaimTypes.Name)
                               ?? User.FindFirstValue(ClaimTypes.Sid)
                               ?? User.FindFirstValue(ClaimTypes.PrimarySid)
                               ?? User.FindFirstValue("sub")
                               ?? throw new UnauthorizedAccessException("No user id claim found."));

    public UserRole Role => Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role) ?? throw new UnauthorizedAccessException());

    public int? AreaId
    {
        get
        {
            var value = User.FindFirstValue("areaId");
            return int.TryParse(value, out var areaId) ? areaId : null;
        }
    }
}
