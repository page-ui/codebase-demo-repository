namespace Page.Ui.Domain.Auth.Entities
{
    public class PendingRegistration
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public DateTime ExpirationTime { get; set; }
        public DateTime? LastResentAt { get; set; }
    }
}
