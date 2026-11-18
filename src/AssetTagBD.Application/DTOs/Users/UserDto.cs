using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Application.DTOs.Users;

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    string? Department)
{
    public static UserDto FromEntity(User user) =>
        new(user.Id, user.FullName, user.Email, user.Role, user.Department);
}
