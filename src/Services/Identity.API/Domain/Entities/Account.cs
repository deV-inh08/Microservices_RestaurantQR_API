namespace Identity.API.Domain.Entities
{
    public enum UserRole
    {
        Admin = 1,
        Staff = 2,
        Guest = 3
    }
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;


        public UserRole Role { get; set; } = UserRole.Staff;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Quan hệ
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
