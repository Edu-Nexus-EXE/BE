using System.Text.Json;
using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using MediatR;

namespace Edu_Nexus.Application.Features.AssessmentSessions.Queries;

public record GetSessionQuestionsQuery(Guid SessionId) : IRequest<SessionQuestionsDto>;

public class GetSessionQuestionsQueryHandler : IRequestHandler<GetSessionQuestionsQuery, SessionQuestionsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetSessionQuestionsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SessionQuestionsDto> Handle(GetSessionQuestionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var session = await _unitOfWork.AssessmentSessions.FirstOrDefaultAsync(
            s => s.Id == request.SessionId && s.UserId == userId,
            nameof(AssessmentSession.AssessmentQuestions),
            cancellationToken)
            ?? throw new Exception("404 SESSION_NOT_FOUND");

        var questions = session.AssessmentQuestions
            .OrderBy(q => q.SequenceOrder)
            .Select(q => new AssessmentQuestionDto(
                q.Id,
                q.SequenceOrder,
                (int)q.Part,
                q.QuestionText,
                DeserializeOptions(q.Options)))
            .ToList();

        var statusString = session.Status switch
        {
            AssessmentSessionStatus.InProgress => "in_progress",
            AssessmentSessionStatus.Submitted => "submitted",
            _ => "expired"
        };

        return new SessionQuestionsDto(
            session.Id,
            statusString,
            session.Part1Count,
            session.Part2Count,
            questions);
    }

    private static QuestionOptionsDto DeserializeOptions(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new QuestionOptionsDto(
                root.TryGetProperty("A", out var a) ? a.GetString() ?? "" : "",
                root.TryGetProperty("B", out var b) ? b.GetString() ?? "" : "",
                root.TryGetProperty("C", out var c) ? c.GetString() ?? "" : "",
                root.TryGetProperty("D", out var d) ? d.GetString() ?? "" : "");
        }
        catch
        {
            return new QuestionOptionsDto("", "", "", "");
        }
    }
}
