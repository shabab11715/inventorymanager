using System.Security.Claims;

namespace InventoryManager.WebApi.Auth;

public static class UserContext
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");

        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}