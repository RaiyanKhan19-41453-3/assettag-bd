using System.Reflection;
using System.Text.Json;
using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using MediatR;

namespace AssetTagBD.Application.Common.Behaviors;

/// <summary>
/// Records a snapshot after every successful mutating command. Auth commands
/// are excluded because their payloads contain secrets.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly HashSet<string> ExcludedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "LoginCommand",
        "RegisterCommand",
        "CreateTenantCommand",
        "RefreshTokenCommand",
        "RevokeTokenCommand",
        "ForgotPasswordCommand",
        "ResetPasswordCommand"
    };

    private readonly IAuditWriter _audit;
    private readonly ICurrentUserService _currentUser;

    public AuditBehavior(IAuditWriter audit, ICurrentUserService currentUser)
    {
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        var name = typeof(TRequest).Name;
        if (!name.EndsWith("Command", StringComparison.Ordinal) || ExcludedCommands.Contains(name))
        {
            return response;
        }

        if (response is IResultMarker { IsSuccess: false })
        {
            return response;
        }

        var properties = typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var entityId = FindEntityId(properties, request) ?? FindResponseEntityId(response);

        var action = name[..^"Command".Length];
        await _audit.RecordAsync(new AuditRecord(
            Action: action,
            EntityType: EntityTypeFrom(action),
            EntityId: entityId,
            Details: BuildDetails(properties, request)),
            cancellationToken);

        return response;
    }

    private static Guid? FindEntityId(PropertyInfo[] properties, object request)
    {
        var candidates = properties.Where(property =>
            property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?));

        var chosen = candidates.FirstOrDefault(property => property.Name == "Id")
            ?? candidates.FirstOrDefault(property => property.Name.EndsWith("Id", StringComparison.Ordinal));

        if (chosen is null) return null;

        var value = chosen.GetValue(request);
        return value is Guid guid ? guid : null;
    }

    private static string EntityTypeFrom(string action)
    {
        string[] verbs = ["Create", "Update", "Delete", "Assign", "Return", "Report", "Resolve", "Import"];
        foreach (var verb in verbs)
        {
            if (action.StartsWith(verb, StringComparison.Ordinal) && action.Length > verb.Length)
            {
                return action[verb.Length..];
            }
        }
        return action;
    }

    private static Guid? FindResponseEntityId(object? response)
    {
        if (response is null) return null;

        var responseType = response.GetType();
        var value = responseType.GetProperty("Value")?.GetValue(response) ?? response;
        var idProperty = value.GetType().GetProperty("Id");

        if (idProperty is null ||
            (idProperty.PropertyType != typeof(Guid) && idProperty.PropertyType != typeof(Guid?)))
        {
            return null;
        }

        return idProperty.GetValue(value) is Guid guid ? guid : null;
    }

    private static string? BuildDetails(PropertyInfo[] properties, object request)
    {
        var safe = new Dictionary<string, object?>();

        foreach (var property in properties)
        {
            var propertyName = property.Name;
            if (propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("FileContent", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var isSimple = type.IsPrimitive || type.IsEnum ||
                           type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime);

            if (isSimple)
            {
                safe[propertyName] = property.GetValue(request);
            }
        }

        return safe.Count == 0 ? null : JsonSerializer.Serialize(safe);
    }
}
