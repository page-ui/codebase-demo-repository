using System.Security.Claims;

namespace Page.Ui.Presentation.Common.Security;

internal static class ClaimsPrincipalExtensions
{
    public static bool IsInternalAiPrincipal(this ClaimsPrincipal? principal)
    {
        return string.Equals(principal?.FindFirstValue("sub"), "worker-ai", StringComparison.Ordinal);
    }

    public static string? GetCurrentUserId(this ClaimsPrincipal? principal)
    {
        var sub = principal?.FindFirstValue("sub");
        if (sub == "worker-ai")
        {
            return principal?.FindFirstValue("user_id");
        }

        return sub
            ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal?.FindFirstValue("nameid")
            ?? principal?.FindFirstValue("user_id")
            ?? principal?.FindFirstValue("uid");
    }

    public static Guid? GetInternalChatId(this ClaimsPrincipal? principal)
    {
        return TryGetGuidClaim(principal, "chat_id");
    }

    public static Guid? GetInternalMessageId(this ClaimsPrincipal? principal)
    {
        return TryGetGuidClaim(principal, "message_id");
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal? principal, string claimType)
    {
        var raw = principal?.FindFirstValue(claimType);
        return Guid.TryParse(raw, out var value) ? value : null;
    }
}
