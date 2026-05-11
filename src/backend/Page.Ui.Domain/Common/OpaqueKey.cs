using System.Security.Cryptography;
using System.Text;

namespace Page.Ui.Domain.Common;

public static class OpaqueKey
{
    private const int DefaultLength = 24;

    public static string FromGuid(Guid id, int length = DefaultLength)
    {
        var hash = SHA256.HashData(id.ToByteArray());
        return Convert.ToHexString(hash)[..length].ToLowerInvariant();
    }

    public static string FromString(string value, int length = DefaultLength)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash)[..length].ToLowerInvariant();
    }
}
