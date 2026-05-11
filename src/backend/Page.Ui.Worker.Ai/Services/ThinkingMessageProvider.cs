using System.Text.Json;
using System.Text.Json.Serialization;

namespace Page.Ui.Worker.Ai.Services;

public class ThinkingMessageProvider : IThinkingMessageProvider
{
    private readonly List<string> _messages;
    private readonly Random _random = new();
    private int? _lastMessageIndex;

    public ThinkingMessageProvider()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "thinking_loading_messages.json");
        
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "thinking_loading_messages.json");
        }

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<ThinkingMessagesData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                _messages = NormalizeMessages(data?.LoadingMessages);
            }
            catch
            {
                _messages = ThinkingLoadingMessages.Defaults.ToList();
            }
        }
        else
        {
            _messages = ThinkingLoadingMessages.Defaults.ToList();
        }
        
        if (_messages.Count == 0)
        {
            _messages.AddRange(ThinkingLoadingMessages.Defaults);
        }
    }

    public ThinkingMessageProvider(IEnumerable<string> messages)
    {
        _messages = NormalizeMessages(messages);
    }

    public string GetRandomMessage()
    {
        lock (_random)
        {
            var index = _random.Next(_messages.Count);
            if (_messages.Count > 1 && index == _lastMessageIndex)
            {
                index = (index + 1) % _messages.Count;
            }

            _lastMessageIndex = index;
            return _messages[index];
        }
    }

    private class ThinkingMessagesData
    {
        [JsonPropertyName("loading_messages")]
        public List<string> LoadingMessages { get; set; } = new();
    }

    private static List<string> NormalizeMessages(IEnumerable<string>? messages)
    {
        var normalized = messages?
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        return normalized.Count > 0
            ? normalized
            : ThinkingLoadingMessages.Defaults.ToList();
    }
}
