using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentPaths;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using MediatR;

namespace Edu_Nexus.Application.Features.AssessmentSessions.Commands;

public record StartAssessmentSessionCommand(Guid PathId, StartAssessmentSessionRequest Request) : IRequest<SessionAcceptedDto>;

public class StartAssessmentSessionCommandHandler : IRequestHandler<StartAssessmentSessionCommand, SessionAcceptedDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAssessmentGenerateQueue _generateQueue;

    public StartAssessmentSessionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAssessmentGenerateQueue generateQueue)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _generateQueue = generateQueue;
    }

    public async Task<SessionAcceptedDto> Handle(StartAssessmentSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var path = await _unitOfWork.AssessmentPaths
            .FirstOrDefaultAsync(p => p.Id == request.PathId && p.UserId == userId, nameof(AssessmentPath.Jd), cancellationToken)
            ?? throw new Exception("404 PATH_NOT_FOUND");

        if (path.PathType != PathType.Assessment)
        {
            throw new Exception("422 PATH_TYPE_MISMATCH");
        }

        var jobRoleSnapshot = path.Jd?.JobRoleCategory ?? "general_software";

        var oldCurrent = await _unitOfWork.AssessmentSessions
            .FirstOrDefaultAsync(s => s.AssessmentPathId == request.PathId && s.IsCurrent, "", cancellationToken);

        if (oldCurrent != null)
        {
            oldCurrent.IsCurrent = false;
            _unitOfWork.AssessmentSessions.Update(oldCurrent);
        }

        var newSession = new AssessmentSession
        {
            AssessmentPathId = request.PathId,
            UserId = userId,
            JobRoleCategorySnapshot = jobRoleSnapshot,
            Status = AssessmentSessionStatus.InProgress,
            IsCurrent = true,
            Part1Count = 0,
            Part2Count = 0,
        };

        if (request.Request.ReuseSessionId.HasValue)
        {
            var reuseId = request.Request.ReuseSessionId.Value;
            var sourceSession = await _unitOfWork.AssessmentSessions
                .FirstOrDefaultAsync(
                    s => s.Id == reuseId
                        && s.UserId == userId
                        && s.Status == AssessmentSessionStatus.Submitted
                        && s.JobRoleCategorySnapshot == jobRoleSnapshot,
                    nameof(AssessmentSession.AssessmentQuestions),
                    cancellationToken)
                ?? throw new Exception("404 REUSE_SESSION_NOT_FOUND");

            newSession.ReusedFromSessionId = reuseId;
            newSession.Part1Count = sourceSession.Part1Count;
            newSession.Part2Count = sourceSession.Part2Count;

            _unitOfWork.AssessmentSessions.Add(newSession);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var q in sourceSession.AssessmentQuestions.OrderBy(q => q.SequenceOrder))
            {
                _unitOfWork.AssessmentQuestions.Add(new AssessmentQuestion
                {
                    SessionId = newSession.Id,
                    SequenceOrder = q.SequenceOrder,
                    Part = q.Part,
                    QuestionText = q.QuestionText,
                    Options = q.Options,
                    CorrectOption = q.CorrectOption,
                    RelatedSkill = q.RelatedSkill,
                    Explanation = q.Explanation,
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _unitOfWork.AssessmentSessions.Add(newSession);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _generateQueue.Enqueue(newSession.Id);
        }

        return new SessionAcceptedDto(
            newSession.Id,
            newSession.Status.ToString().ToLowerInvariant() == "inprogress" ? "in_progress" : newSession.Status.ToString().ToLowerInvariant(),
            newSession.ReusedFromSessionId,
            newSession.StartedAt == default ? DateTime.UtcNow : newSession.StartedAt);
    }
}
