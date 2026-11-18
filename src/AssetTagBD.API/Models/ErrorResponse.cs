namespace AssetTagBD.API.Models;

/// <summary>Wire format shared by the exception middleware and controller error results.</summary>
public sealed record ErrorResponse(
    string Code,
    string Message,
    string? TraceId = null,
    IDictionary<string, string[]>? Errors = null);
