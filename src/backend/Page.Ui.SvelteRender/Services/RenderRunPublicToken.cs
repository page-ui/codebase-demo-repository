using System.Security.Cryptography;
using System.Text;

namespace Page.Ui.SvelteRender.Services;

internal static class RenderRunPublicToken
{
    private const string UserStorageKeyMetadataKey = "userStorageKey";
    private const string ChatKeyMetadataKey = "chatKey";
    private const string VersionIdMetadataKey = "versionId";

    public static string FromRunId(string runId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"page-ui-run::{runId}"));
        return $"run_pub_{Convert.ToHexString(hash)[..24].ToLowerInvariant()}";
    }

    public static string FromRunContext(string runId, IReadOnlyDictionary<string, string> metadata)
    {
        var userStorageKey = GetMetadataValue(metadata, UserStorageKeyMetadataKey);
        var chatKey = GetMetadataValue(metadata, ChatKeyMetadataKey);
        var versionId = GetMetadataValue(metadata, VersionIdMetadataKey);
        if (string.IsNullOrWhiteSpace(userStorageKey) ||
            string.IsNullOrWhiteSpace(chatKey) ||
            string.IsNullOrWhiteSpace(versionId))
        {
            return FromRunId(runId);
        }

        var tokenPayload = string.Join(
            "::",
            "page-ui-run",
            userStorageKey,
            chatKey,
            versionId,
            runId);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tokenPayload));
        return $"run_pub_{Convert.ToHexString(hash)[..24].ToLowerInvariant()}";
    }

    public static string BuildPublicBasePath(string runId, IReadOnlyDictionary<string, string> metadata)
    {
        return $"/runs/{FromRunContext(runId, metadata)}";
    }

    private static string? GetMetadataValue(IReadOnlyDictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }
}
