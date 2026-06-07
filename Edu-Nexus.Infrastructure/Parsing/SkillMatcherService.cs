using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

public class SkillMatcherService : ISkillMatcherService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SkillMatcherService> _logger;

    public SkillMatcherService(IUnitOfWork unitOfWork, ILogger<SkillMatcherService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid?> MatchSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return null;

        // TODO: Implement skill matching logic:
        // 1. Exact match (case-insensitive): WHERE LOWER(name) = LOWER(skillName)
        // 2. PostgreSQL pg_trgm similarity: WHERE similarity(name, skillName) > 0.6
        //    (requires CREATE EXTENSION pg_trgm in migrations)
        // 3. If no match: optionally call LLM for fuzzy matching
        //    (ask "Is 'ReactJS' same as 'React'?" -> yes -> return react skill id)

        _logger.LogDebug("SkillMatcherService.MatchSkillAsync: {SkillName}", skillName);

        // Stub: return null (no match found - let caller handle)
        return await Task.FromResult<Guid?>(null);
    }
}

public class SkillMatcherBatchService : ISkillMatcherBatchService
{
    private readonly ISkillMatcherService _singleMatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SkillMatcherBatchService> _logger;
    private readonly Dictionary<string, Guid?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SkillMatcherBatchService(
        ISkillMatcherService singleMatcher,
        IUnitOfWork unitOfWork,
        ILogger<SkillMatcherBatchService> logger)
    {
        _singleMatcher = singleMatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Dictionary<string, Guid?>> MatchSkillsAsync(
        IEnumerable<string> skillNames,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);
        var toMatch = new List<string>();

        // Check cache first
        foreach (var name in skillNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (_cache.TryGetValue(name, out var cachedId))
            {
                result[name] = cachedId;
            }
            else
            {
                toMatch.Add(name);
            }
        }

        // Batch match remaining skills
        if (toMatch.Count > 0)
        {
            _logger.LogInformation("SkillMatcherBatchService: batch matching {Count} skills", toMatch.Count);

            // TODO: Implement efficient batch matching:
            // 1. Query all skills once: SELECT id, name FROM skills WHERE is_active = true
            // 2. For each skillName in toMatch:
            //    - Try exact match first
            //    - Try pg_trgm similarity (single query with OR'd conditions)
            //    - Cache result
            // 3. Return dict

            foreach (var name in toMatch)
            {
                var matched = await _singleMatcher.MatchSkillAsync(name, cancellationToken);
                result[name] = matched;
                _cache[name] = matched;
            }
        }

        return result;
    }

    public void CacheSkillMapping(string skillName, Guid? skillId)
    {
        if (!string.IsNullOrWhiteSpace(skillName))
            _cache[skillName] = skillId;
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
