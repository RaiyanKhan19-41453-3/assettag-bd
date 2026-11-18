using AssetTagBD.Application.Common.Interfaces;

namespace AssetTagBD.Infrastructure.Identity;

public sealed record AuthSettings(bool ExposeResetTokenInResponse) : IAuthSettings;
