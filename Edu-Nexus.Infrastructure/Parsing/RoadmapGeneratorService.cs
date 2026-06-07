using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

/// <summary>
/// AI-powered roadmap generation from gap analysis.
/// Uses Semantic Kernel, RAG retrieval, and skill matching to create
/// structured learning paths with prerequisites and resource suggestions.
/// </summary>
public class RoadmapGeneratorService : IRoadmapGeneratorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISkillMatcherBatchService _skillMatcher;
    private readonly IResourceSuggestionService _resourceSuggestion;
    private readonly IRagService _rag;
    private readonly ILlmResponseValidator _validator;
    private readonly ILogger<RoadmapGeneratorService> _logger;

    public RoadmapGeneratorService(
        IUnitOfWork unitOfWork,
        ISkillMatcherBatchService skillMatcher,
        IResourceSuggestionService resourceSuggestion,
        IRagService rag,
        ILlmResponseValidator validator,
        ILogger<RoadmapGeneratorService> logger)
    {
        _unitOfWork = unitOfWork;
        _skillMatcher = skillMatcher;
        _resourceSuggestion = resourceSuggestion;
        _rag = rag;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RoadmapGenerationResult> GenerateAsync(
        Guid roadmapId,
        Guid gapAnalysisId,
        string jobTitle,
        List<GapSkillInput> gapSkills,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "RoadmapGeneratorService.GenerateAsync: roadmapId={RoadmapId}, gapAnalysisId={GapAnalysisId}, jobTitle={JobTitle}, skillCount={SkillCount}",
            roadmapId, gapAnalysisId, jobTitle, gapSkills.Count);

        // TODO: Implement real roadmap generation:
        // 1. Filter gap skills (missing + needs_upgrade by urgency score)
        // 2. For each skill:
        //    a. Retrieve RAG context via _rag.SearchBySkillAsync(skillName)
        //    b. Call Semantic Kernel with RAG context + job context to generate:
        //       - learning_path (sequence of sub-skills)
        //       - estimated_hours
        //       - prerequisites (references to other gap skills)
        //       - description
        //    c. Map skill_name -> skill_id via _skillMatcher.MatchSkillsAsync()
        //    d. Get existing resources for skill; if < 2, call _resourceSuggestion
        //    e. Validate LLM response via _validator.ValidateResources()
        // 3. Build prerequisite graph (detect circular deps, topological sort)
        // 4. Assign sequence order based on dependencies
        // 5. Calculate EstimatedTotalHours = sum of node hours
        // 6. Return RoadmapGenerationResult with all nodes

        // Stub: return empty nodes (prevents breaking callers during implementation)
        _logger.LogWarning("RoadmapGeneratorService: returning stub result (no real implementation yet)");

        return await Task.FromResult(new RoadmapGenerationResult(
            Nodes: new(),
            EstimatedTotalHours: null,
            Title: jobTitle,
            Success: false,
            ErrorMessage: "RoadmapGeneratorService not yet implemented (stub)"));
    }
}
