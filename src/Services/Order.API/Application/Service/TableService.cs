using Microsoft.EntityFrameworkCore;
using Order.API.Application.DTOs;
using Order.API.Domain.Entities;
using Order.API.Infrastructure.Persistence;
using Shared.DTOs;

namespace Order.API.Application.Service;

public class TableService
{
    private readonly OrderDbContext _db;

    public TableService(OrderDbContext db) => _db = db;

    public async Task<PaginatedResponse<TableDto>> GetAllAsync(PaginationParams p)
    {
        var query = _db.Tables.OrderBy(t => t.Number);
        var total = await query.CountAsync();
        var items = await query
            .Skip(p.Skip)
            .Take(p.Take)
            .ToListAsync();
        return new PaginatedResponse<TableDto>(items.Select(ToDto), total, p.Page, p.Take);
    }

    public async Task<TableDto> GetByIdAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id)
            ?? throw new KeyNotFoundException("Table not found");
        return ToDto(table);
    }

    public async Task<TableDto> CreateAsync(CreateTableRequest request)
    {
        if (await _db.Tables.AnyAsync(t => t.Number == request.Number))
            throw new ArgumentException($"Table number {request.Number} already exists");

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
            ?? throw new KeyNotFoundException("Table not found");

        table.Status = request.Status;
        table.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(table);
    }

    // Gọi khi khách rời bàn — vô hiệu hoá tất cả GuestToken cũ
    public async Task<TableDto> ResetTableAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id)
            ?? throw new KeyNotFoundException("Table not found");

        table.SessionId = Guid.NewGuid(); // Token cũ hết hiệu lực
        table.Status = TableStatus.Hidden;
        table.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(table);
    }

    public async Task<TableDto> DeleteAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id)
            ?? throw new KeyNotFoundException("Table not found");

        _db.Tables.Remove(table);
        await _db.SaveChangesAsync();
        return ToDto(table);
    }

    public async Task<object> GetByIdPublicAsync(int id)
    {
        var table = await _db.Tables.FindAsync(id)
            ?? throw new KeyNotFoundException("Table not found");

        return new
        {
            table.Id,
            table.Number,
            table.Status  // FE check Hidden/Available
        };
    }

    public static TableDto ToDto(Table t) => new(
        t.Id, t.Number, t.Capacity, t.Status.ToString(),
        t.IsVisibleOnReservation, t.CreatedAt, t.UpdatedAt);
}