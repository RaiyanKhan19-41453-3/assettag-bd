using AssetTagBD.Domain.Entities;

namespace AssetTagBD.Application.DTOs.Assignments;

public sealed record AssignmentDto(
    Guid Id,
    Guid AssetId,
    string AssetTag,
    string AssetName,
    Guid AssignedToUserId,
    string AssignedToUserName,
    Guid AssignedByUserId,
    string AssignedByUserName,
    DateTime AssignedDate,
    DateTime? ReturnedDate,
    string? ConditionNotes)
{
    public static AssignmentDto FromEntity(Assignment assignment) =>
        new(
            assignment.Id,
            assignment.AssetId,
            assignment.Asset.AssetTag,
            assignment.Asset.Name,
            assignment.AssignedToUserId,
            assignment.AssignedToUser.FullName,
            assignment.AssignedByUserId,
            assignment.AssignedByUser.FullName,
            assignment.AssignedDate,
            assignment.ReturnedDate,
            assignment.ConditionNotes);
}
