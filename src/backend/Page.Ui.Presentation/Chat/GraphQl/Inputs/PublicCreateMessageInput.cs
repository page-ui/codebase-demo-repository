using HotChocolate;
using Page.Ui.Application.Chat.Inputs;
using Page.Ui.Domain.Chat.Enums;

namespace Page.Ui.Presentation.Chat.GraphQl.Inputs;

[GraphQLName("CreateMessageInput")]
public sealed record PublicCreateMessageInput(
    string ChatKey,
    string Content,
    string? ReplyToKey,
    string? AttachmentUrl,
    MessageType? Type = null,
    bool IsQuestion = false)
{
    public CreateMessageInput ToApplicationInput()
    {
        return new CreateMessageInput(ChatKey, Content, ReplyToKey, AttachmentUrl, Type: Type, IsQuestion: IsQuestion);
    }
}
