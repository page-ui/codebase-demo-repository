using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Page.Ui.Infrastructure.Chat.Services;

internal static class ChatAttachmentUrlValidator
{
    private const string ChatUploadsBucketName = "chat-uploads";
    private const string MinioProxyPrefix = "/minio/";

    public static async Task<string?> ValidateAsync(
        IMinioClient minioClient,
        string? attachmentUrl,
        CancellationToken cancellationToken)
    {
        var sanitized = ChatServiceFields.SanitizeOptionalField(attachmentUrl, ChatServiceFields.MaxAttachmentUrlLength, nameof(attachmentUrl));
        if (sanitized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(sanitized, UriKind.Absolute, out var attachmentUri))
        {
            throw new InvalidOperationException("attachmentUrl must be an absolute URL.");
        }

        if (!TryExtractChatUploadObjectName(attachmentUri, out var objectName))
        {
            throw new InvalidOperationException("attachmentUrl must target /minio/chat-uploads/<object>.");
        }

        try
        {
            await minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(ChatUploadsBucketName)
                    .WithObject(objectName),
                cancellationToken);
        }
        catch (ObjectNotFoundException)
        {
            throw new InvalidOperationException("Attachment was not found in storage. Upload it before sending the message.");
        }
        catch (MinioException ex)
        {
            throw new InvalidOperationException("Attachment validation failed due to storage error.", ex);
        }

        return attachmentUri.GetLeftPart(UriPartial.Path);
    }

    private static bool TryExtractChatUploadObjectName(Uri attachmentUri, out string objectName)
    {
        objectName = string.Empty;
        var path = attachmentUri.AbsolutePath;
        if (path.StartsWith(MinioProxyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            path = path[MinioProxyPrefix.Length..];
        }
        else if (path.StartsWith("/", StringComparison.Ordinal))
        {
            path = path[1..];
        }

        var segments = path.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2 || !segments[0].Equals(ChatUploadsBucketName, StringComparison.Ordinal))
        {
            return false;
        }

        var candidateObject = Uri.UnescapeDataString(segments[1]).Trim();
        if (string.IsNullOrWhiteSpace(candidateObject))
        {
            return false;
        }

        objectName = candidateObject;
        return true;
    }
}
