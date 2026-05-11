using MassTransit;
using Page.Ui.Application.Auth.Contracts;
using Page.Ui.Application.Auth.Interfaces;

namespace Page.Ui.Presentation.Auth.Consumers;

public class AuthEmailRequestedConsumer : IConsumer<AuthEmailRequested>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthEmailRequestedConsumer> _logger;

    public AuthEmailRequestedConsumer(IEmailService emailService, ILogger<AuthEmailRequestedConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AuthEmailRequested> context)
    {
        _logger.LogInformation(
            "Dispatching auth email. MessageId={MessageId} RecipientEmail={RecipientEmail} Subject={Subject}",
            context.MessageId,
            context.Message.RecipientEmail,
            context.Message.Subject);

        var sent = await _emailService.SendEmailAsync(
            context.Message.RecipientEmail,
            context.Message.Subject,
            context.Message.HtmlBody);

        if (!sent)
        {
            _logger.LogError(
                "Auth email dispatch failed. MessageId={MessageId} RecipientEmail={RecipientEmail}",
                context.MessageId,
                context.Message.RecipientEmail);
            throw new InvalidOperationException(
                $"Failed to send auth email to {context.Message.RecipientEmail}.");
        }

        _logger.LogInformation(
            "Auth email dispatched successfully. MessageId={MessageId} RecipientEmail={RecipientEmail}",
            context.MessageId,
            context.Message.RecipientEmail);
    }
}
