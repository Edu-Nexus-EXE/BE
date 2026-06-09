using System.Text.Json;
using System.Text.Json.Serialization;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Domain.Enums.LearningResources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

public class RoadmapGeneratorService : IRoadmapGeneratorService
{
    private readonly ILlmService _llm;
    private readonly IRagService _rag;
    private readonly ISkillMatcherBatchService _skillMatcher;
    private readonly IResourceSuggestionService _resourceSuggestion;
    private readonly IUrlVerificationService _urlVerifier;
    private readonly IUnitOfWork _uow;
    private readonly int _maxTokens;
    private readonly string[] _sourceTypes;
    private readonly ILogger<RoadmapGeneratorService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public RoadmapGeneratorService(ILlmService llm, IRagService rag,
        ISkillMatcherBatchService skillMatcher, IResourceSuggestionService resourceSuggestion,
        IUrlVerificationService urlVerifier, IUnitOfWork uow, IConfiguration config,
        ILogger<RoadmapGeneratorService> logger)
    {
        _llm = llm; _rag = rag; _skillMatcher = skillMatcher;
        _resourceSuggestion = resourceSuggestion; _urlVerifier = urlVerifier; _uow = uow;
        _maxTokens = config.GetValue<int?>("OpenAI:MaxTokens:Roadmap") ?? 4000;
        _sourceTypes = config.GetSection("Rag:AllowedSourceTypes").Get<string[]>()
            ?? new[] { "fptu_curriculum", "fptu_syllabus", "external_doc" };
        _logger = logger;
    }

