using Menu.API.Domain.Entities;

namespace Menu.API.Application.DTOs;



public record DishDto(
    int Id,
    string Name,
    string Description,
    string? Image,
    int Price,
    string Status,
    DateTime CreatedAt);

public record CreateDishRequest(
    string Name,
    string Description,
    string? Image,
    int Price);

public record UpdateDishRequest(
    string Name,
    string Description,
    string? Image,
    int Price);

public record UpdateDishStatusRequest(DishStatus Status);