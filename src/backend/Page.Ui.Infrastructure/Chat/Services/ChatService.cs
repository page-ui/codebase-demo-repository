using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minio;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Application.Chat.Inputs;
using Page.Ui.Application.Chat.Payloads;
using Page.Ui.Application.Chat.Services;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Domain.Chat;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Domain.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using NpgsqlTypes;
using StackExchange.Redis;
using System.Text.Json;

namespace Page.Ui.Infrastructure.Chat.Services;

public class ChatService : IChatService
{
    private const int ReadRequestsPerWindow = 60;
    private static readonly TimeSpan ReadWindow = TimeSpan.FromSeconds(10);
    private const int WriteRequestsPerWindow = 30;
    private static readonly TimeSpan WriteWindow = TimeSpan.FromSeconds(10);
    private const int CreateChatRequestsPerWindow = 8;
    private static readonly TimeSpan CreateChatWindow = TimeSpan.FromMinutes(1);
    private const int RenameRequestsPerWindow = 10;
    private static readonly TimeSpan RenameWindow = TimeSpan.FromSeconds(30);
    private const int DeleteRequestsPerWindow = 5;
    private static readonly TimeSpan DeleteWindow = TimeSpan.FromMinutes(1);
    private const int ReportErrorRequestsPerWindow = 10;
    private static readonly TimeSpan ReportErrorWindow = TimeSpan.FromSeconds(30);
    private const int MaxRenderDiagnosticEntries = 50;
    private const int MaxRenderDiagnosticEntryLength = 1_000;
    private const int MaxRenderDiagnosticSerializedLength = 8 * 1024;

    private readonly IApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IConnectionMultiplexer _redis;
    private readonly IMinioClient _minioClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IApplicationDbContext context,
        IPublishEndpoint publishEndpoint,
        IConnectionMultiplexer redis,
        IMinioClient minioClient,
        ILogger<ChatService> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _redis = redis;
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task<CreateChatPayload> CreateChatAsync(CreateChatInput input, string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authenticated user identifier is required.");
        }

        ChatRateLimitGuard.EnforceWrite(_redis, userId, "create-chat", CreateChatRequestsPerWindow, CreateChatWindow);
        var db = _redis.GetDatabase();
        var lockKey = $"lock:chat:create:{userId}";
        var lockToken = GenerateServerGeneratedId();
        var lockAcquired = await db.LockTakeAsync(lockKey, lockToken, TimeSpan.FromSeconds(10));

        if (!lockAcquired)
        {
            throw new InvalidOperationException("A createChat request is already in progress for this user. Please retry.");
        }

