namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IJdParser
{
    Task<ParsedJdResult> ParseAsync(string rawContent, CancellationToken cancellationToken = default);
}

public record ParsedJdResult(
    string JobTitle,
    string JobRoleCategory,
    string SeniorityLevel,
    int? SalaryMin,
    int? SalaryMax,
    string? Currency,
    IReadOnlyList<ParsedJdSkill> HardSkills,
    IReadOnlyList<ParsedJdSkill> SoftSkills);

public record ParsedJdSkill(string SkillNameRaw, bool IsMandatory);
