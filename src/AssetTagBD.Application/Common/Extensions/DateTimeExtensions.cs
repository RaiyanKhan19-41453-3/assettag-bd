namespace AssetTagBD.Application.Common.Extensions;

/// <summary>
/// Npgsql requires DateTime values written to <c>timestamp with time zone</c>
/// columns to have Kind=Utc. JSON date-only strings are bound as Unspecified,
/// so all incoming dates must be normalised before they reach EF Core.
/// </summary>
public static class DateTimeExtensions
{
    public static DateTime AsUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? AsUtc(this DateTime? value) => value?.AsUtc();
}
