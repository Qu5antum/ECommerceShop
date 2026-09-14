using System.Security.Claims;

namespace App.Controllers;

public class Helper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Helper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null)
        {
            throw new UnauthorizedAccessException("HttpContext or User is not available");
        }

        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
        {
            throw new UnauthorizedAccessException("User ID not found or invalid in token");
        }

        return userId;
    }
}