namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface ICvParser
{
    Task<ParsedCvResult> ParseAsync(string anonymizedText, CancellationToken cancellationToken = default);
}

public record ParsedCvResult(
    decimal? TotalExperienceYears,
    IReadOnlyList<ParsedCvSkill> Skills);

public record ParsedCvSkill(
    string SkillName,
    string ProficiencyLevel,
    decimal? YearsExp,
    string? Evidence);
