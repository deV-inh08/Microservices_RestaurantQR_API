using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.API.Application.DTOs;
using Order.API.Application.Service;

namespace Order.API.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TableController : ControllerBase
{
    private readonly TableService _tableService;
    public TableController(TableService tableService) => _tableService = tableService;

    [HttpGet]
    [Authorize(AuthenticationSchemes = "Staff", Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _tableService.GetAllAsync();
        return Ok(new { message = "Lấy danh sách bàn thành công", data = result });
    }

    [HttpGet("{id:int}")]
    [Authorize(AuthenticationSchemes = "Staff", Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _tableService.GetByIdAsync(id);
        return Ok(new { message = "Lấy thông tin bàn thành công", data = result });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Staff", Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateTableRequest request)
    {
        var result = await _tableService.CreateAsync(request);
        return Ok(new { message = "Tạo bàn thành công", data = result });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(AuthenticationSchemes = "Staff", Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTableStatusRequest request)
    {
        var result = await _tableService.UpdateStatusAsync(id, request);
        return Ok(new { message = "Cập nhật trạng thái thành công", data = result });
    }

    // Staff bấm "Reset bàn" khi khách rời đi
    [HttpPatch("{id:int}/reset")]
    [Authorize(AuthenticationSchemes = "Staff", Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> Reset(int id)
    {
        var result = await _tableService.ResetTableAsync(id);
        return Ok(new { message = "Reset bàn thành công", data = result });
    }

    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = "Staff", Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _tableService.DeleteAsync(id);
        return Ok(new { message = "Xóa bàn thành công", data = result });
    }
}