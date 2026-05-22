namespace Edu_Nexus.Application.DTOs;

public record GapAnalysisAcceptedDto(
    Guid Id,
    Guid JdId,
    string InputSource,
    int Version,
    string Status);

public record GapSkillDto(
    Guid Id,
    string SkillName,
    Guid? SkillId,
    string GapStatus,
    string? CurrentLevel,
    string TargetLevel,
    int? UrgencyScore,
    string? Reasoning,
    bool IsMandatoryInJd);

public record GapAnalysisDetailDto(
    Guid Id,
    int Version,
    bool IsLatest,
    string InputSource,
    string Status,
    string? Summary,
    IReadOnlyList<GapSkillDto> Skills,
    DateTime? CompletedAt);
