using System.Text.Json;
using System.Text.Json.Serialization;
using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

public class OpenAiCvParser : ICvParser
{
    private readonly ILlmService _llm;
    private readonly int _maxTokens;
    private readonly ILogger<OpenAiCvParser> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public OpenAiCvParser(ILlmService llm, IConfiguration config, ILogger<OpenAiCvParser> logger)
    {
        _llm = llm;
        _maxTokens = config.GetValue<int?>("OpenAI:MaxTokens:CvParse") ?? 1000;
        _logger = logger;
    }

    public async Task<ParsedCvResult> ParseAsync(string anonymizedText, CancellationToken cancellationToken = default)
    {
        // A.3 — CV scan/ảnh: text rỗng/quá ngắn → fail ngay, KHÔNG gọi LLM
        if (string.IsNullOrWhiteSpace(anonymizedText) || anonymizedText.Trim().Length < 50)
            throw new InvalidOperationException(
                "Không thể đọc nội dung CV. File có thể là scan/ảnh — chỉ hỗ trợ PDF có text.");

        const string system = "Bạn là chuyên gia phân tích CV. Trích xuất kỹ năng và kinh nghiệm từ CV text.";
        var user = "CV Text (đã extract từ PDF):\n\"\"\"\n" + anonymizedText + "\n\"\"\"\n\n" +
            "Trả về JSON: { \"skills\": [ { \"skill_name\": \"string\", \"proficiency_level\": \"basic|intermediate|advanced\", " +
            "\"years_exp\": number|null, \"evidence\": \"string\" } ], \"total_experience_years\": number|null }";

        var resp = await _llm.ChatJsonAsync(system, user, "fast", _maxTokens, ct: cancellationToken);
        await _llm.LogQueryAsync("cv_parse", resp, "gpt-4o-mini");
        if (!resp.Success)
            throw new InvalidOperationException("CV parse: LLM call failed after retry");

        var dto = JsonSerializer.Deserialize<CvParsedDto>(resp.Content, JsonOpts)
            ?? throw new InvalidOperationException("CV parse: empty response");

        return new ParsedCvResult(
            TotalExperienceYears: dto.TotalExperienceYears,
            Skills: (dto.Skills ?? new()).Select(s => new ParsedCvSkill(
                s.SkillName, s.ProficiencyLevel ?? "basic", s.YearsExp, s.Evidence)).ToList());
    }

    private record CvParsedDto(
        [property: JsonPropertyName("total_experience_years")] decimal? TotalExperienceYears,
        [property: JsonPropertyName("skills")] List<CvSkillDto>? Skills);

    private record CvSkillDto(
        [property: JsonPropertyName("skill_name")] string SkillName,
        [property: JsonPropertyName("proficiency_level")] string? ProficiencyLevel,
        [property: JsonPropertyName("years_exp")] decimal? YearsExp,
        [property: JsonPropertyName("evidence")] string? Evidence);
}
