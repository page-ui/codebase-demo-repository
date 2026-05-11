using Microsoft.EntityFrameworkCore;
using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Domain.Chat.Entities;

namespace Page.Ui.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<PasswordResetCode> PasswordResetCodes { get; }
        DbSet<PendingRegistration> PendingRegistrations { get; }
        DbSet<ApplicationUser> Users { get; }
        DbSet<Page.Ui.Domain.Chat.Entities.Chat> Chats { get; }
        DbSet<Message> Messages { get; }
        DbSet<AiRun> AiRuns { get; }
        DbSet<AiRunFile> AiRunFiles { get; }
        DbSet<RenderRun> RenderRuns { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
