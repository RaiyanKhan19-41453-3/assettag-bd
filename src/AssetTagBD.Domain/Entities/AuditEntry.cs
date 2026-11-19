using AssetTagBD.Domain.Common;

namespace AssetTagBD.Domain.Entities;

/// <summary>
/// Immutable audit record written by the MediatR pipeline after a successful
/// command. Stores an actor snapshot so the trail survives user deletion.
/// </summary>
public class AuditEntry : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string Actor { get; set; } = "System";

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
