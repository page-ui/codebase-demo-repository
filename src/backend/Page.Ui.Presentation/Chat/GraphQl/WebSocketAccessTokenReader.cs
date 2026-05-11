using System.Text.Json;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Http;

namespace Page.Ui.Presentation.Chat.GraphQl;

internal static class WebSocketAccessTokenReader
{
    private const string AccessTokenKey = "access_token";
    private const string HeadersKey = "headers";

    public static bool TryRead(IOperationMessagePayload payload, out string token)
    {
        token = string.Empty;

        return TryReadFromJsonPayload(payload, out token)
            || TryReadFromStringPayload(payload, out token)
            || TryReadFromObjectPayload(payload, out token);
    }

    public static bool TryRead(IQueryCollection query, out string token)
    {
        token = query[AccessTokenKey].ToString();
        return !string.IsNullOrWhiteSpace(token);
    }

    public static string Normalize(string token)
    {
        return token.Trim().Trim('"');
    }

    private static bool TryReadFromJsonPayload(IOperationMessagePayload payload, out string token)
    {
        token = string.Empty;

        IReadOnlyDictionary<string, JsonElement>? map;
        try
        {
            map = payload.As<IReadOnlyDictionary<string, JsonElement>>();
        }
        catch
        {
            return false;
        }

        if (map is null)
        {
            return false;
        }

        return TryReadFromJsonMap(map, out token)
            || TryReadHeadersJsonMap(map, out token);
    }

    private static bool TryReadFromStringPayload(IOperationMessagePayload payload, out string token)
    {
        token = string.Empty;

        IReadOnlyDictionary<string, string>? map;
        try
        {
            map = payload.As<IReadOnlyDictionary<string, string>>();
        }
        catch
        {
            return false;
        }

        if (map is null)
        {
            return false;
        }

        if (map.TryGetValue(AccessTokenKey, out var directToken) && !string.IsNullOrWhiteSpace(directToken))
        {
            token = directToken;
            return true;
        }

        if (!map.TryGetValue(HeadersKey, out var headersRaw) || string.IsNullOrWhiteSpace(headersRaw))
        {
            token = string.Empty;
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(headersRaw);
            return doc.RootElement.ValueKind == JsonValueKind.Object &&
                TryReadFromJsonElement(doc.RootElement, out token);
        }
        catch
        {
            token = string.Empty;
            return false;
        }
    }

    private static bool TryReadFromObjectPayload(IOperationMessagePayload payload, out string token)
    {
        token = string.Empty;

        IReadOnlyDictionary<string, object?>? map;
        try
        {
            map = payload.As<IReadOnlyDictionary<string, object?>>();
        }
        catch
        {
            return false;
        }

        if (map is null)
        {
            return false;
        }

        if (TryReadFromObjectMap(map, out token))
        {
            return true;
        }

        if (!map.TryGetValue(HeadersKey, out var headersRaw) || headersRaw is null)
        {
            return false;
        }

        return headersRaw switch
        {
            JsonElement headersJson when headersJson.ValueKind == JsonValueKind.Object => TryReadFromJsonElement(headersJson, out token),
            IReadOnlyDictionary<string, object?> headersMap => TryReadFromObjectMap(headersMap, out token),
            _ => false
        };
    }

    private static bool TryReadHeadersJsonMap(IReadOnlyDictionary<string, JsonElement> map, out string token)
    {
        token = string.Empty;

        return map.TryGetValue(HeadersKey, out var headers)
            && headers.ValueKind == JsonValueKind.Object
            && TryReadFromJsonElement(headers, out token);
    }

    private static bool TryReadFromJsonMap(IReadOnlyDictionary<string, JsonElement> map, out string token)
    {
        token = string.Empty;

        return map.TryGetValue(AccessTokenKey, out var value)
            && TryReadTokenValue(value, out token);
    }

    private static bool TryReadFromObjectMap(IReadOnlyDictionary<string, object?> map, out string token)
    {
        token = string.Empty;

        return map.TryGetValue(AccessTokenKey, out var value)
            && value is not null
            && TryReadTokenValue(value, out token);
    }

    private static bool TryReadFromJsonElement(JsonElement element, out string token)
    {
        token = string.Empty;

        return element.TryGetProperty(AccessTokenKey, out var value)
            && TryReadTokenValue(value, out token);
    }

    private static bool TryReadTokenValue(object value, out string token)
    {
        token = value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString() ?? string.Empty,
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(token);
    }

    private static bool TryReadTokenValue(JsonElement value, out string token)
    {
        token = value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

        return !string.IsNullOrWhiteSpace(token);
    }
}
