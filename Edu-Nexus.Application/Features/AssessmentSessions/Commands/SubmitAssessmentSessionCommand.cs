using System.Text.Json;
using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentQuestions;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using MediatR;

namespace Edu_Nexus.Application.Features.AssessmentSessions.Commands;

public record SubmitAssessmentSessionCommand(Guid SessionId, SubmitAssessmentSessionRequest Request) : IRequest<AssessmentSessionResultDto>;

public class SubmitAssessmentSessionCommandHandler : IRequestHandler<SubmitAssessmentSessionCommand, AssessmentSessionResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGapAnalysisQueue _gapAnalysisQueue;

    public SubmitAssessmentSessionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IGapAnalysisQueue gapAnalysisQueue)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _gapAnalysisQueue = gapAnalysisQueue;
    }

    public async Task<AssessmentSessionResultDto> Handle(SubmitAssessmentSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var session = await _unitOfWork.AssessmentSessions.FirstOrDefaultAsync(
            s => s.Id == request.SessionId && s.UserId == userId,
            includeProperties: $"{nameof(AssessmentSession.AssessmentQuestions)},{nameof(AssessmentSession.AssessmentPath)}",
            cancellationToken: cancellationToken)
            ?? throw new Exception("404 SESSION_NOT_FOUND");

        if (session.Status != AssessmentSessionStatus.InProgress)
        {
            throw new Exception("409 ALREADY_SUBMITTED");
        }

        var questions = session.AssessmentQuestions.ToList();
        if (questions.Count == 0)
        {
            throw new Exception("422 QUESTIONS_NOT_READY");
        }

        var answerMap = request.Request.Answers
            .ToDictionary(a => a.QuestionId, a => a.SelectedOption);

        if (answerMap.Count != questions.Count)
        {
            throw new Exception("422 ANSWER_COUNT_MISMATCH");
        }

        var results = new List<AnswerResultDto>();
        var correctCount = 0;
        var perSkill = new Dictionary<string, (int Score, int Max)>();

        foreach (var q in questions.OrderBy(q => q.SequenceOrder))
        {
            if (!answerMap.TryGetValue(q.Id, out var selectedRaw))
            {
                throw new Exception("422 MISSING_ANSWER");
            }

            if (!Enum.TryParse<AssessmentOption>(selectedRaw, ignoreCase: true, out var selected))
            {
                throw new Exception("422 INVALID_OPTION");
            }

            var isCorrect = selected == q.CorrectOption;
            if (isCorrect) correctCount++;

            _unitOfWork.AssessmentAnswers.Add(new AssessmentAnswer
            {
                SessionId = session.Id,
                QuestionId = q.Id,
                SelectedOption = selected,
                IsCorrect = isCorrect,
            });

            var skill = q.RelatedSkill ?? "general";
            var (s, m) = perSkill.TryGetValue(skill, out var prev) ? prev : (0, 0);
            perSkill[skill] = (isCorrect ? s + 1 : s, m + 1);

            results.Add(new AnswerResultDto(
                q.Id,
                selected.ToString(),
                q.CorrectOption.ToString(),
                isCorrect,
                q.Explanation));
        }

        var skillScores = perSkill
            .Select(kvp => new SkillScoreDto(
                kvp.Key,
                kvp.Value.Score,
                kvp.Value.Max,
                ToProficiency(kvp.Value.Score, kvp.Value.Max)))
            .ToList();

        var totalQuestions = questions.Count;
        var scorePercent = totalQuestions == 0
            ? 0m
            : Math.Round(correctCount * 100m / totalQuestions, 1);

        session.Status = AssessmentSessionStatus.Submitted;
        session.SubmittedAt = DateTime.UtcNow;
        session.SkillScores = JsonSerializer.Serialize(new
        {
            totalQuestions,
            correctCount,
            scorePercent,
            skills = skillScores
        });
        _unitOfWork.AssessmentSessions.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var autoTriggered = await BuildAutoTriggeredAsync(session, cancellationToken);

        return new AssessmentSessionResultDto(
            session.Id,
            "submitted",
            totalQuestions,
            correctCount,
            scorePercent,
            skillScores,
            results,
            session.SubmittedAt,
            autoTriggered);
    }

    private async Task<AutoTriggeredDto?> BuildAutoTriggeredAsync(AssessmentSession session, CancellationToken ct)
    {
        if (session.AssessmentPath == null) return null;

        var jdId = session.AssessmentPath.JdId;
        var userId = session.UserId;

        var priorCompletedGap = (await _unitOfWork.GapAnalyses.FindAsync(
            g => g.JdId == jdId && g.UserId == userId && g.Status == GapAnalysisStatus.Completed,
            "", ct))
            .OrderByDescending(g => g.Version)
            .FirstOrDefault();

        if (priorCompletedGap == null) return null;

        foreach (var latest in await _unitOfWork.GapAnalyses.FindAsync(
            g => g.JdId == jdId && g.UserId == userId && g.IsLatest, "", ct))
        {
            latest.IsLatest = false;
            _unitOfWork.GapAnalyses.Update(latest);
        }

        var newGap = new GapAnalysis
        {
            UserId = userId,
            JdId = jdId,
            AssessmentPathId = session.AssessmentPathId,
            InputSource = priorCompletedGap.InputSource,
            Version = (short)(priorCompletedGap.Version + 1),
            IsLatest = true,
            Status = GapAnalysisStatus.Pending,
        };
        _unitOfWork.GapAnalyses.Add(newGap);
        await _unitOfWork.SaveChangesAsync(ct);

        _gapAnalysisQueue.Enqueue(newGap.Id);

        return new AutoTriggeredDto(newGap.Id, "pending");
    }

    private static string ToProficiency(int score, int max)
    {
        if (max == 0) return "none";
        var percent = (decimal)score / max;
        return percent switch
        {
            >= 0.75m => "advanced",
            >= 0.50m => "intermediate",
            >= 0.25m => "beginner",
            _ => "none"
        };
    }
}
