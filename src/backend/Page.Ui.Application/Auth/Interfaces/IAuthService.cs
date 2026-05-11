namespace Page.Ui.Application.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<string> GenerateAccessTokenAsync(string userId, string email, IEnumerable<string> roles);
        Task<string> GenerateRefreshTokenAsync(string userId, string remoteIpAddress);
        Task<(bool Success, string AccessToken, string RefreshToken)> RefreshAccessTokenAsync(string token, string remoteIpAddress);
        Task<bool> InvalidateRefreshTokenAsync(string token);
        Task<string?> VerifyPasswordResetCodeAsync(string email, string code);
        Task<bool> ResetPasswordAsync(string email, string tempToken, string newPassword);
        Task<bool> DeleteAccountAsync(string userId);
    }
}

