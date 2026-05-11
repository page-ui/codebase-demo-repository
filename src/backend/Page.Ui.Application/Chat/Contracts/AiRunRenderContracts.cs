namespace Page.Ui.Application.Chat.Contracts;

public record TriggerAiRunRender(
    Guid ChatId,
    string ChatKey,
    Guid ReplyToMessageId,
    Guid RunId,
    Guid VersionId,
    string UserStorageKey,
    IReadOnlyList<AiSourceFileDto> Files
);

public record AiSourceFileDto(
    string FileName,
    string ContentType,
    string ObjectKey
);
