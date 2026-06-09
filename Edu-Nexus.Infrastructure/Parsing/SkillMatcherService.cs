using Edu_Nexus.Application.Helpers;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

public class SkillMatcherService : ISkillMatcherService
{
    private readonly EduNexusDbContext _db;
    private readonly ILogger<SkillMatcherService> _logger;

    public SkillMatcherService(EduNexusDbContext db, ILogger<SkillMatcherService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid?> MatchSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillName)) return null;
        var match = await _db.Database
            .SqlQuery<SkillMatchRow>($@"
                SELECT id AS ""Id"", similarity(name, {skillName}) AS ""Score""
                FROM skills
                WHERE name % {skillName} AND is_active = TRUE
                ORDER BY similarity(name, {skillName}) DESC
                LIMIT 1")
            .FirstOrDefaultAsync(cancellationToken);
        return match is { Score: >= 0.85 } ? match.Id : null;
    }

    public record SkillMatchRow(Guid Id, double Score);
}

public class SkillMatcherBatchService : ISkillMatcherBatchService
{
    private readonly EduNexusDbContext _db;
    private readonly ISkillMatcherService _single;
    private readonly ILlmService _llm;
    private readonly ILogger<SkillMatcherBatchService> _logger;
    private readonly Dictionary<string, Guid?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SkillMatcherBatchService(EduNexusDbContext db, ISkillMatcherService single,
        ILlmService llm, ILogger<SkillMatcherBatchService> logger)
    {
        _db = db;
        _single = single;
        _llm = llm;
        _logger = logger;
    }

    public async Task<Dictionary<string, Guid?>> MatchSkillsAsync(
        IEnumerable<string> skillNames, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in skillNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (_cache.TryGetValue(name, out var cached)) { result[name] = cached; continue; }
            var id = await _single.MatchSkillAsync(name, cancellationToken);
            _cache[name] = id;
            result[name] = id;
        }
        return result;
    }

    public async Task<Dictionary<string, Guid>> MatchOrCreateBatchAsync(
        IEnumerable<string> skillNames, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var needsLlm = new List<(string name, List<SkillCandidate> candidates)>();

        foreach (var name in skillNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (name.Trim().Length < 3)
            {
                result[name] = await InsertAiGeneratedAsync(name, cancellationToken);
                continue;
            }

            var candidates = await _db.Database
                .SqlQuery<SkillCandidate>($@"
                    SELECT id AS ""Id"", name AS ""Name"", description AS ""Description"",
                           similarity(name, {name}) AS ""Sim""
                    FROM skills
                    WHERE is_active = TRUE AND similarity(name, {name}) >= 0.3
                    ORDER BY similarity(name, {name}) DESC
                    LIMIT 5")
                .ToListAsync(cancellationToken);

            if (candidates.FirstOrDefault()?.Sim >= 0.92) { result[name] = candidates[0].Id; continue; }
            if (candidates.Count == 0) { result[name] = await InsertAiGeneratedAsync(name, cancellationToken); continue; }
            needsLlm.Add((name, candidates));
        }

        if (needsLlm.Count > 0)
        {
            var decisions = await CallLlmBatchAsync(needsLlm, cancellationToken);
            foreach (var (name, candidates) in needsLlm)
            {
                var d = decisions.GetValueOrDefault(name);
                var valid = d?.MatchedId != null && candidates.Any(c => c.Id == d.MatchedId);
                var decided = DecideMatch(d?.Confidence ?? 0m, valid ? d!.MatchedId : null);
                result[name] = decided ?? await InsertAiGeneratedAsync(name, cancellationToken);
            }
        }

        return result;
    }

    /// <summary>Pure decision (testable): confidence >= 0.85 AND a matchedId present -> return it, else null.</summary>
    public static Guid? DecideMatch(decimal confidence, Guid? matchedId)
        => (matchedId.HasValue && confidence >= 0.85m) ? matchedId : null;

    public void CacheSkillMapping(string skillName, Guid? skillId)
    {
        if (!string.IsNullOrWhiteSpace(skillName)) _cache[skillName] = skillId;
    }

    public void ClearCache() => _cache.Clear();

    private async Task<Guid> InsertAiGeneratedAsync(string name, CancellationToken ct)
    {
        var skill = new Skill
        {
            Name = name,
            Slug = SlugHelper.GenerateSlug(name),
            Category = "uncategorized",
            Major = "IT",
            Description = "[AI-GENERATED] Skill này được tự động tạo từ Roadmap Generator. Cần admin verify tên/category/major.",
            DifficultyLevel = (short)3,
            IsActive = true,
        };
        _db.Skills.Add(skill);
        await _db.SaveChangesAsync(ct);
        return skill.Id;
    }

    private async Task<Dictionary<string, LlmDecision>> CallLlmBatchAsync(
        List<(string name, List<SkillCandidate> candidates)> items, CancellationToken ct)
    {
        var payload = items.Select(i => new
        {
            newSkill = i.name,
            candidates = i.candidates.Select(c => new { id = c.Id, name = c.Name, description = c.Description })
        });
        const string system = "Bạn là expert phân loại kỹ năng nghề nghiệp. Với MỖI skill mới, quyết định nó có trùng nghĩa với 1 candidate có sẵn không. MATCH khi cùng 1 kỹ năng dù viết tắt/đa ngôn ngữ (\"ReactJS\"<->\"React\",\"ML\"<->\"Machine Learning\"). KHÔNG match khi khác phạm vi (\"React\"<->\"React Native\",\"SQL\"<->\"PostgreSQL\"). Confidence trong [0,1], chỉ >=0.85 khi rất chắc.";
        var user = "Phân loại: " + System.Text.Json.JsonSerializer.Serialize(payload) +
            "\nTrả về JSON: { \"decisions\": [ { \"newSkill\": \"string\", \"matchedId\": \"uuid|null\", \"confidence\": 0.0 } ] }";

        var resp = await _llm.ChatJsonAsync(system, user, "fast", 1500, ct: ct);
        await _llm.LogQueryAsync("skill_match", resp, "gpt-4o-mini");
        var map = new Dictionary<string, LlmDecision>(StringComparer.OrdinalIgnoreCase);
        if (!resp.Success) return map;
        try
        {
            var dto = System.Text.Json.JsonSerializer.Deserialize<DecisionsDto>(resp.Content, JsonOpts);
            foreach (var d in dto?.Decisions ?? new())
            {
                Guid? mid = Guid.TryParse(d.MatchedId, out var g) ? g : null;
                map[d.NewSkill] = new LlmDecision(mid, d.Confidence);
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "skill match LLM parse failed"); }
        return map;
    }

    public record SkillCandidate(Guid Id, string Name, string? Description, double Sim);
    private record LlmDecision(Guid? MatchedId, decimal Confidence);
    private record DecisionsDto(List<DecisionDto>? Decisions);
    private record DecisionDto(
        [property: System.Text.Json.Serialization.JsonPropertyName("newSkill")] string NewSkill,
        [property: System.Text.Json.Serialization.JsonPropertyName("matchedId")] string? MatchedId,
        [property: System.Text.Json.Serialization.JsonPropertyName("confidence")] decimal Confidence);
}
