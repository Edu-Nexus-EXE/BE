namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IGapAnalyzer
{
    Task<GapAnalysisResult> AnalyzeAsync(
        GapAnalysisInput input,
        CancellationToken cancellationToken = default);
}

public record GapAnalysisInput(
    string JobTitle,
    string JobRoleCategory,
    string? SeniorityLevel,
    IReadOnlyList<JdSkillInput> JdSkills,
    IReadOnlyList<CvSkillInput> CvSkills,
    IReadOnlyList<AssessmentSkillScore> AssessmentScores,
    OnboardingSnapshot? Onboarding);

public record JdSkillInput(string SkillName, bool IsMandatory, bool IsHardSkill);

public record CvSkillInput(string SkillName, string ProficiencyLevel, decimal? YearsExp);

public record AssessmentSkillScore(string SkillName, int Score, int MaxScore, string ProficiencyLevel);

public record OnboardingSnapshot(
    string? Major,
    string? ProficiencyLevel,
    string? WeeklyStudyHours,
    string? PrimaryGoal);

public record GapAnalysisResult(
    string Summary,
    IReadOnlyList<GapSkillOutcome> Skills);

public record GapSkillOutcome(
    string SkillName,
    string GapStatus,
    string? CurrentLevel,
    string TargetLevel,
    int UrgencyScore,
    string Reasoning,
    bool IsMandatoryInJd);
