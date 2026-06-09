using System.Text.Json;
using System.Text.Json.Serialization;
using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

public class ResourceSuggestionService : IResourceSuggestionService
{
    private readonly ILlmService _llm;
    private readonly int _maxTokens;
    private readonly ILogger<ResourceSuggestionService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ResourceSuggestionService(ILlmService llm, IConfiguration config, ILogger<ResourceSuggestionService> logger)
    {
        _llm = llm;
        _maxTokens = config.GetValue<int?>("OpenAI:MaxTokens:ResourceSuggestion") ?? 600;
        _logger = logger;
    }

    public async Task<List<SuggestedResource>> SuggestResourcesAsync(
        string skillName, string? skillCategory, int count = 3, CancellationToken cancellationToken = default)
    {
        const string system = "Bạn là chuyên gia gợi ý nguồn học cho sinh viên Việt Nam. Trả về ĐÚNG 3 resources: 1 video YouTube, 1 doc/article miễn phí, 1 paid course. URL phải thật; nếu không chắc vẫn trả nhưng đánh dấu cẩn thận.";
        var user = "Skill: \"" + skillName + "\" — " + (skillCategory ?? "general") + "\n" +
            "Trả về JSON: { \"resources\": [ { \"title\": \"string\", \"type\": \"video|article|course|documentation\", " +
            "\"provider\": \"string\", \"url\": \"https://...\", \"is_free\": true, \"language\": \"vi|en\", \"estimated_minutes\": 60 } ] }";

        var resp = await _llm.ChatJsonAsync(system, user, "fast", _maxTokens, ct: cancellationToken);
        await _llm.LogQueryAsync("resource_suggest", resp, "gpt-4o-mini");
        if (!resp.Success)
        {
            _logger.LogWarning("Resource suggestion LLM failed for skill {Skill}", skillName);
            return new();
        }

        try
        {
            var dto = JsonSerializer.Deserialize<ResourcesDto>(resp.Content, JsonOpts);
            return (dto?.Resources ?? new()).Select(r => new SuggestedResource(
                Title: r.Title ?? "",
                Type: r.Type ?? "article",
                Url: r.Url ?? "",
                Provider: r.Provider,
                IsFree: r.IsFree,
                Language: r.Language ?? "en",
                DurationMinutes: r.EstimatedMinutes)).Where(r => !string.IsNullOrWhiteSpace(r.Url)).ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Resource suggestion JSON parse failed for {Skill}", skillName);
            return new();
        }
    }

    private record ResourcesDto([property: JsonPropertyName("resources")] List<ResourceDto>? Resources);
    private record ResourceDto(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("provider")] string? Provider,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("is_free")] bool IsFree,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("estimated_minutes")] int? EstimatedMinutes);
}
