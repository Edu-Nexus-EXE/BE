namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface ILlmService
{
    Task<LlmResponse> ChatJsonAsync(
        string systemPrompt,
        string userPrompt,
        string serviceId = "fast",
        int maxTokens = 800,
        double temperature = 0.3,
        CancellationToken ct = default);

    Task LogQueryAsync(string queryType, LlmResponse response, string modelUsed,
        Guid? userId = null, Guid? entityId = null);
}

public record LlmResponse(
    string Content, int PromptTokens, int CompletionTokens,
    decimal CostUsd, int DurationMs, bool Success);
