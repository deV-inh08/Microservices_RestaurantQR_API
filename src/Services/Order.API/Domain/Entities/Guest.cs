namespace Order.API.Domain.Entities
{


    public class Guest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TableId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Table Table { get; set; } = null!;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
