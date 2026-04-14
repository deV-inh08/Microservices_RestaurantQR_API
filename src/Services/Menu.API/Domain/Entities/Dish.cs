namespace Menu.API.Domain.Entities
{
    public enum DishStatus
    {
        // Món ăn đang sẵn sàng phục vụ
        Available = 1,

        // Món ăn tạm thời hết (do hết nguyên liệu trong ngày)
        OutOfStock = 2,
    }
    public class Dish
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Price { get; set; }
        public DishStatus Status { get; set; } = DishStatus.Available;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
