using System.Text.Json;
using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using MediatR;

namespace Edu_Nexus.Application.Features.AssessmentSessions.Queries;

public record GetSessionResultQuery(Guid SessionId) : IRequest<AssessmentSessionResultDto>;

public class GetSessionResultQueryHandler : IRequestHandler<GetSessionResultQuery, AssessmentSessionResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetSessionResultQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<AssessmentSessionResultDto> Handle(GetSessionResultQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var session = await _unitOfWork.AssessmentSessions.FirstOrDefaultAsync(
            s => s.Id == request.SessionId && s.UserId == userId,
            includeProperties: $"{nameof(AssessmentSession.AssessmentQuestions)},{nameof(AssessmentSession.AssessmentAnswers)},{nameof(AssessmentSession.AssessmentPath)}",
            cancellationToken: cancellationToken)
            ?? throw new Exception("404 SESSION_NOT_FOUND");

        var answersByQuestion = session.AssessmentAnswers.ToDictionary(a => a.QuestionId);

        var results = session.AssessmentQuestions
            .OrderBy(q => q.SequenceOrder)
            .Select(q =>
            {
                answersByQuestion.TryGetValue(q.Id, out var ans);
                return new AnswerResultDto(
                    q.Id,
                    ans?.SelectedOption.ToString() ?? "",
                    q.CorrectOption.ToString(),
                    ans?.IsCorrect ?? false,
                    q.Explanation);
            })
            .ToList();

        var (totalQuestions, correctCount, scorePercent, skillScores) = DeserializeSkillScores(session.SkillScores, results);

        var statusString = session.Status switch
        {
            AssessmentSessionStatus.InProgress => "in_progress",
            AssessmentSessionStatus.Submitted => "submitted",
            _ => "expired"
        };

        AutoTriggeredDto? autoTriggered = null;
        if (session.AssessmentPath != null)
        {
            var latestGap = await _unitOfWork.GapAnalyses.FirstOrDefaultAsync(
                g => g.JdId == session.AssessmentPath.JdId && g.UserId == userId && g.IsLatest,
                "", cancellationToken);
            
            if (latestGap != null && latestGap.Version > 1)
            {
                autoTriggered = new AutoTriggeredDto(latestGap.Id, latestGap.Status.ToString().ToLower());
            }
        }

        return new AssessmentSessionResultDto(
            session.Id,
            statusString,
            totalQuestions,
            correctCount,
            scorePercent,
            skillScores,
            results,
            session.SubmittedAt,
            autoTriggered);
    }

    private static (int Total, int Correct, decimal ScorePercent, IReadOnlyList<SkillScoreDto> Skills) DeserializeSkillScores(
        string? json,
        IReadOnlyList<AnswerResultDto> results)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            var total = results.Count;
            var correct = results.Count(r => r.IsCorrect);
            var pct = total == 0 ? 0m : Math.Round(correct * 100m / total, 1);
            return (total, correct, pct, Array.Empty<SkillScoreDto>());
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var total = root.TryGetProperty("totalQuestions", out var t) ? t.GetInt32() : results.Count;
            var correct = root.TryGetProperty("correctCount", out var c) ? c.GetInt32() : results.Count(r => r.IsCorrect);
            var pct = root.TryGetProperty("scorePercent", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetDecimal() : 0m;

            var skills = new List<SkillScoreDto>();
            if (root.TryGetProperty("skills", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in arr.EnumerateArray())
                {
                    skills.Add(new SkillScoreDto(
                        s.GetProperty("skillName").GetString() ?? "",
                        s.GetProperty("score").GetInt32(),
                        s.GetProperty("maxScore").GetInt32(),
                        s.GetProperty("proficiencyLevel").GetString() ?? "none"));
                }
            }
            return (total, correct, pct, skills);
        }
        catch
        {
            return (results.Count, results.Count(r => r.IsCorrect), 0m, Array.Empty<SkillScoreDto>());
        }
    }
}
