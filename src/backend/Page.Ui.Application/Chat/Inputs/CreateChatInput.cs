namespace Page.Ui.Application.Chat.Inputs;

public sealed class CreateChatInput
{
    public string? Name { get; init; }
    public required InitialUserMessageInput InitialUserMessage { get; init; }
}

public sealed class InitialUserMessageInput
{
    public required string Content { get; init; }
    public string? AttachmentUrl { get; init; }
}
