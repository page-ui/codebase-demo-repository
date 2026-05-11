namespace Page.Ui.Application.Chat.Payloads;

public sealed record CreateChatPayload(
    Page.Ui.Domain.Chat.Entities.Chat Chat,
    Page.Ui.Domain.Chat.Entities.Message? InitialMessage
);
