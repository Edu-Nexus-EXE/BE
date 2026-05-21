using System.Text.Json;
using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;

namespace Edu_Nexus.Application.Features.CvSubmissions.Queries;

public record GetCvByPathQuery(Guid PathId) : IRequest<CvDetailDto>;

public class GetCvByPathQueryHandler : IRequestHandler<GetCvByPathQuery, CvDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetCvByPathQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CvDetailDto> Handle(GetCvByPathQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var cv = await _unitOfWork.CvSubmissions
            .FirstOrDefaultAsync(c => c.AssessmentPathId == request.PathId && c.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 CV_NOT_FOUND");

        var (skills, totalYears) = DeserializeSkills(cv.ParsedSkills);

        return new CvDetailDto(
            cv.Id,
            cv.FileName,
            cv.ParseStatus.ToString().ToLowerInvariant(),
            skills,
            totalYears,
            cv.ParsedAt,
            cv.ParseError);
    }

    private static (IReadOnlyList<CvSkillDto>? Skills, decimal? TotalYears) DeserializeSkills(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return (null, null);

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            decimal? totalYears = root.TryGetProperty("totalExperienceYears", out var ty) && ty.ValueKind == JsonValueKind.Number
                ? ty.GetDecimal()
                : null;

            var skillsList = new List<CvSkillDto>();
            if (root.TryGetProperty("skills", out var skillsEl) && skillsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in skillsEl.EnumerateArray())
                {
                    skillsList.Add(new CvSkillDto(
                        SkillName: s.GetProperty("skillName").GetString() ?? "",
                        ProficiencyLevel: s.TryGetProperty("proficiencyLevel", out var pl) ? pl.GetString() ?? "beginner" : "beginner",
                        YearsExp: s.TryGetProperty("yearsExp", out var ye) && ye.ValueKind == JsonValueKind.Number ? ye.GetDecimal() : null,
                        Evidence: s.TryGetProperty("evidence", out var ev) ? ev.GetString() : null
                    ));
                }
            }

            return (skillsList, totalYears);
        }
        catch
        {
            return (null, null);
        }
    }
}
