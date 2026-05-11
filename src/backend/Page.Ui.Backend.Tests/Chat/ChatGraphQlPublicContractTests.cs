using System.Reflection;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Presentation.Chat.GraphQl.Inputs;
using Page.Ui.Presentation.Chat.GraphQl.Mutations;
using Page.Ui.Presentation.Chat.GraphQl.Queries;
using Page.Ui.Presentation.Chat.GraphQl.Subscriptions;
using Page.Ui.Presentation.Chat.GraphQl.Views;

namespace Page.Ui.Backend.Tests.Chat;

public sealed class ChatGraphQlPublicContractTests
{
    [Fact]
    public async Task PublicGraphQlSchema_DoesNotExposeInternalChatIdentifiers()
    {
        var services = new ServiceCollection();
        services
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query"))
                .AddTypeExtension<ChatQueries>()
            .AddMutationType(d => d.Name("Mutation"))
                .AddTypeExtension<ChatMutations>()
            .AddSubscriptionType(d => d.Name("Subscription"))
                .AddTypeExtension<ChatSubscriptions>()
            .AddType<ChatView>()
            .AddType<MessageView>()
            .AddType<CreateChatPayloadView>()
            .AddAuthorization()
            .AddFiltering()
            .AddSorting();

        var executor = await services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

        var schema = executor.Schema.ToString();

        Assert.Contains("type ChatView", schema);
        Assert.Contains("type MessageView", schema);
        Assert.Contains("type CreateChatPayloadView", schema);
        Assert.DoesNotContain("type Chat ", schema);
        Assert.DoesNotContain("type Message ", schema);
        Assert.DoesNotContain("type ChatMessageCreated", schema);
        Assert.DoesNotContain("ownerUserId", schema);
        Assert.DoesNotContain("senderId", schema);
        Assert.DoesNotContain("chatId", schema);
        Assert.DoesNotContain("replyToId", schema);
        Assert.DoesNotContain("clientRequestId", schema);
        Assert.DoesNotContain("serverGeneratedId", schema);
    }

    [Fact]
    public void PublicChatView_OnlyExposesSafeFields()
    {
        AssertPublicProperties<ChatView>(
            "ChatKey",
            "Name",
            "ModelId",
            "CreatedAt",
            "UpdatedAt");
    }

    [Fact]
    public void PublicMessageView_OnlyExposesSafeFields()
    {
        AssertPublicProperties<MessageView>(
            "MessageKey",
            "ChatKey",
            "Title",
            "Content",
            "IsQuestion",
            "Type",
            "Status",
            "CreatedAt",
            "UpdatedAt",
            "ReplyToKey",
            "AttachmentUrl",
            "SenderType");
    }

    [Fact]
    public void CreateMessageInput_DoesNotExposeClientRequestId()
    {
        AssertPublicProperties<PublicCreateMessageInput>(
            "ChatKey",
            "Content",
            "ReplyToKey",
            "AttachmentUrl",
            "Type",
            "IsQuestion");
    }

    [Fact]
    public void CreateMessageInput_MapsOptionalType()
    {
        var input = new PublicCreateMessageInput(
            "chat-key",
            "hello",
            null,
            null,
            MessageType.AiMessage,
            true);

        Assert.Equal(MessageType.AiMessage, input.ToApplicationInput().Type);
        Assert.True(input.ToApplicationInput().IsQuestion);
    }

    [Fact]
    public void ChatResolvers_ReturnPublicDtos()
    {
        Assert.Equal(typeof(Task<CreateChatPayloadView>), GetMethod<ChatMutations>(nameof(ChatMutations.CreateChat)).ReturnType);
        Assert.Equal(typeof(Task<MessageView>), GetMethod<ChatMutations>(nameof(ChatMutations.CreateMessage)).ReturnType);
        Assert.Equal(typeof(Task<ChatView>), GetMethod<ChatMutations>(nameof(ChatMutations.RenameChat)).ReturnType);
        Assert.Equal(typeof(IQueryable<ChatView>), GetMethod<ChatQueries>(nameof(ChatQueries.GetChat)).ReturnType);
        Assert.Equal(typeof(IQueryable<ChatView>), GetMethod<ChatQueries>(nameof(ChatQueries.GetChats)).ReturnType);
        Assert.Equal(typeof(IQueryable<MessageView>), GetMethod<ChatQueries>(nameof(ChatQueries.GetMessages)).ReturnType);
        Assert.Equal(typeof(Task<MessageView>), GetMethod<ChatSubscriptions>(nameof(ChatSubscriptions.OnMessageCreated)).ReturnType);
    }

    [Fact]
    public void CreateMessageResolver_UsesPublicInput()
    {
        var inputParameter = GetMethod<ChatMutations>(nameof(ChatMutations.CreateMessage))
            .GetParameters()
            .Single(parameter => parameter.Name == "input");

        Assert.Equal(typeof(PublicCreateMessageInput), inputParameter.ParameterType);
    }

    private static MethodInfo GetMethod<T>(string name)
    {
        return typeof(T).GetMethod(name)
            ?? throw new InvalidOperationException($"{typeof(T).Name}.{name} was not found.");
    }

    private static void AssertPublicProperties<T>(params string[] expectedProperties)
    {
        var actualProperties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(expectedProperties.OrderBy(name => name), actualProperties);
    }
}
