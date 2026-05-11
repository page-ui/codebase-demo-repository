namespace Page.Ui.Application.Common.Interfaces;

public interface IInternalServiceJwtProvider
{
    string CreateAiApiToken(Guid chatId, Guid messageId, string userId);
}
