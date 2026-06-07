using System.Text.Json;
using Edu_Nexus.Application.Interfaces.Parsing;

namespace Edu_Nexus.Infrastructure.Parsing;

public class LlmResponseValidator : ILlmResponseValidator
{
    public ValidationResult ValidateGapAnalysis(JsonDocument doc)
    {
        var errors = new List<string>();
        var root = doc.RootElement;

        if (!root.TryGetProperty("summary", out var summary) || summary.ValueKind != JsonValueKind.String)
            errors.Add("Missing required field: summary (string)");

        if (!root.TryGetProperty("skills", out var skills) || skills.ValueKind != JsonValueKind.Array)
            errors.Add("Missing required field: skills (array)");
        else
        {
            var skillCount = 0;
            foreach (var skill in skills.EnumerateArray())
            {
                skillCount++;
                if (!skill.TryGetProperty("skillName", out var name) || name.ValueKind != JsonValueKind.String)
                    errors.Add($"Skill #{skillCount}: missing skillName (string)");
                if (!skill.TryGetProperty("gapStatus", out var status) || status.ValueKind != JsonValueKind.String)
                    errors.Add($"Skill #{skillCount}: missing gapStatus (string)");
                if (!skill.TryGetProperty("currentLevel", out _) || !skill.TryGetProperty("targetLevel", out _))
                    errors.Add($"Skill #{skillCount}: missing currentLevel or targetLevel");
                if (!skill.TryGetProperty("urgencyScore", out var score) || score.ValueKind != JsonValueKind.Number)
                    errors.Add($"Skill #{skillCount}: missing urgencyScore (number)");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors.ToArray());
    }

    public ValidationResult ValidateJdParse(JsonDocument doc)
    {
        var errors = new List<string>();
        var root = doc.RootElement;

        var requiredFields = new[] { "jobTitle", "jobRoleCategory", "seniorityLevel" };
        foreach (var field in requiredFields)
        {
            if (!root.TryGetProperty(field, out var val) || val.ValueKind == JsonValueKind.Null)
                errors.Add($"Missing required field: {field}");
        }

        if (root.TryGetProperty("hardSkills", out var hardSkills) && hardSkills.ValueKind == JsonValueKind.Array)
        {
            foreach (var skill in hardSkills.EnumerateArray())
            {
                if (!skill.TryGetProperty("skillName", out var name) || name.ValueKind != JsonValueKind.String)
                    errors.Add("Hard skill missing skillName");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors.ToArray());
    }

    public ValidationResult ValidateAssessment(JsonDocument doc)
    {
        var errors = new List<string>();
        var root = doc.RootElement;

        if (!root.TryGetProperty("questions", out var questions) || questions.ValueKind != JsonValueKind.Array)
            errors.Add("Missing required field: questions (array)");
        else
        {
            var questionCount = 0;
            foreach (var q in questions.EnumerateArray())
            {
                questionCount++;
                if (!q.TryGetProperty("questionText", out var text) || text.ValueKind != JsonValueKind.String)
                    errors.Add($"Question #{questionCount}: missing questionText");
                if (!q.TryGetProperty("options", out var opts) || opts.ValueKind != JsonValueKind.Array)
                    errors.Add($"Question #{questionCount}: missing options (array)");
                if (!q.TryGetProperty("correctAnswer", out _))
                    errors.Add($"Question #{questionCount}: missing correctAnswer");
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors.ToArray());
    }

    public ValidationResult ValidateResources(JsonDocument doc)
    {
        var errors = new List<string>();
        var root = doc.RootElement;

        if (!root.TryGetProperty("resources", out var resources) || resources.ValueKind != JsonValueKind.Array)
            errors.Add("Missing required field: resources (array)");
        else
        {
            var resourceCount = 0;
            foreach (var res in resources.EnumerateArray())
            {
                resourceCount++;
                var requiredFields = new[] { "title", "type", "url", "provider" };
                foreach (var field in requiredFields)
                {
                    if (!res.TryGetProperty(field, out var val) || val.ValueKind == JsonValueKind.Null)
                        errors.Add($"Resource #{resourceCount}: missing {field}");
                }
            }
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.Fail(errors.ToArray());
    }
}
