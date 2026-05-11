namespace Page.Ui.SvelteRender.Services;

internal static class RenderRunPath
{
    private const string SharedUserSegment = "_shared";
    private const string SharedChatSegment = "_shared-chat";

    public static string GetUserSegment(IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("userStorageKey", out var userStorageKey) && !string.IsNullOrWhiteSpace(userStorageKey))
        {
            return SanitizePathSegment(userStorageKey);
        }

        if (metadata.TryGetValue("userId", out var userId) && !string.IsNullOrWhiteSpace(userId))
        {
            return SanitizePathSegment(userId);
        }

        return SharedUserSegment;
    }

    public static string GetChatSegment(IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("chatKey", out var chatKey) && !string.IsNullOrWhiteSpace(chatKey))
        {
            return SanitizePathSegment(chatKey);
        }

        if (metadata.TryGetValue("chatId", out var chatId) && !string.IsNullOrWhiteSpace(chatId))
        {
            return SanitizePathSegment(chatId);
        }

        return SharedChatSegment;
    }

    public static string GetVersionSegment(IReadOnlyDictionary<string, string> metadata, string runId)
    {
        if (metadata.TryGetValue("versionId", out var versionId) && !string.IsNullOrWhiteSpace(versionId))
        {
            return SanitizePathSegment(versionId);
        }

        return SanitizePathSegment(runId);
    }

    public static string GetRelativeRunPath(IReadOnlyDictionary<string, string> metadata, string runId)
    {
        var userSegment = GetUserSegment(metadata);
        var chatSegment = GetChatSegment(metadata);
        var versionSegment = GetVersionSegment(metadata, runId);
        return $"{userSegment}/{chatSegment}/{versionSegment}";
    }

    public static string GetPhysicalRunPath(string contentRootPath, string runsDirectory, IReadOnlyDictionary<string, string> metadata, string runId)
    {
        var runsRoot = RenderPathGuard.GetContainedRootPath(contentRootPath, runsDirectory);
        return RenderPathGuard.GetContainedPath(
            runsRoot,
            GetUserSegment(metadata),
            GetChatSegment(metadata),
            GetVersionSegment(metadata, runId));
    }

    public static string GetPublicRunBasePath(IReadOnlyDictionary<string, string> metadata, string runId)
    {
        return RenderRunPublicToken.BuildPublicBasePath(runId, metadata);
    }

    private static string SanitizePathSegment(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return SharedUserSegment;
        }

        Span<char> buffer = stackalloc char[trimmed.Length];
        var index = 0;

        foreach (var ch in trimmed)
        {
            buffer[index++] = ch switch
            {
                _ when char.IsLetterOrDigit(ch) => ch,
                '-' or '_' or '.' => ch,
                _ => '_'
            };
        }

        var sanitized = new string(buffer[..index]).Trim(' ', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? SharedUserSegment : sanitized;
    }
}
