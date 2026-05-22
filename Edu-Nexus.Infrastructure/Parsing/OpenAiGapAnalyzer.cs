using System.Text.Json;
using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Edu_Nexus.Infrastructure.Parsing;

/// LLM-backed Gap Analyzer using OpenAI via SemanticKernel.
/// Activated when "Ai:Enabled" = true in configuration; otherwise the fake heuristic is used.
/// NOTE: RAG retrieval (FPTU docs context) is wired in S3.3. For now the prompt only uses
/// the user-provided JD + CV/assessment + onboarding signals.
public class OpenAiGapAnalyzer : IGapAnalyzer
{
    private readonly IChatCompletionService _chat;
    private readonly OpenAIPromptExecutionSettings _settings;
    private readonly ILogger<OpenAiGapAnalyzer> _logger;

    public OpenAiGapAnalyzer(IConfiguration configuration, ILogger<OpenAiGapAnalyzer> logger)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured. Set it via user-secrets.");
        var model = configuration["OpenAI:Models:Smart"] ?? "gpt-4o-mini";
        var maxTokens = configuration.GetValue<int?>("OpenAI:MaxTokens:GapAnalysis") ?? 1500;

        _chat = new OpenAIChatCompletionService(model, apiKey);
        _settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = maxTokens,
            Temperature = 0.2,
            ResponseFormat = "json_object",
        };
        _logger = logger;
    }

    public async Task<GapAnalysisResult> AnalyzeAsync(GapAnalysisInput input, CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(input);

        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userPrompt);

        var response = await _chat.GetChatMessageContentAsync(history, _settings, cancellationToken: cancellationToken);
        var raw = response.Content ?? "{}";

        _logger.LogDebug("Gap analysis LLM raw response: {Raw}", raw);

        return ParseResponse(raw, input);
    }

    private static string BuildSystemPrompt() =>
        """
        Bạn là cố vấn nghề nghiệp cho sinh viên FPT University. Nhiệm vụ: phân tích Gap giữa kỹ năng JD yêu cầu và kỹ năng hiện tại của ứng viên.

        Quy tắc:
        - Với mỗi JD hard skill, đánh giá `gapStatus`: "missing" (user chưa có), "needs_upgrade" (có nhưng dưới target), "have" (đạt hoặc vượt).
        - `currentLevel` ∈ {"none","beginner","intermediate","advanced"} dựa trên CV hoặc Assessment evidence.
        - `targetLevel` dựa vào seniority JD: intern/fresher→beginner, junior→intermediate, middle/senior/lead→advanced.
        - `urgencyScore` 1-10: skill bắt buộc + missing = 9, optional + missing = 6, mandatory + needs_upgrade = 6, optional + needs_upgrade = 4, have = 1-2.
        - `reasoning` ngắn gọn (1-2 câu tiếng Việt) giải thích kết luận, kèm bằng chứng từ CV/Assessment nếu có.
        - `summary` 2-4 câu tổng quan: vị trí gì, thiếu/cần upgrade bao nhiêu skill, đề xuất ưu tiên dựa trên thời gian học onboarding.

        Trả về JSON ĐÚNG schema:
        {
          "summary": "string",
          "skills": [
            {
              "skillName": "string (giữ nguyên tên từ JD)",
              "gapStatus": "missing|needs_upgrade|have",
              "currentLevel": "none|beginner|intermediate|advanced",
              "targetLevel": "beginner|intermediate|advanced",
              "urgencyScore": 1-10,
              "reasoning": "string",
              "isMandatoryInJd": true|false
            }
          ]
        }
        """;

    private static string BuildUserPrompt(GapAnalysisInput input)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# JD: {input.JobTitle}");
        sb.AppendLine($"Category: {input.JobRoleCategory}");
        sb.AppendLine($"Seniority: {input.SeniorityLevel ?? "junior"}");
        sb.AppendLine();

        sb.AppendLine("## JD Hard Skills:");
        foreach (var s in input.JdSkills.Where(s => s.IsHardSkill))
        {
            sb.AppendLine($"- {s.SkillName} ({(s.IsMandatory ? "MANDATORY" : "optional")})");
        }

        if (input.JdSkills.Any(s => !s.IsHardSkill))
        {
            sb.AppendLine();
            sb.AppendLine("## JD Soft Skills (FYI, không cần đánh giá gap):");
            foreach (var s in input.JdSkills.Where(s => !s.IsHardSkill))
            {
                sb.AppendLine($"- {s.SkillName}");
            }
        }

        if (input.CvSkills.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Skills từ CV:");
            foreach (var s in input.CvSkills)
            {
                var yrs = s.YearsExp.HasValue ? $", {s.YearsExp} năm" : "";
                sb.AppendLine($"- {s.SkillName}: {s.ProficiencyLevel}{yrs}");
            }
        }

        if (input.AssessmentScores.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Điểm Assessment:");
            foreach (var s in input.AssessmentScores)
            {
                sb.AppendLine($"- {s.SkillName}: {s.Score}/{s.MaxScore} ({s.ProficiencyLevel})");
            }
        }

        if (input.Onboarding != null)
        {
            sb.AppendLine();
            sb.AppendLine("## Bối cảnh user:");
            if (input.Onboarding.Major != null) sb.AppendLine($"- Chuyên ngành: {input.Onboarding.Major}");
            if (input.Onboarding.ProficiencyLevel != null) sb.AppendLine($"- Trình độ hiện tại: {input.Onboarding.ProficiencyLevel}");
            if (input.Onboarding.WeeklyStudyHours != null) sb.AppendLine($"- Thời gian học/tuần: {input.Onboarding.WeeklyStudyHours}");
            if (input.Onboarding.PrimaryGoal != null) sb.AppendLine($"- Mục tiêu: {input.Onboarding.PrimaryGoal}");
        }

        return sb.ToString();
    }

    private static GapAnalysisResult ParseResponse(string rawJson, GapAnalysisInput input)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            var summary = root.TryGetProperty("summary", out var sumEl)
                ? sumEl.GetString() ?? ""
                : "";

            var outcomes = new List<GapSkillOutcome>();

            if (root.TryGetProperty("skills", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in arr.EnumerateArray())
                {
                    var name = s.TryGetProperty("skillName", out var n) ? n.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    outcomes.Add(new GapSkillOutcome(
                        SkillName: name,
                        GapStatus: GetString(s, "gapStatus", "missing"),
                        CurrentLevel: GetString(s, "currentLevel", "none"),
                        TargetLevel: GetString(s, "targetLevel", "intermediate"),
                        UrgencyScore: GetInt(s, "urgencyScore", 5),
                        Reasoning: GetString(s, "reasoning", ""),
                        IsMandatoryInJd: GetBool(s, "isMandatoryInJd", true)));
                }
            }

            // Defensive: ensure every JD hard skill is represented even if LLM missed one
            var present = outcomes.Select(o => o.SkillName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var jdSkill in input.JdSkills.Where(s => s.IsHardSkill))
            {
                if (!present.Contains(jdSkill.SkillName))
                {
                    outcomes.Add(new GapSkillOutcome(
                        jdSkill.SkillName,
                        "missing",
                        "none",
                        "intermediate",
                        jdSkill.IsMandatory ? 9 : 6,
                        "LLM bỏ sót, fallback heuristic đánh giá missing.",
                        jdSkill.IsMandatory));
                }
            }

            return new GapAnalysisResult(summary, outcomes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse LLM response as JSON: {ex.Message}\nRaw: {rawJson}", ex);
        }
    }

    private static string GetString(JsonElement el, string key, string fallback)
        => el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? fallback : fallback;

    private static int GetInt(JsonElement el, string key, int fallback)
        => el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : fallback;

    private static bool GetBool(JsonElement el, string key, bool fallback)
        => el.TryGetProperty(key, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False)
            ? v.GetBoolean()
            : fallback;
}
