using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Chat.Inputs;
using Page.Ui.Application.Chat.Services;
using Page.Ui.Application.Common.Interfaces;

namespace Page.Ui.Presentation.Chat.Controllers;

[ApiController]
[Route("api/internal/render-diagnostics")]
public sealed class RenderDiagnosticsController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IChatService _chatService;
    private readonly IConfiguration _configuration;

    public RenderDiagnosticsController(
        IApplicationDbContext dbContext,
        IChatService chatService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _chatService = chatService;
        _configuration = configuration;
    }

    [HttpPost("report")]
    public async Task<IActionResult> Report(
        [FromBody] InternalRenderDiagnosticsReport? report,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedRelay())
        {
            return Unauthorized(new { error = "Unauthorized" });
        }

        if (report is null || string.IsNullOrWhiteSpace(report.ChatKey) || !report.VersionId.HasValue)
        {
            return BadRequest(new { error = "chatKey and versionId are required." });
        }

        var ownerUserId = await _dbContext.Chats
            .AsNoTracking()
            .Where(chat => chat.ChatKey == report.ChatKey)
            .Select(chat => chat.OwnerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return NotFound(new { error = "Chat was not found." });
        }

        var stored = await _chatService.ReportRenderErrorAsync(
            new ReportRenderErrorInput
            {
                ChatKey = report.ChatKey,
                VersionId = report.VersionId,
                Errors = report.Errors,
                Logs = report.Logs
            },
            ownerUserId,
            cancellationToken);

        return Ok(new { stored });
    }

    private bool IsAuthorizedRelay()
    {
        var expectedKey = _configuration["RenderDiagnostics:RelayApiKey"] ??
            _configuration["RenderDiagnostics__RelayApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return false;
        }

        var providedKey = Request.Headers["X-Render-Diagnostics-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(providedKey) && AreApiKeysEqual(providedKey, expectedKey);
    }

    private static bool AreApiKeysEqual(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided.Trim());
        var expectedBytes = Encoding.UTF8.GetBytes(expected.Trim());

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}

public sealed class InternalRenderDiagnosticsReport
{
    public string ChatKey { get; set; } = string.Empty;
    public Guid? VersionId { get; set; }
    public string? PagePath { get; set; }
    public string? PublicRunToken { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Logs { get; set; } = new();
}
