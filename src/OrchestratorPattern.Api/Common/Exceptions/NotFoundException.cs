using System.Net;
using OrchestratorPattern.Api.Common.Constants;

namespace OrchestratorPattern.Api.Common.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message, string errorCode = ErrorCodes.NotFound)
        : base(message, errorCode, HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with ID '{key}' was not found.", ErrorCodes.NotFound, HttpStatusCode.NotFound)
    {
    }
}
