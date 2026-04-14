namespace Identity.API.Domain.Entities
{
    public class RefreshToken
    {
        public string Token { get; set; } = String.Empty;
        public int AccountId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
