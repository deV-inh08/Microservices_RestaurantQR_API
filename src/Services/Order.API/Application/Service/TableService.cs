using Microsoft.EntityFrameworkCore;
using Order.API.Application.DTOs;
using Order.API.Domain.Entities;
using Order.API.Infrastructure.Persistence;

namespace Order.API.Application.Service;

public class TableService
{
    private readonly OrderDbContext _db;

    public TableService(OrderDbContext db) => _db = db;

    public async Task<List<TableDto>> GetAllAsync()
    {
        var tables = await _db.Tables.OrderBy(t => t.Number).ToListAsync();
        return tables.Select(ToDto).ToList();
    }

    public async Task<TableDto> GetByIdAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id)
            ?? throw new KeyNotFoundException("Bàn không tồn tại");
        return ToDto(table);
    }

    public async Task<TableDto> CreateAsync(CreateTableRequest request)
    {
        if (await _db.Tables.AnyAsync(t => t.Number == request.Number))
            throw new ArgumentException($"Bàn số {request.Number} đã tồn tại");

        var table = new Table
        {
            Number = request.Number,
            Capacity = request.Capacity,
            Status = TableStatus.Available,
            SessionId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tables.Add(table);
        await _db.SaveChangesAsync();
        return ToDto(table);
    }

    public async Task<TableDto> UpdateStatusAsync(int id, UpdateTableStatusRequest request)
    {
        var table = await _db.Tables.FindAsync(id)
            ?? throw new KeyNotFoundException("Bàn không tồn tại");

        table.Status = request.Status;
        table.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(table);
    }

    // Gọi khi khách rời bàn — vô hiệu hoá tất cả GuestToken cũ
    public async Task<TableDto> ResetTableAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id)
            ?? throw new KeyNotFoundException("Bàn không tồn tại");

        table.SessionId = Guid.NewGuid(); // Token cũ hết hiệu lực
        table.Status = TableStatus.Hidden;
        table.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(table);
    }

    public async Task<TableDto> DeleteAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id)
            ?? throw new KeyNotFoundException("Bàn không tồn tại");

        _db.Tables.Remove(table);
        await _db.SaveChangesAsync();
        return ToDto(table);
    }

    public static TableDto ToDto(Table t) => new(
        t.Id, t.Number, t.Capacity, t.Status.ToString(),
        t.IsVisibleOnReservation, t.CreatedAt, t.UpdatedAt);
}