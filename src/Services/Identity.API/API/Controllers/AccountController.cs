using Identity.API.Application.DTOs;
using Identity.API.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    // ─── Ai cũng dùng được (đã login) ────────────────

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        var result = await _accountService.GetProfileAsync(userId);
        return Ok(new { message = "Lấy thông tin cá nhân thành công", data = result });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _accountService.UpdateProfileAsync(userId, request);
        return Ok(new { message = "Cập nhật thành công", data = result });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        await _accountService.ChangePasswordAsync(userId, request);
        return Ok(new { message = "Đổi mật khẩu thành công" });
    }

    // ─── SuperAdmin: quản lý Admin ────────────────────

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _accountService.GetAllAsync();
        return Ok(new { message = "Lấy danh sách tài khoản thành công", data = result });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _accountService.GetByIdAsync(id);
        if (result is null)
            return NotFound(new { message = "Tài khoản không tồn tại" });
        return Ok(new { message = "Lấy tài khoản thành công", data = result });
    }

    [HttpPost("admin")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        var result = await _accountService.CreateAdminAsync(request);
        return Ok(new { message = "Tạo tài khoản Admin thành công", data = result });
    }

    // ─── Admin: quản lý Staff ─────────────────────────

    [HttpPost("staff")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var result = await _accountService.CreateStaffAsync(request);
        return Ok(new { message = "Tạo tài khoản Staff thành công", data = result });
    }

    // ─── Cập nhật / Xóa (cấp trên quản lý cấp dưới) ─

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
    {
        var result = await _accountService.UpdateEmployeeAsync(id, request);
        return Ok(new { message = "Cập nhật tài khoản thành công", data = result });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _accountService.DeleteAsync(id);
        return Ok(new { message = "Xóa tài khoản thành công", data = result });
    }

    private int GetCurrentUserId()
    {
        // Middleware decode JWT --> Assign User to HTTPContext
        var claim = HttpContext.User.FindFirst("userId")?.Value
            ?? throw new UnauthorizedAccessException("Không tìm thấy userId trong token");
        return int.Parse(claim);
    }
}