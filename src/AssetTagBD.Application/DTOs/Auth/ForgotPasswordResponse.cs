namespace AssetTagBD.Application.DTOs.Auth;

/// <summary>
/// Always returned for a forgot-password request so account existence is never
/// disclosed. DevToken is populated only when explicitly enabled for demos/tests.
/// </summary>
public sealed record ForgotPasswordResponse(string Message, string? DevToken);
