namespace Edu_Nexus.Application.DTOs;

public record SubmitJdRequest(
    string SourceType,
    string? SourceUrl,
    string? RawContent);

public record JdSubmissionAcceptedDto(
    Guid Id,
    string SourceType,
    string ParseStatus,
    DateTime CreatedAt);

public record JdSubmissionListItemDto(
    Guid Id,
    string SourceType,
    string? JobTitle,
    string? JobRoleCategory,
    string? SeniorityLevel,
    string ParseStatus,
    DateTime CreatedAt,
    bool HasAssessmentPath,
    bool HasGapAnalysis,
    bool HasActiveRoadmap);

public record JdSkillDto(
    Guid Id,
    string SkillNameRaw,
    Guid? SkillId,
    bool IsMandatory);

public record AssessmentPathInJdDto(Guid Id, string PathType);

public record JdSubmissionDetailDto(
    Guid Id,
    string SourceType,
    string? SourceUrl,
    string? JobTitle,
    string? JobRoleCategory,
    string? SeniorityLevel,
    int? SalaryMin,
    int? SalaryMax,
    string? Currency,
    string ParseStatus,
    DateTime? ParsedAt,
    IReadOnlyList<JdSkillDto> HardSkills,
    IReadOnlyList<JdSkillDto> SoftSkills,
    AssessmentPathInJdDto? AssessmentPath,
    DateTime CreatedAt);

public record PaginationDto(int Page, int PageSize, int TotalItems, int TotalPages);

public record PagedResult<T>(IReadOnlyList<T> Data, PaginationDto Pagination);
