namespace Edu_Nexus.Application.Interfaces.Data;

public interface IRagService
{
    /// <summary>
    /// Search RAG chunks by semantic similarity using pgvector.
    /// </summary>
    /// <param name="query">Natural language query (will be embedded)</param>
    /// <param name="limit">Max chunks to return</param>
    /// <param name="sourceTypes">Filter by source type (optional)</param>
    /// <param name="minSimilarity">Cosine similarity threshold (0.0-1.0)</param>
    Task<List<RagChunkResult>> SearchAsync(
        string query,
        int limit = 5,
        string[]? sourceTypes = null,
        double minSimilarity = 0.5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search by skill name. Searches both skill_name and description fields.
    /// </summary>
    Task<List<RagChunkResult>> SearchBySkillAsync(
        string skillName,
        int limit = 5,
        CancellationToken cancellationToken = default);
}

public record RagChunkResult(
    Guid ChunkId,
    Guid DocumentId,
    string Content,
    string SourceType,       // "fptu_doc", "external_tutorial", etc.
    string? SourceUrl,
    double SimilarityScore,
    string? Title);
