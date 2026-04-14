namespace Order.API.Domain.Entities
{
    public enum OrderStatus
    {
        Pending = 1, // Tiếp nhận / Chờ duyệt
        Preparing = 2, // Đang nấu / Đang chế biến
        Served = 3, //  Đã phục vụ
        Cancelled = 4 // Đã hủy
    }
    public class Order
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public int TableId { get; set; }
        public int DishSnapshotId { get; set; }
        public int? AccountId { get; set; } // Nhân viên xử lý (từ Account Id)
        public int Quantity { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
