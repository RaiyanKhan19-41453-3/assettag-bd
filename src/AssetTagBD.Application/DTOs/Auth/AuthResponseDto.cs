using AssetTagBD.Application.DTOs.Users;

namespace AssetTagBD.Application.DTOs.Auth;

public sealed record AuthResponseDto(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);
