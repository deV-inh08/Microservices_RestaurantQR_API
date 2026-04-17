using Menu.API.Application.DTOs;
using Menu.API.Domain.Entities;
using Menu.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menu.API.Application.Services;

public class MenuService
{
    private readonly MenuDbContext _db;

    public MenuService(MenuDbContext db)
    {
        _db = db;
    }

    // ─── Query ────────────────────────────────────────

    public async Task<List<DishDto>> GetAllAsync()
    {
        var dishes = await _db.Dishes
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return dishes.Select(ToDto).ToList();
    }

    public async Task<DishDto> GetByIdAsync(int id)
    {
        var dish = await _db.Dishes.FindAsync(id)
            ?? throw new KeyNotFoundException("Món ăn không tồn tại");

        return ToDto(dish);
    }

    // ─── Mutations ────────────────────────────────────

    public async Task<DishDto> CreateAsync(CreateDishRequest request)
    {
        if (request.Price <= 0)
            throw new ArgumentException("Giá món ăn phải lớn hơn 0");

        var dish = new Dish
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            Image = request.Image,
            Description = request.Description,
            Status = DishStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        _db.Dishes.Add(dish);
        await _db.SaveChangesAsync();


        // Tạo snapshot đầu tiên ngay khi tạo món
        _db.DishSnapshots.Add(new DishSnapshot
        {
            DishId = dish.Id,
            Name = dish.Name,
            Price = dish.Price,
            Image = dish.Image,
            Description = dish.Description,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return ToDto(dish);
    }

    public async Task<DishDto> UpdateAsync(int id, UpdateDishRequest request)
    {
        var dish = await _db.Dishes.FindAsync(id)
            ?? throw new KeyNotFoundException("Món ăn không tồn tại");

        if (request.Price <= 0)
            throw new ArgumentException("Giá món ăn phải lớn hơn 0");

        // Khi update giá/tên → tạo snapshot để Order.API có thể
        // reference lại giá tại thời điểm đặt hàng (immutable history)
        if (dish.Price != request.Price || dish.Name != request.Name.Trim())
        {
            _db.DishSnapshots.Add(new DishSnapshot
            {
                DishId = dish.Id,
                Name = dish.Name,   // snapshot giá trị CŨ
                Image = dish.Image,
                Description = dish.Description,
                Price = dish.Price,
                CreatedAt = DateTime.UtcNow
            });
        }

        dish.Name = request.Name.Trim();
        dish.Price = request.Price;

        await _db.SaveChangesAsync();
        return ToDto(dish);
    }

    public async Task<DishDto> UpdateStatusAsync(int id, UpdateDishStatusRequest request)
    {
        var dish = await _db.Dishes.FindAsync(id)
            ?? throw new KeyNotFoundException("Món ăn không tồn tại");

        if (!Enum.IsDefined(typeof(DishStatus), request.Status))
            throw new ArgumentException($"Trạng thái không hợp lệ. Dùng: Available hoặc OutOfStock");

        dish.Status = request.Status;
        await _db.SaveChangesAsync();
        return ToDto(dish);
    }

    public async Task<DishDto> DeleteAsync(int id)
    {
        var dish = await _db.Dishes.FindAsync(id)
            ?? throw new KeyNotFoundException("Món ăn không tồn tại");

        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync();
        return ToDto(dish);
    }

    // ─── Mapping ──────────────────────────────────────

    public static DishDto ToDto(Dish d) => new(
        d.Id, d.Name, d.Description, d.Image, d.Price, d.Status.ToString(), d.CreatedAt);
}