namespace Page.Ui.Application.Chat.Contracts;

public record RenderErrorReported(
    Guid ChatId,
    Guid VersionId,
    string UserId,
    List<string> Errors,
    List<string> Logs);
