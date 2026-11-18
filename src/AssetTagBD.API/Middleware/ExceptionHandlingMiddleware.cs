using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetTagBD.API.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AssetTagBD.API.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a consistent JSON error shape.
/// Expected business failures use the Result pattern in handlers and are mapped
/// by ApiControllerBase; this middleware handles the truly exceptional cases.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "Response had already started when an exception was thrown.");
                throw;
            }

            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message, errors) = MapException(exception);
        var traceId = context.TraceIdentifier;

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode}. TraceId: {TraceId}", statusCode, traceId);
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var body = new ErrorResponse(code, message, traceId, errors);
        await context.Response.WriteAsJsonAsync(body, JsonOptions);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: "23505" };

    private static (int StatusCode, string Code, string Message, IDictionary<string, string[]>? Errors)
        MapException(Exception exception)
    {
        switch (exception)
        {
            case ValidationException validationException:
            {
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                return ((int)HttpStatusCode.BadRequest, "ValidationError",
                    "One or more validation errors occurred.", errors);
            }

            case UnauthorizedAccessException:
                return ((int)HttpStatusCode.Unauthorized, "Unauthorized",
                    "You are not authorized to perform this action.", null);

            case KeyNotFoundException:
                return ((int)HttpStatusCode.NotFound, "NotFound",
                    "The requested resource was not found.", null);

            case DbUpdateConcurrencyException:
                return ((int)HttpStatusCode.Conflict, "Conflict",
                    "The record was modified by another user. Please retry.", null);

            case DbUpdateException dbException when IsUniqueViolation(dbException):
                return ((int)HttpStatusCode.Conflict, "Conflict",
                    "A record with the same unique value already exists.", null);

            case DbUpdateException:
                return ((int)HttpStatusCode.InternalServerError, "ServerError",
                    "The request could not be completed. Please try again later.", null);

            case OperationCanceledException canceled when
                canceled.CancellationToken.IsCancellationRequested:
                // Client disconnected / request aborted - not a server error.
                return (499, "ClientClosedRequest", "The request was cancelled.", null);

            default:
                return ((int)HttpStatusCode.InternalServerError, "ServerError",
                    "An unexpected error occurred. Please try again later.", null);
        }
    }
}
