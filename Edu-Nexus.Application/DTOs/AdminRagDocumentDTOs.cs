namespace Edu_Nexus.Application.DTOs;

public record RagDocumentAcceptedDto(
    Guid Id,
    string Title,
    string EmbeddingStatus,
    int ChunksCount);

public record AdminRagDocumentDto(
    Guid Id,
    string Title,
    string SourceType,
    string? FileUrl,
    IReadOnlyList<Guid> RelatedSkillIds,
    int ChunksCount,
    string EmbeddingStatus,
    DateTime CreatedAt);
