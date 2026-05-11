namespace Page.Ui.Presentation.Chat.GraphQl.Views;

public sealed record CreateChatPayloadView(
    ChatView Chat,
    MessageView? InitialMessage);
