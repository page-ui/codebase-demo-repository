namespace Page.Ui.Domain.Auth.Entities
{
    public class PasswordResetCode
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? TempToken { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime ExpirationTime { get; set; }
    }
}
