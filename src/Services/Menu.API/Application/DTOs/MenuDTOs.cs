using Menu.API.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Menu.API.Application.DTOs;



public record DishDto(
    int Id,
    string Name,
    string Description,
    string? ImagePath,
    DishCategory Category,
    int Price,
    string Status,
    DateTime CreatedAt);

public class CreateDishRequest
{
    [FromForm(Name = "name")]
    public required string Name { get; set; }

    [FromForm(Name = "price")]
    public required decimal Price { get; set; }

    [FromForm(Name = "description")]
    public required string Description { get; set; }

    [FromForm(Name = "category")]
    public required DishCategory Category { get; set; }

    [FromForm(Name = "image")]
    public IFormFile? Image { get; set; }
}

//public record CreateDishRequest(
//    string Name,
//    string Description,
//    IFormFile Image,
//    DishCategory Category,
//    int Price);

public record UpdateDishRequest(
    string Name,
    string Description,
    string? ImagePath,
    int Price,
    DishCategory Category
    );

public record UpdateDishStatusRequest(DishStatus Status);