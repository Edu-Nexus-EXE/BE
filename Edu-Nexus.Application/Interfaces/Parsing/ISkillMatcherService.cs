namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface ISkillMatcherService
{
    /// <summary>
    /// Match a single skill name to a Skill entity using hybrid approach:
    /// 1. Exact match (case-insensitive)
    /// 2. PostgreSQL pg_trgm similarity (threshold ~0.6)
    /// 3. LLM-based fuzzy matching (if configured)
    /// </summary>
    Task<Guid?> MatchSkillAsync(string skillName, CancellationToken cancellationToken = default);
}

public interface ISkillMatcherBatchService
{
    /// <summary>
    /// Batch match multiple skill names to Skill entities.
    /// Returns dict of skillName -> skillId (null if no match found).
    /// Uses efficient batch queries and caching to minimize DB hits.
    /// </summary>
    Task<Dictionary<string, Guid?>> MatchSkillsAsync(
        IEnumerable<string> skillNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache skill mappings for session to avoid repeated queries.
    /// Useful when processing many gaps/nodes with overlapping skills.
    /// </summary>
    void CacheSkillMapping(string skillName, Guid? skillId);

    /// <summary>
    /// Clear cache for fresh matching.
    /// </summary>
    void ClearCache();
}
