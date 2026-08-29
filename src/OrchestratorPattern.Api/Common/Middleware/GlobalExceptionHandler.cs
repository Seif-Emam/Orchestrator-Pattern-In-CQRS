using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using OrchestratorPattern.Api.Common.Constants;
using OrchestratorPattern.Api.Common.Exceptions;
using OrchestratorPattern.Api.Common.Models;

namespace OrchestratorPattern.Api.Common.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, errorCode, message, details) = exception switch
        {
            AppException appEx => (
                (int)appEx.StatusCode,
                appEx.ErrorCode,
                appEx.Message,
                appEx.Details
            ),
            FluentValidation.ValidationException fvEx => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationError,
                "Validation failed for one or more fields.",
                (IReadOnlyCollection<string>)fvEx.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").ToList()
            ),
            KeyNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                ErrorCodes.NotFound,
                notFoundEx.Message,
                null
            ),
            InvalidOperationException invalidOpEx => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.DomainRuleViolation,
                invalidOpEx.Message,
                null
            ),
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.ValidationError,
                argEx.Message,
                null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalServerError,
                "An unexpected internal error occurred. Please try again later or contact support.",
                null
            )
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}, Message: {Message}", traceId, exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled business/client exception: Status {StatusCode}, ErrorCode {ErrorCode}, Message: {Message}, TraceId: {TraceId}",
                statusCode, errorCode, message, traceId);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(
            new ApiError
            {
                Code = errorCode,
                Message = message,
                Details = details
            },
            traceId
        );

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions), cancellationToken);

        return true;
    }
}
