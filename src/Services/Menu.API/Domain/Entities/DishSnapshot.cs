namespace Menu.API.Domain.Entities
{
    public class DishSnapshot
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Price { get; set; }
        public string? Description { get; set; }
        public string Image { get; set; } = string.Empty;
        public int DishId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
