using System.Net;
using OrchestratorPattern.Api.Common.Constants;

namespace OrchestratorPattern.Api.Common.Exceptions;

public class DomainException : AppException
{
    public DomainException(string message, string errorCode = ErrorCodes.DomainRuleViolation, HttpStatusCode statusCode = HttpStatusCode.UnprocessableEntity)
        : base(message, errorCode, statusCode)
    {
    }
}
