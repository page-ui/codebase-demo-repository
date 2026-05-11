namespace Page.Ui.Presentation.Auth.GraphQl.Support;

internal static class AuthInputGuard
{
    private static readonly string[] DangerousPatterns = ["..", "/", "\\", "%", "0x", "<", ">", "|", "&"];

    public static string NormalizeEmail(string email)
    {
        return email.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
    }

    public static string Normalize(string value)
    {
        return value.Trim();
    }

    public static bool IsSafe(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        foreach (var pattern in DangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
