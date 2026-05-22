using System.Text.Json;
using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;

namespace Edu_Nexus.Application.Features.GapAnalyses.Queries;

public record GetGapAnalysisQuery(Guid JdId, bool All) : IRequest<IReadOnlyList<GapAnalysisDetailDto>>;

public class GetGapAnalysisQueryHandler : IRequestHandler<GetGapAnalysisQuery, IReadOnlyList<GapAnalysisDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetGapAnalysisQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<GapAnalysisDetailDto>> Handle(GetGapAnalysisQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var jd = await _unitOfWork.JdSubmissions.FirstOrDefaultAsync(
            j => j.Id == request.JdId && j.UserId == userId && j.DeletedAt == null,
            "", cancellationToken)
            ?? throw new Exception("404 JD_NOT_FOUND");

        var gaps = (await _unitOfWork.GapAnalyses.FindAsync(
            g => g.JdId == request.JdId && g.UserId == userId && (request.All || g.IsLatest),
            includeProperties: nameof(GapAnalysis.GapAnalysisSkills),
            cancellationToken: cancellationToken))
            .OrderByDescending(g => g.Version)
            .ToList();

        if (gaps.Count == 0)
        {
            throw new Exception("404 GAP_NOT_FOUND");
        }

        return gaps.Select(MapToDto).ToList();
    }

    private static GapAnalysisDetailDto MapToDto(GapAnalysis g)
    {
        var summary = DeserializeSummary(g.Summary);

        var skills = g.GapAnalysisSkills
            .OrderByDescending(s => s.UrgencyScore)
            .Select(s => new GapSkillDto(
                s.Id,
                s.SkillName,
                s.SkillId,
                s.GapStatus.ToString() switch
                {
                    "Missing" => "missing",
                    "NeedsUpgrade" => "needs_upgrade",
                    _ => "have"
                },
                s.CurrentLevel?.ToString().ToLowerInvariant(),
                s.TargetLevel.ToString().ToLowerInvariant(),
                s.UrgencyScore,
                s.Reasoning,
                s.IsMandatoryInJd))
            .ToList();

        return new GapAnalysisDetailDto(
            g.Id,
            g.Version,
            g.IsLatest,
            g.InputSource.ToString().ToLowerInvariant(),
            g.Status.ToString().ToLowerInvariant(),
            summary,
            skills,
            g.CompletedAt);
    }

    private static string? DeserializeSummary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("text", out var t))
            {
                return t.GetString();
            }
            return json;
        }
        catch
        {
            return json;
        }
    }
}
