using System.Text.Json;
using System.Text.Json.Serialization;
using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Parsing;

public class OpenAiJdParser : IJdParser
{
    private readonly ILlmService _llm;
    private readonly ILlmResponseValidator _validator;
    private readonly int _maxTokens;
    private readonly ILogger<OpenAiJdParser> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public OpenAiJdParser(ILlmService llm, ILlmResponseValidator validator,
        IConfiguration config, ILogger<OpenAiJdParser> logger)
    {
        _llm = llm;
        _validator = validator;
        _maxTokens = config.GetValue<int?>("OpenAI:MaxTokens:JdParse") ?? 800;
        _logger = logger;
    }

    public async Task<ParsedJdResult> ParseAsync(string rawContent, CancellationToken cancellationToken = default)
    {
        const string system = "Bạn là chuyên gia phân tích Job Description. Trích xuất dữ liệu có cấu trúc từ JD. Trả về JSON đúng schema, không thêm field nào khác.";
        var user = "JD Content:\n\"\"\"\n" + rawContent + "\n\"\"\"\n\n" +
            "Trả về JSON: { \"job_title\": \"string\", \"job_role_category\": \"backend_java|frontend_react|devops|digital_marketing|...\", " +
            "\"seniority_level\": \"fresher|junior|mid|senior\", \"salary_min\": number|null, \"salary_max\": number|null, " +
            "\"currency\": \"VND|USD|null\", \"hard_skills\": [ { \"skill_name\": \"string\", \"is_mandatory\": true } ], " +
            "\"soft_skills\": [ { \"skill_name\": \"string\", \"is_mandatory\": false } ] }";

        var resp = await _llm.ChatJsonAsync(system, user, "fast", _maxTokens, ct: cancellationToken);
        await _llm.LogQueryAsync("jd_parse", resp, "gpt-4o-mini");
        if (!resp.Success)
            throw new InvalidOperationException("JD parse: LLM call failed after retry");

        var parsed = ParseAndValidate(resp.Content);
        if (parsed == null)
        {
            var resp2 = await _llm.ChatJsonAsync(system, user, "fast", _maxTokens, ct: cancellationToken);
            await _llm.LogQueryAsync("jd_parse", resp2, "gpt-4o-mini");
            parsed = ParseAndValidate(resp2.Content)
                ?? throw new InvalidOperationException("JD parse: validation failed after retry");
        }
        return parsed;
    }

    private ParsedJdResult? ParseAndValidate(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var validation = _validator.ValidateJdParse(doc);
            if (!validation.IsValid)
            {
                _logger.LogWarning("JD parse validation failed: {Errors}", string.Join("; ", validation.Errors));
                return null;
            }
            var dto = JsonSerializer.Deserialize<JdParsedDto>(content, JsonOpts)!;
            return new ParsedJdResult(
                JobTitle: dto.JobTitle ?? "Software Developer",
                JobRoleCategory: dto.JobRoleCategory ?? "general_software",
                SeniorityLevel: dto.SeniorityLevel ?? "junior",
                SalaryMin: dto.SalaryMin,
                SalaryMax: dto.SalaryMax,
                Currency: dto.Currency,
                HardSkills: (dto.HardSkills ?? new()).Select(s => new ParsedJdSkill(s.SkillName, s.IsMandatory)).ToList(),
                SoftSkills: (dto.SoftSkills ?? new()).Select(s => new ParsedJdSkill(s.SkillName, s.IsMandatory)).ToList());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JD parse JSON error");
            return null;
        }
    }

    private record JdParsedDto(
        [property: JsonPropertyName("job_title")] string? JobTitle,
        [property: JsonPropertyName("job_role_category")] string? JobRoleCategory,
        [property: JsonPropertyName("seniority_level")] string? SeniorityLevel,
        [property: JsonPropertyName("salary_min")] int? SalaryMin,
        [property: JsonPropertyName("salary_max")] int? SalaryMax,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("hard_skills")] List<JdSkillDto>? HardSkills,
        [property: JsonPropertyName("soft_skills")] List<JdSkillDto>? SoftSkills);

    private record JdSkillDto(
        [property: JsonPropertyName("skill_name")] string SkillName,
        [property: JsonPropertyName("is_mandatory")] bool IsMandatory);
}
