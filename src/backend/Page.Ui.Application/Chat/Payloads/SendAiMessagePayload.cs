using Page.Ui.Domain.Chat.Entities;

namespace Page.Ui.Application.Chat.Payloads;

public sealed record SendAiMessagePayload(
    Message UserMessage,
    Message? AiMessage
);
