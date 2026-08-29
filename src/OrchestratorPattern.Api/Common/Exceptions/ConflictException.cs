using System.Net;
using OrchestratorPattern.Api.Common.Constants;

namespace OrchestratorPattern.Api.Common.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message, string errorCode = ErrorCodes.ResourceConflict)
        : base(message, errorCode, HttpStatusCode.Conflict)
    {
    }
}
