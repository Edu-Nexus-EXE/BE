using System.Text.Json;
using System.Text.Json.Serialization;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

public class OpenAiAssessmentQuestionGenerator : IAssessmentQuestionGenerator
{
    private readonly ILlmService _llm;
    private readonly IRagService _rag;
    private readonly int _maxTokens;
    private readonly string[] _sourceTypes;
    private readonly int _topK;
    private readonly ILogger<OpenAiAssessmentQuestionGenerator> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public OpenAiAssessmentQuestionGenerator(ILlmService llm, IRagService rag,
        IConfiguration config, ILogger<OpenAiAssessmentQuestionGenerator> logger)
    {
        _llm = llm;
        _rag = rag;
        _maxTokens = config.GetValue<int?>("OpenAI:MaxTokens:Assessment") ?? 3000;
        _sourceTypes = config.GetSection("Rag:AllowedSourceTypes").Get<string[]>()
            ?? new[] { "fptu_curriculum", "fptu_syllabus", "external_doc" };
        _topK = config.GetValue<int?>("Rag:TopK") ?? 5;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        AssessmentGenerationInput input, CancellationToken cancellationToken = default)
    {
        var ragQuery = $"{input.JobRoleCategory} {string.Join(" ", input.HardSkills)}";
        var chunks = await _rag.SearchAsync(ragQuery, _topK, _sourceTypes, 0.65, cancellationToken);
        var ragContext = chunks.Count == 0 ? "(không có tài liệu)" : string.Join("\n---\n", chunks.Select(c => c.Content));

        const string system = "Bạn là giảng viên đại học IT/Marketing tại FPTU. Generate bài test đánh giá kỹ năng. Phần 1 (tổng quát): 10-12 câu cover lĩnh vực chung. Phần 2 (must-have): 5-8 câu hard skill cụ thể trong JD. Mỗi câu: 4 lựa chọn A/B/C/D, 1 đáp án đúng, explanation ngắn, related_skill rõ ràng.";
        var user = "JD role: " + input.JobRoleCategory + ", seniority: " + (input.SeniorityLevel ?? "junior") + "\n" +
            "Hard skills: " + string.Join(", ", input.HardSkills) + "\n" +
            "Số câu Phần 1: " + input.Part1Target + ", Phần 2: " + input.Part2Target + "\n" +
            "Tài liệu FPTU: \"\"\"" + ragContext + "\"\"\"\n\n" +
            "Trả về JSON: { \"questions\": [ { \"sequence_order\": 1, \"part\": 1, \"question_text\": \"string\", " +
            "\"options\": { \"A\": \"...\", \"B\": \"...\", \"C\": \"...\", \"D\": \"...\" }, " +
            "\"correct_option\": \"A|B|C|D\", \"related_skill\": \"string\", \"explanation\": \"string\" } ] }";

        var resp = await _llm.ChatJsonAsync(system, user, "fast", _maxTokens, ct: cancellationToken);
        await _llm.LogQueryAsync("assessment_gen", resp, "gpt-4o-mini");
        if (!resp.Success)
            throw new InvalidOperationException("Assessment gen: LLM call failed after retry");

        var dto = JsonSerializer.Deserialize<QuestionsDto>(resp.Content, JsonOpts)
            ?? throw new InvalidOperationException("Assessment gen: empty response");
        if (dto.Questions == null || dto.Questions.Count == 0)
            throw new InvalidOperationException("Assessment gen: no questions returned");
        if (dto.Questions.Count < 10)
            _logger.LogWarning("Assessment gen returned only {Count} questions (expected >=10)", dto.Questions.Count);

        return dto.Questions.Select(q => new GeneratedQuestion(
            Part: q.Part == 2 ? 2 : 1,
            RelatedSkill: q.RelatedSkill ?? "general",
            QuestionText: q.QuestionText ?? "",
            OptionA: q.Options.GetValueOrDefault("A", ""),
            OptionB: q.Options.GetValueOrDefault("B", ""),
            OptionC: q.Options.GetValueOrDefault("C", ""),
            OptionD: q.Options.GetValueOrDefault("D", ""),
            CorrectOption: (q.CorrectOption ?? "A").Trim().ToUpperInvariant(),
            Explanation: q.Explanation ?? "")).ToList();
    }

    private record QuestionsDto([property: JsonPropertyName("questions")] List<QuestionDto>? Questions);

    private record QuestionDto(
        [property: JsonPropertyName("part")] int Part,
        [property: JsonPropertyName("question_text")] string? QuestionText,
        [property: JsonPropertyName("options")] Dictionary<string, string> Options,
        [property: JsonPropertyName("correct_option")] string? CorrectOption,
        [property: JsonPropertyName("related_skill")] string? RelatedSkill,
        [property: JsonPropertyName("explanation")] string? Explanation);
}
