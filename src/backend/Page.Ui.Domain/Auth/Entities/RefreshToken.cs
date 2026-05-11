namespace Page.Ui.Domain.Auth.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool Used { get; set; }
        public bool Invalidated { get; set; }
        public string RemoteIpAddress { get; set; } = string.Empty;
        public string HashedToken { get; set; } = string.Empty;
    }
}

