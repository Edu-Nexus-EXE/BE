namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IResourceSuggestionService
{
    /// <summary>
    /// Generate 2-3 resource suggestions for a skill.
    /// Used when skill has < 2 existing resources in roadmap/assessment context.
    /// Returns list of suggested resources (title, type, url, provider, etc.)
    /// </summary>
    Task<List<SuggestedResource>> SuggestResourcesAsync(
        string skillName,
        string? skillCategory,
        int count = 3,
        CancellationToken cancellationToken = default);
}

public record SuggestedResource(
    string Title,
    string Type,      // "video", "article", "course", "documentation"
    string Url,
    string? Provider, // "YouTube", "Udemy", "MDN", etc.
    bool IsFree,
    string? Language = "en",
    int? DurationMinutes = null);
