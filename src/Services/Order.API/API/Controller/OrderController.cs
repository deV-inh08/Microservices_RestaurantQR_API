using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.API.Application.DTOs;
using Order.API.Application.Service;

namespace Order.API.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;
    public OrderController(OrderService orderService) => _orderService = orderService;

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Staff", Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _orderService.GetAllAsync();
        return Ok(new { message = "Lấy danh sách order thành công", data = result });
    }

    [HttpGet("my-orders")]
    [Authorize(AuthenticationSchemes = "Guest", Roles = "Guest")]
    public async Task<IActionResult> GetMyOrders()
    {
        var (guestId, sessionId) = GetGuestClaims();
        var result = await _orderService.GetByGuestAsync(guestId);
        return Ok(new { message = "Lấy orders thành công", data = result });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Guest", Roles = "Guest")]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var (guestId, sessionId) = GetGuestClaims();
        var result = await _orderService.CreateAsync(guestId, sessionId, request);
        return Ok(new { message = "Đặt món thành công", data = result });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(AuthenticationSchemes = "Staff", Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateStatusAsync(id, request);
        return Ok(new { message = "Cập nhật trạng thái thành công", data = result });
    }

    private (int guestId, Guid sessionId) GetGuestClaims()
    {
        var guestId = int.Parse(HttpContext.User.FindFirst("guestId")?.Value
            ?? throw new UnauthorizedAccessException("Token không hợp lệ"));

        var sessionId = Guid.Parse(HttpContext.User.FindFirst("sessionId")?.Value
            ?? throw new UnauthorizedAccessException("Token không hợp lệ"));

        return (guestId, sessionId);
    }
}