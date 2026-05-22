using Edu_Nexus.Application.Interfaces.Parsing;

namespace Edu_Nexus.Infrastructure.Parsing;

/// Heuristic Gap Analyzer:
/// - For each JD skill: check CV (Path A) or Assessment (Path B) for evidence.
///   - Not found              -> Missing  (target=intermediate, urgency 8/9 if mandatory)
///   - Found beginner level    -> NeedsUpgrade (urgency 5-6)
///   - Found intermediate/advanced -> Have (urgency 1-3)
/// Replace with LLM-backed analyzer by swapping the DI registration of IGapAnalyzer.
public class FakeGapAnalyzer : IGapAnalyzer
{
    private static readonly string[] LevelOrder = { "none", "beginner", "intermediate", "advanced" };

    public Task<GapAnalysisResult> AnalyzeAsync(GapAnalysisInput input, CancellationToken cancellationToken = default)
    {
        var seniority = NormalizeSeniority(input.SeniorityLevel);
        var targetLevel = TargetForSeniority(seniority);

        var hardSkills = input.JdSkills.Where(s => s.IsHardSkill).ToList();

        var outcomes = new List<GapSkillOutcome>(hardSkills.Count);

        foreach (var jd in hardSkills)
        {
            var (currentLevel, evidence) = LookupCurrentLevel(jd.SkillName, input.CvSkills, input.AssessmentScores);

            var gapStatus = ClassifyGap(currentLevel, targetLevel);
            var urgency = ComputeUrgency(gapStatus, jd.IsMandatory);
            var reasoning = BuildReasoning(jd.SkillName, jd.IsMandatory, currentLevel, targetLevel, gapStatus, evidence);

            outcomes.Add(new GapSkillOutcome(
                SkillName: jd.SkillName,
                GapStatus: gapStatus,
                CurrentLevel: currentLevel,
                TargetLevel: targetLevel,
                UrgencyScore: urgency,
                Reasoning: reasoning,
                IsMandatoryInJd: jd.IsMandatory));
        }

        var missing = outcomes.Count(o => o.GapStatus == "missing");
        var upgrade = outcomes.Count(o => o.GapStatus == "needs_upgrade");
        var have = outcomes.Count(o => o.GapStatus == "have");

        var summary = BuildSummary(input.JobTitle, missing, upgrade, have, input.Onboarding);

        return Task.FromResult(new GapAnalysisResult(summary, outcomes));
    }

    private static string TargetForSeniority(string seniority) => seniority switch
    {
        "intern" or "fresher" => "beginner",
        "junior" => "intermediate",
        "middle" or "senior" or "lead" => "advanced",
        _ => "intermediate"
    };

    private static string NormalizeSeniority(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? "junior" : raw.Trim().ToLowerInvariant();

    private static (string CurrentLevel, string? Evidence) LookupCurrentLevel(
        string skillName,
        IReadOnlyList<CvSkillInput> cv,
        IReadOnlyList<AssessmentSkillScore> assessment)
    {
        var cvMatch = cv.FirstOrDefault(s => string.Equals(s.SkillName, skillName, StringComparison.OrdinalIgnoreCase));
        if (cvMatch != null)
        {
            return (NormalizeLevel(cvMatch.ProficiencyLevel),
                $"CV: {cvMatch.ProficiencyLevel}{(cvMatch.YearsExp.HasValue ? $" ({cvMatch.YearsExp} years)" : "")}");
        }

        var aMatch = assessment.FirstOrDefault(s => string.Equals(s.SkillName, skillName, StringComparison.OrdinalIgnoreCase));
        if (aMatch != null)
        {
            return (NormalizeLevel(aMatch.ProficiencyLevel),
                $"Assessment: {aMatch.Score}/{aMatch.MaxScore} ({aMatch.ProficiencyLevel})");
        }

        return ("none", null);
    }

    private static string NormalizeLevel(string raw)
    {
        var lower = (raw ?? "").Trim().ToLowerInvariant();
        return lower switch
        {
            "advanced" or "expert" => "advanced",
            "intermediate" => "intermediate",
            "beginner" or "basic" => "beginner",
            _ => "none"
        };
    }

    private static int RankLevel(string level)
        => Array.IndexOf(LevelOrder, level) is var i && i < 0 ? 0 : i;

    private static string ClassifyGap(string current, string target)
    {
        var c = RankLevel(current);
        var t = RankLevel(target);
        if (c == 0) return "missing";
        if (c < t) return "needs_upgrade";
        return "have";
    }

    private static int ComputeUrgency(string gapStatus, bool mandatory)
    {
        return gapStatus switch
        {
            "missing" => mandatory ? 9 : 6,
            "needs_upgrade" => mandatory ? 6 : 4,
            _ => mandatory ? 2 : 1
        };
    }

    private static string BuildReasoning(string skill, bool mandatory, string current, string target, string gapStatus, string? evidence)
    {
        var mandatoryHint = mandatory ? "bắt buộc trong JD" : "tuỳ chọn trong JD";
        var evidenceHint = evidence == null ? "không tìm thấy bằng chứng trong CV/Assessment" : evidence;

        return gapStatus switch
        {
            "missing" => $"JD yêu cầu {target} {skill} ({mandatoryHint}), user hiện chưa có ({evidenceHint}).",
            "needs_upgrade" => $"User đang ở mức {current} {skill} ({evidenceHint}), cần nâng lên {target} để khớp JD ({mandatoryHint}).",
            _ => $"User đã đạt {current} ≥ {target} {skill} ({evidenceHint})."
        };
    }

    private static string BuildSummary(string jobTitle, int missing, int upgrade, int have, OnboardingSnapshot? onboarding)
    {
        var lines = new List<string>
        {
            $"Phân tích Gap cho vị trí \"{jobTitle}\":",
            $"- {missing} kỹ năng còn thiếu, {upgrade} kỹ năng cần nâng cấp, {have} kỹ năng đã đạt."
        };

        if (onboarding?.WeeklyStudyHours != null)
        {
            lines.Add($"- Quỹ thời gian học khai báo: {onboarding.WeeklyStudyHours}, mức hiện tại: {onboarding.ProficiencyLevel ?? "chưa rõ"}.");
        }

        return string.Join("\n", lines);
    }
}
