using System.Security.Claims;

namespace DevHabit.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetIdentityId(this ClaimsPrincipal? principal)
    {
        string? identityId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        // return the identityId of the current user
        return identityId;
    }
}
