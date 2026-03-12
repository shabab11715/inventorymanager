using System.Security.Claims;

namespace InventoryManager.WebApi.Auth;

public static class RoleContext
{
    public static bool IsAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("admin");
    }
}