using System.Net;
using OrchestratorPattern.Api.Common.Constants;

namespace OrchestratorPattern.Api.Common.Exceptions;

public class ValidationException : AppException
{
    public ValidationException(IReadOnlyCollection<string> errors)
        : base("One or more validation errors occurred.", ErrorCodes.ValidationError, HttpStatusCode.BadRequest, errors)
    {
    }

    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage, ErrorCodes.ValidationError, HttpStatusCode.BadRequest, new[] { $"{propertyName}: {errorMessage}" })
    {
    }
}