    public async Task<RoadmapGenerationResult> GenerateAsync(Guid roadmapId, Guid gapAnalysisId,
        string jobTitle, List<GapSkillInput> gapSkills, CancellationToken cancellationToken = default)
    {
        try
        {
            var targets = gapSkills
                .Where(g => g.GapStatus is "missing" or "needs_upgrade")
                .OrderByDescending(g => g.UrgencyScore).ToList();
            if (targets.Count == 0)
                return new RoadmapGenerationResult(new(), 0, jobTitle, false, "Không có skill gap để dựng roadmap");

            var ragQuery = $"{jobTitle} lộ trình {string.Join(" ", targets.Select(t => t.SkillName))}";
            var chunks = await _rag.SearchAsync(ragQuery, 8, _sourceTypes, 0.65, cancellationToken);
            var ragContext = chunks.Count == 0 ? "(không có tài liệu)" : string.Join("\n---\n", chunks.Select(c => c.Content));

            const string system = "Bạn là learning path designer. Tạo skill tree roadmap dựa trên gap analysis. Sắp xếp theo prerequisite (skill nền học trước). Ước tính giờ học hợp lý.";
            var user = "Vị trí: " + jobTitle + "\n" +
                "Gap skills (ưu tiên cao->thấp): " + string.Join(", ", targets.Select(t => $"{t.SkillName}({t.GapStatus},urgency={t.UrgencyScore})")) + "\n" +
                "Tài liệu FPTU: \"\"\"" + ragContext + "\"\"\"\n\n" +
                "Trả về JSON: { \"title\": \"string\", \"estimated_total_hours\": number, \"nodes\": [ { \"sequence_order\": 1, \"skill_name\": \"string\", \"description\": \"string\", \"estimated_hours\": 10, \"is_prerequisite\": true, \"prerequisite_node_indexes\": [] } ] }";

            var resp = await _llm.ChatJsonAsync(system, user, "smart", _maxTokens, 0.3, cancellationToken);
            await _llm.LogQueryAsync("roadmap_gen", resp, "gpt-4o");
            if (!resp.Success)
                return new RoadmapGenerationResult(new(), null, jobTitle, false, "LLM call failed");

            var dto = JsonSerializer.Deserialize<RoadmapDto>(resp.Content, JsonOpts);
            if (dto?.Nodes == null || dto.Nodes.Count == 0)
                return new RoadmapGenerationResult(new(), null, jobTitle, false, "Roadmap rỗng");

            var nameToId = await _skillMatcher.MatchOrCreateBatchAsync(dto.Nodes.Select(n => n.SkillName), cancellationToken);

            var nodes = new List<GeneratedNode>();
            foreach (var n in dto.Nodes.OrderBy(n => n.SequenceOrder))
            {
                nameToId.TryGetValue(n.SkillName, out var skillId);
                var suggested = await EnsureResourcesAsync(skillId, n.SkillName, cancellationToken);
                nodes.Add(new GeneratedNode(
                    SkillName: n.SkillName,
                    SkillId: skillId == Guid.Empty ? null : skillId,
                    Description: n.Description ?? "",
                    SequenceOrder: (short)n.SequenceOrder,
                    EstimatedHours: n.EstimatedHours,
                    PrerequisiteSequences: n.PrerequisiteNodeIndexes ?? new(),
                    SuggestedResources: suggested));
            }

            return new RoadmapGenerationResult(nodes, dto.EstimatedTotalHours, dto.Title ?? jobTitle, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Roadmap generation failed for {RoadmapId}", roadmapId);
            return new RoadmapGenerationResult(new(), null, jobTitle, false, ex.Message);
        }
    }

    private async Task<List<GeneratedResource>?> EnsureResourcesAsync(Guid skillId, string skillName, CancellationToken ct)
    {
        if (skillId == Guid.Empty) return null;
        var existing = await _uow.SkillResources.FindAsync(sr => sr.SkillId == skillId, "", ct);
        if (existing.Count() >= 2) return null;

        var suggestions = await _resourceSuggestion.SuggestResourcesAsync(skillName, null, 3, ct);
        if (suggestions.Count == 0) return null;

        // Verify URLs using VerifyUrlsAsync → Dictionary<string, bool>
        var urlList = suggestions.Select(s => s.Url).ToList();
        var urlValidityMap = await _urlVerifier.VerifyUrlsAsync(urlList, ct);

        var generated = new List<GeneratedResource>();
        foreach (var s in suggestions)
        {
            var isValid = urlValidityMap.GetValueOrDefault(s.Url, false);

            // Parse string Type → LearningResourceType enum, fallback to Video
            var resourceType = Enum.TryParse<LearningResourceType>(s.Type, ignoreCase: true, out var parsedType)
                ? parsedType
                : LearningResourceType.Video;

            // Map IsFree to LearningResourceAccessType enum
            var accessType = s.IsFree ? LearningResourceAccessType.Free : LearningResourceAccessType.Affiliate;

            var resource = new Domain.Entities.LearningResource
            {
                Title = s.Title,
                Type = resourceType,
                Provider = s.Provider,
                Url = s.Url,
                IsFree = s.IsFree,
                AccessType = accessType,
                Language = (s.Language ?? "en").Length <= 5 ? (s.Language ?? "en") : "en",
                DurationMinutes = s.DurationMinutes,
                NeedsAdminReview = true,
                IsActive = isValid,
            };
            _uow.LearningResources.Add(resource);
            await _uow.SaveChangesAsync(ct);

            _uow.SkillResources.Add(new Domain.Entities.SkillResource
            {
                SkillId = skillId,
                ResourceId = resource.Id,
                IsPrimary = false,
            });
            await _uow.SaveChangesAsync(ct);

            generated.Add(new GeneratedResource(s.Title, s.Type, s.Url, s.Provider, s.IsFree));
        }
        return generated;
    }

    private record RoadmapDto(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("estimated_total_hours")] int? EstimatedTotalHours,
        [property: JsonPropertyName("nodes")] List<NodeDto>? Nodes);

    private record NodeDto(
        [property: JsonPropertyName("sequence_order")] int SequenceOrder,
        [property: JsonPropertyName("skill_name")] string SkillName,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("estimated_hours")] int? EstimatedHours,
        [property: JsonPropertyName("is_prerequisite")] bool IsPrerequisite,
        [property: JsonPropertyName("prerequisite_node_indexes")] List<int>? PrerequisiteNodeIndexes);
}
