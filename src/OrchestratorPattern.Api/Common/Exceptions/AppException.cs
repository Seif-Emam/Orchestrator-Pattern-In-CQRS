using System.Net;

namespace OrchestratorPattern.Api.Common.Exceptions;

public abstract class AppException : Exception
{
    public string ErrorCode { get; }
    public HttpStatusCode StatusCode { get; }
    public IReadOnlyCollection<string>? Details { get; }

    protected AppException(string message, string errorCode, HttpStatusCode statusCode, IReadOnlyCollection<string>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details;
    }
}
