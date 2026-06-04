namespace Edu_Nexus.Application.DTOs;

public record AdminJdListItemDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    string SourceType,
    string? SourceUrl,
    string? JobTitle,
    string ParseStatus,
    string? ParseError,
    DateTime CreatedAt,
    DateTime? ParsedAt);

public record MarkJdInvalidRequest(string? Reason);
