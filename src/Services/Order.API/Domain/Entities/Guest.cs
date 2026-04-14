namespace Order.API.Domain.Entities
{


    public class Guest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TableId { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
