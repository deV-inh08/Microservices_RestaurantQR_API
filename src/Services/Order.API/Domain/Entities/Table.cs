namespace Order.API.Domain.Entities
{
    public enum TableStatus
    {
        Available = 1,
        Occupied = 2,
        Hidden = 3 // Bàn tạm thời không hiển thị (do hỏng hóc, đang dọn dẹp, v.v.)
    }
    public class Table
    {
        public int Id { get; set; } // PK là Number
        public int Capacity { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Available;
        public string Token { get; set; } = string.Empty; // QR Token
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
