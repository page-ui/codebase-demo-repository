using System.Numerics;
using System.Security.Cryptography;

namespace Page.Ui.Domain.Chat;

public static class UlidGenerator
{
    private const string Base32Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string NewUlid()
    {
        Span<byte> bytes = stackalloc byte[16];
        var timestampMs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        bytes[0] = (byte)(timestampMs >> 40);
        bytes[1] = (byte)(timestampMs >> 32);
        bytes[2] = (byte)(timestampMs >> 24);
        bytes[3] = (byte)(timestampMs >> 16);
        bytes[4] = (byte)(timestampMs >> 8);
        bytes[5] = (byte)timestampMs;

        RandomNumberGenerator.Fill(bytes[6..]);

        var value = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
        Span<char> encoded = stackalloc char[26];

        for (var i = encoded.Length - 1; i >= 0; i--)
        {
            var alphabetIndex = (int)(value % 32);
            encoded[i] = Base32Alphabet[alphabetIndex];
            value /= 32;
        }

        return new string(encoded);
    }
}
