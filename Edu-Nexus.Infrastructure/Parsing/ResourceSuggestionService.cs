using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

/// <summary>
/// Stub implementation for resource suggestion. To be replaced with AI-based
/// suggestion service that uses RAG + LLM to find/generate relevant resources.
/// </summary>
public class ResourceSuggestionService : IResourceSuggestionService
{
    private readonly ILogger<ResourceSuggestionService> _logger;

    public ResourceSuggestionService(ILogger<ResourceSuggestionService> logger)
    {
        _logger = logger;
    }

    public async Task<List<SuggestedResource>> SuggestResourcesAsync(
        string skillName,
        string? skillCategory,
        int count = 3,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement real suggestion logic:
        // 1. Query RAG service for skill-related chunks from FPTU docs
        // 2. Use LLM to generate 2-3 resource suggestions based on chunks
        // 3. Verify URLs via IUrlVerificationService
        // 4. Validate response schema via ILlmResponseValidator
        // For now return empty list to avoid breaking callers.

        _logger.LogInformation("ResourceSuggestionService stub called for skill={Skill}", skillName);

        await Task.Delay(100, cancellationToken); // Simulate async work

        return new List<SuggestedResource>();
    }
}
