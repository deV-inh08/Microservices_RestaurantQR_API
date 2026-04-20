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
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _tableService.GetAllAsync();
        return Ok(new { message = "Get all tables successfully", data = result });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _tableService.GetByIdAsync(id);
        return Ok(new { message = "Get table by id successfully", data = result });
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateTableRequest request)
    {
        var result = await _tableService.CreateAsync(request);
        return Ok(new { message = "Create table successfully", data = result });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTableStatusRequest request)
    {
        var result = await _tableService.UpdateStatusAsync(id, request);
        return Ok(new { message = "Update status successfully", data = result });
    }

    // Staff click "Reset" when guest go out
    [HttpPatch("{id:int}/reset")]
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> Reset(int id)
    {
        var result = await _tableService.ResetTableAsync(id);
        return Ok(new { message = "Reset table successfully", data = result });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Staff")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _tableService.DeleteAsync(id);
        return Ok(new { message = "Delete table successfully", data = result });
    }
}