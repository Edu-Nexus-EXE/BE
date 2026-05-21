using System.Text.Json;
using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using MediatR;

namespace Edu_Nexus.Application.Features.AssessmentSessions.Queries;

public record GetReusableSessionsQuery(Guid JdId) : IRequest<IReadOnlyList<ReusableSessionDto>>;

public class GetReusableSessionsQueryHandler : IRequestHandler<GetReusableSessionsQuery, IReadOnlyList<ReusableSessionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetReusableSessionsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ReusableSessionDto>> Handle(GetReusableSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var jd = await _unitOfWork.JdSubmissions.FirstOrDefaultAsync(
            j => j.Id == request.JdId && j.UserId == userId && j.DeletedAt == null,
            "", cancellationToken)
            ?? throw new Exception("404 JD_NOT_FOUND");

        var category = jd.JobRoleCategory ?? "";
        if (string.IsNullOrEmpty(category)) return Array.Empty<ReusableSessionDto>();

        var sessions = (await _unitOfWork.AssessmentSessions.FindAsync(
            s => s.UserId == userId
                && s.Status == AssessmentSessionStatus.Submitted
                && s.IsCurrent
                && s.JobRoleCategorySnapshot == category,
            includeProperties: $"{nameof(AssessmentSession.AssessmentPath)}.{nameof(AssessmentPath.Jd)}",
            cancellationToken: cancellationToken))
            .OrderByDescending(s => s.SubmittedAt)
            .ToList();

        return sessions.Select(s => new ReusableSessionDto(
            s.Id,
            s.JobRoleCategorySnapshot,
            ExtractScorePercent(s.SkillScores),
            s.SubmittedAt,
            s.AssessmentPath?.Jd?.JobTitle))
        .ToList();
    }

    private static decimal ExtractScorePercent(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0m;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("scorePercent", out var sp) && sp.ValueKind == JsonValueKind.Number)
            {
                return sp.GetDecimal();
            }
        }
        catch { }
        return 0m;
    }
}
