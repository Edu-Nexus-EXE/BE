namespace Edu_Nexus.Application.DTOs;

public record CvUploadAcceptedDto(
    Guid Id,
    string FileName,
    int FileSize,
    string ParseStatus);

public record CvSkillDto(
    string SkillName,
    string ProficiencyLevel,
    decimal? YearsExp,
    string? Evidence);

public record CvDetailDto(
    Guid Id,
    string? FileName,
    string ParseStatus,
    IReadOnlyList<CvSkillDto>? ParsedSkills,
    decimal? TotalExperienceYears,
    DateTime? ParsedAt,
    string? ParseError);
