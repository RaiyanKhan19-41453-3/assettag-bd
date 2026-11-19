namespace AssetTagBD.Application.DTOs.Audit;

public sealed record AuditEntryDto(
    Guid Id,
    string Action,
    string EntityType,
    Guid? EntityId,
    string Actor,
    string? Details,
    DateTime OccurredAt);
