using System.Text.Json.Serialization;

namespace OrchestratorPattern.Api.Common.Models;

public class ApiError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? Details { get; init; }
}
