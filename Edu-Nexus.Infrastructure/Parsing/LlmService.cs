using System.Diagnostics;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Edu_Nexus.Infrastructure.Parsing;

public class LlmService : ILlmService
{
    private readonly Kernel _kernel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LlmService> _logger;

    public LlmService(Kernel kernel, IServiceScopeFactory scopeFactory, ILogger<LlmService> logger)
    {
        _kernel = kernel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<LlmResponse> ChatJsonAsync(string systemPrompt, string userPrompt,
        string serviceId = "fast", int maxTokens = 800, double temperature = 0.3, CancellationToken ct = default)
    {
        var attempt = await TryOnceAsync(systemPrompt, userPrompt, serviceId, maxTokens, temperature, ct);
        if (!attempt.Success)
        {
            _logger.LogWarning("LLM call failed (serviceId={ServiceId}), retrying once", serviceId);
            attempt = await TryOnceAsync(systemPrompt, userPrompt, serviceId, maxTokens, temperature, ct);
        }
        return attempt;
    }

    private async Task<LlmResponse> TryOnceAsync(string systemPrompt, string userPrompt,
        string serviceId, int maxTokens, double temperature, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var chat = _kernel.GetRequiredService<IChatCompletionService>(serviceId);
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userPrompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = "json_object",
                MaxTokens = maxTokens,
                Temperature = temperature,
            };

            var response = await chat.GetChatMessageContentAsync(history, settings, _kernel, ct);
            sw.Stop();

            IReadOnlyDictionary<string, object?> meta = response.Metadata ?? new Dictionary<string, object?>();
            var (pt, ctk) = ExtractTokens(meta);
            var model = serviceId == "smart" ? "gpt-4o" : "gpt-4o-mini";

            return new LlmResponse(response.Content ?? "{}", pt, ctk,
                CalcCost(model, pt, ctk), (int)sw.ElapsedMilliseconds, true);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "LLM call exception (serviceId={ServiceId})", serviceId);
            return new LlmResponse("{}", 0, 0, 0m, (int)sw.ElapsedMilliseconds, false);
        }
    }

    public async Task LogQueryAsync(string queryType, LlmResponse response, string modelUsed,
        Guid? userId = null, Guid? entityId = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            uow.RagQueryLogs.Add(new RagQueryLog
            {
                UserId = userId,
                QueryType = queryType,
                EntityId = entityId,
                PromptTokens = response.PromptTokens,
                CompletionTokens = response.CompletionTokens,
                CostUsd = response.CostUsd,
                DurationMs = response.DurationMs,
                ModelUsed = modelUsed,
                Success = response.Success,
            });
            await uow.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log rag_query_log for {QueryType}", queryType);
        }
    }

    public static decimal CalcCost(string model, int pt, int ct) => model switch
    {
        "gpt-4o-mini" => (pt * 0.15m + ct * 0.60m) / 1_000_000m,
        "gpt-4o" => (pt * 2.50m + ct * 10.0m) / 1_000_000m,
        _ => 0m,
    };

    private static int ToInt(object? v) => v is null ? 0 : Convert.ToInt32(v);

    private static (int prompt, int completion) ExtractTokens(IReadOnlyDictionary<string, object?> meta)
    {
        // SK 1.76 OpenAI connector: meta["Usage"] is an OpenAI.Chat.ChatTokenUsage-like object.
        if (meta.TryGetValue("Usage", out var usage) && usage is not null)
        {
            var prompt = ReadIntProp(usage, "InputTokenCount", "PromptTokens", "PromptTokenCount");
            var completion = ReadIntProp(usage, "OutputTokenCount", "CompletionTokens", "CompletionTokenCount");
            if (prompt > 0 || completion > 0) return (prompt, completion);
        }
        // Fallback: some providers flatten keys.
        return (ToInt(meta.GetValueOrDefault("Usage.PromptTokens")),
                ToInt(meta.GetValueOrDefault("Usage.CompletionTokens")));
    }

    private static int ReadIntProp(object obj, params string[] candidateNames)
    {
        var type = obj.GetType();
        foreach (var name in candidateNames)
        {
            var prop = type.GetProperty(name);
            if (prop?.GetValue(obj) is { } val)
            {
                try { return Convert.ToInt32(val); } catch { /* try next */ }
            }
        }
        return 0;
    }
}
