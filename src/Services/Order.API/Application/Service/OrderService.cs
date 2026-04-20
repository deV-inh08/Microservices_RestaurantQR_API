using Microsoft.EntityFrameworkCore;
using Order.API.Application.DTOs;
using Order.API.Domain.Entities;
using Order.API.Infrastructure.ExternalServices;
using Order.API.Infrastructure.Persistence;

namespace Order.API.Application.Service;

public class OrderService
{
    private readonly OrderDbContext _db;
    private readonly MenuApiClient _menuApi;

    public OrderService(OrderDbContext db, MenuApiClient menuApi)
    {
        _db = db;
        _menuApi = menuApi;
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        var orders = await _db.Orders
            .Include(o => o.Guest)
            .Include(o => o.Table)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(ToDto).ToList();
    }

    public async Task<List<OrderDto>> GetByGuestAsync(int guestId)
    {
        var orders = await _db.Orders
            .Include(o => o.Guest)
            .Include(o => o.Table)
            .Where(o => o.GuestId == guestId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(ToDto).ToList();
    }

    public async Task<OrderDto> CreateAsync(int guestId, Guid tokenSessionId, CreateOrderRequest request)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Số lượng phải lớn hơn 0");

        var guest = await _db.Guests
            .Include(g => g.Table)
            .FirstOrDefaultAsync(g => g.Id == guestId)
            ?? throw new UnauthorizedAccessException("Phiên đã hết hạn, vui lòng quét QR lại");

        // Kiểm tra SessionId — tránh trường hợp bàn đã reset
        // mà Guest vẫn còn AccessToken chưa hết hạn
        if (guest.Table.SessionId != tokenSessionId)
            throw new UnauthorizedAccessException("Phiên đã hết hạn, vui lòng quét QR lại");

        var snapshot = await _menuApi.GetSnapshotAsync(request.DishSnapshotId);
        var order = new Domain.Entities.Order
        {
            GuestId = guestId,
            TableId = guest.TableId,
            DishSnapshotId = request.DishSnapshotId,
            DishName = snapshot.Name,
            DishPrice = snapshot.Price,
            DishImage = snapshot.ImagePath,
            Quantity = request.Quantity,
            Status = Domain.Entities.OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _db.Entry(order).Reference(o => o.Guest).LoadAsync();
        await _db.Entry(order).Reference(o => o.Table).LoadAsync();

        return ToDto(order);
    }


    public async Task<OrderDto> CreateAsStaffAsync(CreateOrderRequest request)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");

        if (request.TableId is null)
            throw new ArgumentException("TableId is required when Staff creates an order");

        var table = await _db.Tables.FindAsync(request.TableId)
            ?? throw new KeyNotFoundException("Table not found");

        if (table.Status != TableStatus.Occupied)
            throw new ArgumentException("Table is not occupied");

        // Tìm guest mới nhất của bàn này
        var guest = await _db.Guests
            .Where(g => g.TableId == request.TableId)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Guest not found");

        // ── Lấy snapshot data từ Menu.API ──────────────────────────
        var snapshot = await _menuApi.GetSnapshotAsync(request.DishSnapshotId);
        var order = new Domain.Entities.Order
        {
            GuestId = guest.Id,
            TableId = request.TableId.Value,
            DishSnapshotId = request.DishSnapshotId,
            DishName = snapshot.Name,
            DishPrice = snapshot.Price,
            DishImage = snapshot.ImagePath,
            Quantity = request.Quantity,
            Status = Domain.Entities.OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _db.Entry(order).Reference(o => o.Guest).LoadAsync();
        await _db.Entry(order).Reference(o => o.Table).LoadAsync();
        return ToDto(order);
    }
    public async Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusRequest request)
    {
        var order = await _db.Orders
            .Include(o => o.Guest)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException("Order not found");

        order.Status = request.Status;
        order.AccountId = request.AccountId;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(order);
    }

    private static OrderDto ToDto(Domain.Entities.Order o) => new(
         o.Id, o.GuestId, o.Guest.Name,
         o.TableId, o.Table.Number,
         o.DishSnapshotId,
         o.DishName,   // ← đọc từ column
         o.DishPrice,  // ← đọc từ column
         o.DishImage,  // ← đọc từ column
         o.AccountId,
         o.Quantity, o.Status.ToString(),
         o.CreatedAt, o.UpdatedAt);
}