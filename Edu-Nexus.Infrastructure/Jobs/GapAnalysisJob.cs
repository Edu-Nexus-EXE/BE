using System.Text.Json;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentPaths;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using Edu_Nexus.Domain.Enums.GapAnalysisSkills;
using Edu_Nexus.Domain.Enums.JdSkills;
using Edu_Nexus.Domain.Enums.Roadmaps;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

public class GapAnalysisJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGapAnalyzer _analyzer;
    private readonly ILogger<GapAnalysisJob> _logger;

    public GapAnalysisJob(IUnitOfWork unitOfWork, IGapAnalyzer analyzer, ILogger<GapAnalysisJob> logger)
    {
        _unitOfWork = unitOfWork;
        _analyzer = analyzer;
        _logger = logger;
    }

    public async Task RunAsync(Guid gapAnalysisId, CancellationToken cancellationToken)
    {
        var gap = await _unitOfWork.GapAnalyses
            .FirstOrDefaultAsync(g => g.Id == gapAnalysisId, "", cancellationToken);

        if (gap == null)
        {
            _logger.LogWarning("GapAnalysisJob: gap {Id} not found", gapAnalysisId);
            return;
        }

        try
        {
            gap.Status = GapAnalysisStatus.Processing;
            _unitOfWork.GapAnalyses.Update(gap);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var input = await BuildInputAsync(gap, cancellationToken);
            var result = await _analyzer.AnalyzeAsync(input, cancellationToken);

            var skills = await _unitOfWork.Skills.GetAllAsync("", cancellationToken);
            var skillsBySlug = skills
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            foreach (var outcome in result.Skills)
            {
                _unitOfWork.GapAnalysisSkills.Add(new GapAnalysisSkill
                {
                    GapAnalysisId = gap.Id,
                    SkillName = outcome.SkillName,
                    SkillId = skillsBySlug.TryGetValue(outcome.SkillName, out var id) ? id : null,
                    GapStatus = ParseGapStatus(outcome.GapStatus),
                    CurrentLevel = ParseLevel(outcome.CurrentLevel),
                    TargetLevel = ParseLevel(outcome.TargetLevel) ?? SkillLevel.Intermediate,
                    UrgencyScore = (short)outcome.UrgencyScore,
                    Reasoning = outcome.Reasoning,
                    IsMandatoryInJd = outcome.IsMandatoryInJd,
                });
            }

            gap.Summary = JsonSerializer.Serialize(new { text = result.Summary });
            gap.Status = GapAnalysisStatus.Completed;
            gap.CompletedAt = DateTime.UtcNow;
            gap.Error = null;
            _unitOfWork.GapAnalyses.Update(gap);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (gap.Version > 1)
            {
                await MarkRoadmapsOutdatedAsync(gap.JdId, cancellationToken);
            }

            _logger.LogInformation("GapAnalysisJob completed for {Id} version {Version}", gapAnalysisId, gap.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GapAnalysisJob failed for {Id}", gapAnalysisId);
            gap.Status = GapAnalysisStatus.Failed;
            gap.Error = ex.Message;
            _unitOfWork.GapAnalyses.Update(gap);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<GapAnalysisInput> BuildInputAsync(GapAnalysis gap, CancellationToken ct)
    {
        var jd = await _unitOfWork.JdSubmissions.FirstOrDefaultAsync(
            j => j.Id == gap.JdId,
            nameof(JdSubmission.JdSkills),
            ct);

        var jdSkills = jd?.JdSkills
            .Select(s => new JdSkillInput(s.SkillNameRaw, s.IsMandatory, s.SkillType == SkillType.HardSkill))
            .ToList()
            ?? new List<JdSkillInput>();

        var path = await _unitOfWork.AssessmentPaths.FirstOrDefaultAsync(
            p => p.Id == gap.AssessmentPathId,
            "", ct);

        var cvSkills = new List<CvSkillInput>();
        var assessmentScores = new List<AssessmentSkillScore>();

        if (path?.PathType == PathType.Cv)
        {
            var cv = await _unitOfWork.CvSubmissions.FirstOrDefaultAsync(
                c => c.AssessmentPathId == gap.AssessmentPathId, "", ct);
            cvSkills = DeserializeCvSkills(cv?.ParsedSkills);
        }
        else if (path?.PathType == PathType.Assessment)
        {
            var session = await _unitOfWork.AssessmentSessions.FirstOrDefaultAsync(
                s => s.AssessmentPathId == gap.AssessmentPathId && s.IsCurrent && s.Status == AssessmentSessionStatus.Submitted,
                "", ct);
            assessmentScores = DeserializeAssessmentScores(session?.SkillScores);
        }

        var onboarding = await _unitOfWork.OnboardingResponses.FirstOrDefaultAsync(
            o => o.UserId == gap.UserId, "", ct);

        var onboardingSnapshot = onboarding == null ? null : new OnboardingSnapshot(
            onboarding.Major,
            onboarding.ProficiencyLevel,
            onboarding.WeeklyStudyHours,
            onboarding.PrimaryGoal);

        return new GapAnalysisInput(
            JobTitle: jd?.JobTitle ?? "Software Developer",
            JobRoleCategory: jd?.JobRoleCategory ?? "general_software",
            SeniorityLevel: jd?.SeniorityLevel,
            JdSkills: jdSkills,
            CvSkills: cvSkills,
            AssessmentScores: assessmentScores,
            Onboarding: onboardingSnapshot);
    }

    private static List<CvSkillInput> DeserializeCvSkills(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("skills", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return new();

            var result = new List<CvSkillInput>();
            foreach (var s in arr.EnumerateArray())
            {
                result.Add(new CvSkillInput(
                    s.TryGetProperty("skillName", out var n) ? n.GetString() ?? "" : "",
                    s.TryGetProperty("proficiencyLevel", out var p) ? p.GetString() ?? "basic" : "basic",
                    s.TryGetProperty("yearsExp", out var y) && y.ValueKind == JsonValueKind.Number ? y.GetDecimal() : null));
            }
            return result;
        }
        catch
        {
            return new();
        }
    }

    private static List<AssessmentSkillScore> DeserializeAssessmentScores(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("skills", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return new();

            var result = new List<AssessmentSkillScore>();
            foreach (var s in arr.EnumerateArray())
            {
                result.Add(new AssessmentSkillScore(
                    s.GetProperty("skillName").GetString() ?? "",
                    s.GetProperty("score").GetInt32(),
                    s.GetProperty("maxScore").GetInt32(),
                    s.GetProperty("proficiencyLevel").GetString() ?? "none"));
            }
            return result;
        }
        catch
        {
            return new();
        }
    }

    private async Task MarkRoadmapsOutdatedAsync(Guid jdId, CancellationToken ct)
    {
        var actives = await _unitOfWork.Roadmaps.FindAsync(
            r => r.JdId == jdId && r.Status == RoadmapStatus.Active,
            "", ct);

        foreach (var roadmap in actives)
        {
            roadmap.IsOutdated = true;
            _unitOfWork.Roadmaps.Update(roadmap);
        }

        if (actives.Any())
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    private static GapStatus ParseGapStatus(string raw) => raw switch
    {
        "missing" => GapStatus.Missing,
        "needs_upgrade" => GapStatus.NeedsUpgrade,
        _ => GapStatus.Have,
    };

    private static SkillLevel? ParseLevel(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        return raw.ToLowerInvariant() switch
        {
            "basic" => SkillLevel.Basic,
            "intermediate" => SkillLevel.Intermediate,
            "advanced" => SkillLevel.Advanced,
            _ => SkillLevel.None,
        };
    }
}
