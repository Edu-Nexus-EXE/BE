using Edu_Nexus.Domain.Entities;

namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IRoadmapGeneratorService
{
    /// <summary>
    /// Generate roadmap nodes from gap analysis using AI.
    /// - Takes gap skills (missing + needs_upgrade)
    /// - Uses Semantic Kernel + RAG retrieval for context
    /// - Maps skill names to Skill IDs via SkillMatcherService
    /// - Creates nodes with prerequisites based on skill dependencies
    /// - Suggests resources if skill has < 2 existing resources
    /// - Calculates estimated hours per node and total
    /// </summary>
    Task<RoadmapGenerationResult> GenerateAsync(
        Guid roadmapId,
        Guid gapAnalysisId,
        string jobTitle,
        List<GapSkillInput> gapSkills,
        CancellationToken cancellationToken = default);
}

public record GapSkillInput(
    string SkillName,
    string GapStatus,    // "missing" | "needs_upgrade" | "have"
    int UrgencyScore,    // 1-10
    bool IsMandatory);

public record RoadmapGenerationResult(
    List<GeneratedNode> Nodes,
    int? EstimatedTotalHours,
    string? Title,
    bool Success,
    string? ErrorMessage = null);

public record GeneratedNode(
    string SkillName,
    Guid? SkillId,
    string Description,
    short SequenceOrder,
    int? EstimatedHours,
    List<int> PrerequisiteSequences,  // indices into Nodes list
    List<GeneratedResource>? SuggestedResources);

public record GeneratedResource(
    string Title,
    string Type,
    string Url,
    string? Provider,
    bool IsFree);
