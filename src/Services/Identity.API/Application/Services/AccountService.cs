// Application/Services/AccountService.cs
using Identity.API.Application.DTOs;
using Identity.API.Application.Interfaces;
using Identity.API.Domain.Entities;
using Identity.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Application.Services;

public class AccountService
{
    private readonly IdentityDbContext _db;
    private readonly IPasswordUtil _passwordService;

    public AccountService(IdentityDbContext db, IPasswordUtil passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    // ─── Query ────────────────────────────────────────

    public async Task<List<AccountDto>> GetAllAsync()
    {
        var accounts = await _db.Accounts
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return accounts.Select(AuthService.ToDto).ToList();
    }

    public async Task<AccountDto?> GetByIdAsync(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        return account is null ? null : AuthService.ToDto(account);
    }

    public async Task<AccountDto> GetProfileAsync(int accountId)
    {
        var account = await _db.Accounts.FindAsync(accountId);

        if (account is null)
        {
            throw new KeyNotFoundException("Account isn't exists");
        }
        return AuthService.ToDto(account);
    }

    // ─── Profile (self) ───────────────────────────────

    public async Task<AccountDto> UpdateProfileAsync(int accountId, UpdateProfileRequest request)
    {
        var account = await _db.Accounts.FindAsync(accountId);
        if (account is null)
        {
            throw new KeyNotFoundException("Account isn't exists");
        }

        account.Name = request.Name.Trim();
        account.Avatar = request.Avatar;
        account.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return AuthService.ToDto(account);
    }

    public async Task ChangePasswordAsync(int accountId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException("Password");

        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new KeyNotFoundException("Account not found");

        if (!_passwordService.Verify(request.OldPassword, account.Password))
            throw new UnauthorizedAccessException("Old password is incorrect");

        account.Password = _passwordService.Hash(request.NewPassword);
        account.UpdatedAt = DateTime.UtcNow;

        var tokens = await _db.RefreshTokens
            .Where(rt => rt.AccountId == accountId)
            .ToListAsync();
        _db.RefreshTokens.RemoveRange(tokens);

        await _db.SaveChangesAsync();
    }

    // ─── SuperAdmin tạo Admin ─────────────────────────

    public async Task<AccountDto> CreateAdminAsync(CreateAdminRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new ArgumentException("Password confirmation does not match");

        if (await _db.Accounts.AnyAsync(a => a.Email == request.Email.ToLower()))
            throw new ArgumentException("Email already exists");

        var account = new Account
        {
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            Password = _passwordService.Hash(request.Password),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return AuthService.ToDto(account);
    }

    // ─── Admin tạo Staff ──────────────────────────────

    public async Task<AccountDto> CreateStaffAsync(CreateStaffRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new ArgumentException("Confirm password incorrect");

        if (await _db.Accounts.AnyAsync(a => a.Email == request.Email.ToLower()))
            throw new ArgumentException("Email is exist");

        var account = new Account
        {
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            Password = _passwordService.Hash(request.Password),
            Role = UserRole.Staff,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return AuthService.ToDto(account);
    }

    // ─── Update / Delete ──────────────────────────────

    public async Task<AccountDto> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request)
    {
        var account = await _db.Accounts.FindAsync(id)
            ?? throw new KeyNotFoundException("Account not found");

        account.Name = request.Name.Trim();
        account.Email = request.Email.ToLower().Trim();
        account.Avatar = request.Avatar;
        account.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return AuthService.ToDto(account);
    }

    public async Task<AccountDto> DeleteAsync(int id)
    {
        var account = await _db.Accounts.FindAsync(id)
            ?? throw new KeyNotFoundException("Account not found");

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
        return AuthService.ToDto(account);
    }
}