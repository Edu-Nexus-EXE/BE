namespace Edu_Nexus.Application.DTOs;

public record CreateAssessmentPathRequest(string PathType);

public record AssessmentPathDto(
    Guid Id,
    Guid JdId,
    string PathType,
    DateTime CreatedAt);
