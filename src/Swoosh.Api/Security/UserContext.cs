using System.Security.Claims;

namespace Swoosh.Api.Security;

public static class UserContext
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? user.FindFirstValue("sub");
        
        return Guid.Parse(id!);
    }
}