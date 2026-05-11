namespace Page.Ui.Presentation.Chat.GraphQl.Views;

public sealed record ChatView
{
    public string ChatKey { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string ModelId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