        try
        {
            var sanitizedInput = await SanitizeCreateChatInputAsync(input, cancellationToken);

            var caller = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (caller is null)
            {
                throw new UnauthorizedAccessException("Authenticated user was not found.");
            }

            await EnsureCanonicalAiBotUserExistsAsync(cancellationToken);

            var dbContext = GetDbContext();
            Message initialMessage;
            Page.Ui.Domain.Chat.Entities.Chat chat;

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var now = DateTimeOffset.UtcNow;
                var chatId = Guid.NewGuid();
                var chatName = string.IsNullOrWhiteSpace(sanitizedInput.Name) ? "New Chat" : sanitizedInput.Name;

                chat = new Page.Ui.Domain.Chat.Entities.Chat
                {
                    Id = chatId,
                    OwnerUserId = caller.Id,
                    Name = chatName,
                    ChatKey = OpaqueKey.FromGuid(chatId),
                    ModelId = ChatConstants.AiModelId,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync(cancellationToken);

                initialMessage = new Message
                {
                    ChatId = chat.Id,
                    SenderId = caller.Id,
                    Title = DeriveMessageTitle(chat.Name),
                    Content = sanitizedInput.InitialUserMessage.Content,
                    AttachmentUrl = sanitizedInput.InitialUserMessage.AttachmentUrl,
                    Type = Page.Ui.Domain.Chat.Enums.MessageType.UserMessage,
                    Status = Page.Ui.Domain.Chat.Enums.MessageStatus.Sent,
                    ServerGeneratedId = GenerateServerGeneratedId(),
                    CreatedAt = now
                };
                initialMessage.MessageKey = OpaqueKey.FromGuid(initialMessage.Id);

                _context.Messages.Add(initialMessage);
                await PublishMessageCreatedAsync(chat, initialMessage, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            var persistedChat = await LoadChatByIdAsync(chat.Id, cancellationToken)
                ?? throw new InvalidOperationException("Persisted chat could not be loaded after creation.");

            _logger.LogInformation(
                "Created user-ai chat {ChatId} for caller {CallerId} with model {ModelId}",
                persistedChat.Id,
                caller.Id,
                ChatConstants.AiModelId);

            return new CreateChatPayload(persistedChat, initialMessage);
        }
        finally
        {
            try
            {
                await db.LockReleaseAsync(lockKey, lockToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release createChat lock for caller {CallerId}", userId);
            }
        }
    }

    public async Task<Message> CreateMessageAsync(CreateMessageInput input, string userId, CancellationToken cancellationToken)
    {
        ChatRateLimitGuard.EnforceWrite(_redis, userId, "message", WriteRequestsPerWindow, WriteWindow);

        var chat = await _context.Chats
            .FirstOrDefaultAsync(c => c.ChatKey == input.ChatKey && c.OwnerUserId == userId, cancellationToken);

        if (chat is null)
        {
            throw new UnauthorizedAccessException("Chat was not found or access is denied.");
        }

        var hasActiveRun = await _context.AiRuns
            .AnyAsync(r => r.ChatId == chat.Id &&
                           r.IsCurrent &&
                           r.Status != Page.Ui.Domain.Chat.Enums.AiRunStatus.Completed &&
                           r.Status != Page.Ui.Domain.Chat.Enums.AiRunStatus.Failed,
                      cancellationToken);

        if (hasActiveRun)
        {
            throw new InvalidOperationException("Please wait for the AI to finish responding before sending another message.");
        }

        var sanitizedClientRequestId = ChatServiceFields.SanitizeOptionalField(input.ClientRequestId, ChatServiceFields.MaxClientRequestIdLength, nameof(input.ClientRequestId));
        if (!string.IsNullOrWhiteSpace(sanitizedClientRequestId))
        {
            var existingMessage = await _context.Messages
                .FirstOrDefaultAsync(
                    m => m.ChatId == chat.Id &&
                         m.SenderId == userId &&
                         m.ClientRequestId == sanitizedClientRequestId,
                    cancellationToken);

            if (existingMessage is not null)
            {
                return existingMessage;
            }
        }

        Guid? replyToId = null;
        if (!string.IsNullOrWhiteSpace(input.ReplyToKey))
        {
            var replyToMessage = await _context.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MessageKey == input.ReplyToKey && m.ChatId == chat.Id, cancellationToken);
            replyToId = replyToMessage?.Id;
        }

        var sanitizedAttachmentUrl = await ChatAttachmentUrlValidator.ValidateAsync(_minioClient, input.AttachmentUrl, cancellationToken);
        var sanitizedContent = ChatServiceFields.SanitizeRequiredField(input.Content, ChatServiceFields.MaxMessageContentLength, nameof(input.Content));
        var messageType = input.Type ?? MessageType.UserMessage;
        var message = new Message
        {
            ChatId = chat.Id,
            SenderId = userId,
            Title = DeriveMessageTitle(chat.Name),
            Content = sanitizedContent,
            IsQuestion = input.IsQuestion,
            Type = messageType,
            ReplyToId = replyToId,
            AttachmentUrl = sanitizedAttachmentUrl,
            ClientRequestId = sanitizedClientRequestId,
            ServerGeneratedId = GenerateServerGeneratedId(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        message.MessageKey = OpaqueKey.FromGuid(message.Id);

        _context.Messages.Add(message);
        await PublishMessageCreatedAsync(chat, message, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (!string.IsNullOrWhiteSpace(sanitizedClientRequestId) && IsUniqueConstraintViolation(ex))
        {
            DetachEntity(message);
            var existingMessage = await FindExistingClientRequestMessageAsync(chat.Id, userId, sanitizedClientRequestId, cancellationToken);
            if (existingMessage is not null)
            {
                return existingMessage;
            }

            throw;
        }

        return message;
    }

    public async Task<Page.Ui.Domain.Chat.Entities.Chat?> GetChatAsync(string chatKey, string userId, CancellationToken cancellationToken)
    {
        ChatRateLimitGuard.EnforceRead(_redis, userId, "get-chat", ReadRequestsPerWindow, ReadWindow);

        return await _context.Chats
            .AsNoTracking()
            .Include(c => c.OwnerUser)
            .FirstOrDefaultAsync(c => c.ChatKey == chatKey && c.OwnerUserId == userId, cancellationToken);
    }

    public IQueryable<Page.Ui.Domain.Chat.Entities.Chat> GetChats(string userId)
    {
        ChatRateLimitGuard.EnforceRead(_redis, userId, "list-chats", ReadRequestsPerWindow, ReadWindow);

        return _context.Chats
            .AsNoTracking()
            .Where(c => c.OwnerUserId == userId);
    }

    public IQueryable<Page.Ui.Domain.Chat.Entities.Chat> SearchChats(string nameQuery, string userId)
    {
        ChatRateLimitGuard.EnforceRead(_redis, userId, "search-chats", ReadRequestsPerWindow, ReadWindow);

        var trimmedQuery = nameQuery.Trim();
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return GetChats(userId);
        }

        var searchPattern = $"%{trimmedQuery}%";

        return _context.Chats
            .AsNoTracking()
            .Where(c => c.OwnerUserId == userId)
            .Where(c => c.Name != null && EF.Functions.ILike(c.Name, searchPattern));
    }

    public IQueryable<Message> GetMessages(string chatKey, string userId)
    {
        ChatRateLimitGuard.EnforceRead(_redis, userId, "messages", ReadRequestsPerWindow, ReadWindow);

        return _context.Messages
            .AsNoTracking()
            .Include(m => m.Chat)
            .Include(m => m.ReplyTo)
            .Where(m => m.Chat.ChatKey == chatKey && m.Chat.OwnerUserId == userId);
    }

    public IQueryable<Message> SearchMessages(string query, string? chatKey, string userId)
    {
        ChatRateLimitGuard.EnforceRead(_redis, userId, "search", ReadRequestsPerWindow, ReadWindow);

        var baseQuery = _context.Messages
            .AsNoTracking()
            .Include(m => m.Chat)
            .Include(m => m.ReplyTo)
            .Where(m => m.Chat.OwnerUserId == userId);

        if (!string.IsNullOrWhiteSpace(chatKey))
        {
            baseQuery = baseQuery.Where(m => m.Chat.ChatKey == chatKey);
        }

        return baseQuery
            .Where(m => EF.Property<NpgsqlTsVector>(m, "SearchVector").Matches(query));
    }

    public async Task<Page.Ui.Domain.Chat.Entities.Chat> RenameChatAsync(string chatKey, string name, string userId, CancellationToken cancellationToken)
    {
        ChatRateLimitGuard.EnforceWrite(_redis, userId, "rename", RenameRequestsPerWindow, RenameWindow);

        var chat = await FindOwnedChatByKeyAsync(chatKey, userId, cancellationToken);
        if (chat is null)
        {
            throw new UnauthorizedAccessException("Chat was not found or access is denied.");
        }

        chat.Name = ChatServiceFields.SanitizeRequiredField(name, ChatServiceFields.MaxChatNameLength, nameof(name));
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return chat;
    }

    public async Task<bool> DeleteChatAsync(string chatKey, string userId, CancellationToken cancellationToken)
    {
        ChatRateLimitGuard.EnforceWrite(_redis, userId, "delete", DeleteRequestsPerWindow, DeleteWindow);

        var chat = await _context.Chats
            .FirstOrDefaultAsync(c => c.ChatKey == chatKey && c.OwnerUserId == userId, cancellationToken);

        if (chat is null)
        {
            throw new UnauthorizedAccessException("Chat was not found or access is denied.");
        }

        _context.Chats.Remove(chat);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReportRenderErrorAsync(ReportRenderErrorInput input, string userId, CancellationToken cancellationToken)
    {
        ChatRateLimitGuard.EnforceWrite(_redis, userId, "report-error", ReportErrorRequestsPerWindow, ReportErrorWindow);
        var sanitizedErrors = SanitizeRenderDiagnostics(input.Errors);
        var sanitizedLogs = SanitizeRenderDiagnostics(input.Logs);

        if (sanitizedErrors.Count == 0 && sanitizedLogs.Count == 0)
        {
            throw new InvalidOperationException("At least one render error or log entry is required.");
        }

        var chat = await _context.Chats
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChatKey == input.ChatKey && c.OwnerUserId == userId, cancellationToken);

        if (chat is null)
        {
            throw new UnauthorizedAccessException("Chat was not found or access is denied.");
        }

        var query = _context.AiRuns.Where(r => r.ChatId == chat.Id);
        if (input.VersionId.HasValue)
        {
            query = query.Where(r => r.VersionId == input.VersionId.Value);
        }
        else
        {
            query = query.Where(r => r.IsCurrent);
        }

        var run = await query.OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (run is null)
        {
            return false;
        }

        if (sanitizedErrors.Count > 0)
        {
            run.ClientErrors = SerializeBoundedDiagnostics(sanitizedErrors);
        }
        if (sanitizedLogs.Count > 0)
        {
            run.ClientLogs = SerializeBoundedDiagnostics(sanitizedLogs);
        }

        run.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _publishEndpoint.Publish(new RenderErrorReported(
            chat.Id,
            run.VersionId,
            userId,
            sanitizedErrors,
            sanitizedLogs
        ), cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private DbContext GetDbContext()
    {
        if (_context is DbContext dbContext)
        {
            return dbContext;
        }

        throw new InvalidOperationException("IApplicationDbContext must also be a DbContext.");
    }

    private async Task EnsureCanonicalAiBotUserExistsAsync(CancellationToken cancellationToken)
    {
        var aiExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == ChatConstants.AiBotUserId, cancellationToken);

        if (aiExists)
        {
            return;
        }

        _context.Users.Add(new ApplicationUser
        {
            Id = ChatConstants.AiBotUserId,
            UserName = "ai-bot-system",
            NormalizedUserName = "AI-BOT-SYSTEM",
            Email = "aibot@system.local",
            NormalizedEmail = "AIBOT@SYSTEM.LOCAL",
            EmailConfirmed = true,
            Name = "AI Assistant",
            SecurityStamp = "AI_BOT_SYSTEM_SECURITY_STAMP"
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var nowExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == ChatConstants.AiBotUserId, cancellationToken);

            if (!nowExists)
            {
                throw;
            }
        }
    }

    private async Task<Page.Ui.Domain.Chat.Entities.Chat?> LoadChatByIdAsync(Guid chatId, CancellationToken cancellationToken)
    {
        return await _context.Chats
            .Include(c => c.OwnerUser)
            .FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
    }

    private async Task<Message?> FindExistingClientRequestMessageAsync(Guid chatId, string userId, string clientRequestId, CancellationToken cancellationToken)
    {
        return await _context.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.ChatId == chatId &&
                     m.SenderId == userId &&
                     m.ClientRequestId == clientRequestId,
                cancellationToken);
    }

    private void DetachEntity(object entity)
    {
        if (_context is not DbContext dbContext)
        {
            return;
        }

        EntityEntry entry = dbContext.Entry(entity);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private async Task<Page.Ui.Domain.Chat.Entities.Chat?> FindOwnedChatByKeyAsync(string chatKey, string userId, CancellationToken cancellationToken)
    {
        return await _context.Chats
            .Include(c => c.OwnerUser)
            .FirstOrDefaultAsync(c => c.ChatKey == chatKey && c.OwnerUserId == userId, cancellationToken);
    }

    private async Task PublishMessageCreatedAsync(Page.Ui.Domain.Chat.Entities.Chat chat, Message message, CancellationToken cancellationToken)
    {
        await _publishEndpoint.Publish(new ChatMessageCreated(
            message.Id,
            message.ChatId,
            chat.ChatKey,
            message.MessageKey,
            message.SenderId,
            message.Title,
            message.Content,
            message.Type,
            message.CreatedAt,
            message.Status,
            message.AttachmentUrl,
            message.ServerGeneratedId,
            message.ReplyToId,
            message.IsQuestion
        ), cancellationToken);
    }

    private async Task<SanitizedCreateChatInput> SanitizeCreateChatInputAsync(CreateChatInput input, CancellationToken cancellationToken)
    {
        var name = ChatServiceFields.SanitizeOptionalField(input.Name, ChatServiceFields.MaxChatNameLength, nameof(input.Name));
        var initialUserMessageInput = input.InitialUserMessage
            ?? throw new InvalidOperationException("initialUserMessage is required.");
        var content = ChatServiceFields.SanitizeRequiredField(initialUserMessageInput.Content, ChatServiceFields.MaxMessageContentLength, "initialUserMessage.content");
        var attachmentUrl = await ChatAttachmentUrlValidator.ValidateAsync(_minioClient, initialUserMessageInput.AttachmentUrl, cancellationToken);
        var initialUserMessage = new SanitizedInitialUserMessage(content, attachmentUrl);

        return new SanitizedCreateChatInput(name, initialUserMessage);
    }

    private sealed record SanitizedCreateChatInput(
        string? Name,
        SanitizedInitialUserMessage InitialUserMessage);

    private sealed record SanitizedInitialUserMessage(
        string Content,
        string? AttachmentUrl);

    private static string GenerateServerGeneratedId()
    {
        return UlidGenerator.NewUlid();
    }

    private static List<string> SanitizeRenderDiagnostics(IEnumerable<string>? entries)
    {
        return (entries ?? Array.Empty<string>())
            .Select(SanitizeRenderDiagnosticEntry)
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Cast<string>()
            .Take(MaxRenderDiagnosticEntries)
            .ToList();
    }

    private static string? SanitizeRenderDiagnosticEntry(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return null;
        }

        var normalized = entry.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Trim();

        normalized = new string(normalized
            .Select(ch => char.IsControl(ch) && ch != '\n' ? ' ' : ch)
            .ToArray());

        return normalized.Length <= MaxRenderDiagnosticEntryLength
            ? normalized
            : normalized[..MaxRenderDiagnosticEntryLength];
    }

    private static string SerializeBoundedDiagnostics(List<string> entries)
    {
        var boundedEntries = entries.ToList();
        while (boundedEntries.Count > 0)
        {
            var json = JsonSerializer.Serialize(boundedEntries);
            if (json.Length <= MaxRenderDiagnosticSerializedLength)
            {
                return json;
            }

            boundedEntries.RemoveAt(boundedEntries.Count - 1);
        }

        return "[]";
    }

    private static string DeriveMessageTitle(string? chatName)
    {
        return ChatServiceFields.SanitizeOptionalField(chatName, ChatServiceFields.MaxMessageTitleLength, nameof(chatName))
            ?? string.Empty;
    }
}
