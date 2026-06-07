using System.Text.Json;

namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface ILlmResponseValidator
{
    ValidationResult ValidateGapAnalysis(JsonDocument doc);
    ValidationResult ValidateJdParse(JsonDocument doc);
    ValidationResult ValidateAssessment(JsonDocument doc);
    ValidationResult ValidateResources(JsonDocument doc);
}

public record ValidationResult(bool IsValid, List<string> Errors)
{
    public static ValidationResult Success => new(true, new());

    public static ValidationResult Fail(params string[] errors)
        => new(false, errors.ToList());
}
