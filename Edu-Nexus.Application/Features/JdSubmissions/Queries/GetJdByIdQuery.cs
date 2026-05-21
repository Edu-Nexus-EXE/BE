using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.JdSkills;
using MediatR;

namespace Edu_Nexus.Application.Features.JdSubmissions.Queries;

public record GetJdByIdQuery(Guid JdId) : IRequest<JdSubmissionDetailDto>;

public class GetJdByIdQueryHandler : IRequestHandler<GetJdByIdQuery, JdSubmissionDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetJdByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<JdSubmissionDetailDto> Handle(GetJdByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var jd = await _unitOfWork.JdSubmissions.FirstOrDefaultAsync(
            j => j.Id == request.JdId && j.UserId == userId && j.DeletedAt == null,
            includeProperties: $"{nameof(JdSubmission.JdSkills)},{nameof(JdSubmission.AssessmentPath)}",
            cancellationToken: cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        var hard = jd.JdSkills
            .Where(s => s.SkillType == SkillType.HardSkill)
            .Select(s => new JdSkillDto(s.Id, s.SkillNameRaw, s.SkillId, s.IsMandatory))
            .ToList();

        var soft = jd.JdSkills
            .Where(s => s.SkillType == SkillType.SoftSkill)
            .Select(s => new JdSkillDto(s.Id, s.SkillNameRaw, s.SkillId, s.IsMandatory))
            .ToList();

        AssessmentPathInJdDto? pathDto = jd.AssessmentPath == null
            ? null
            : new AssessmentPathInJdDto(jd.AssessmentPath.Id, jd.AssessmentPath.PathType.ToString().ToLowerInvariant());

        return new JdSubmissionDetailDto(
            jd.Id,
            jd.SourceType.ToString().ToLowerInvariant(),
            jd.SourceUrl,
            jd.JobTitle,
            jd.JobRoleCategory,
            jd.SeniorityLevel,
            jd.SalaryMin,
            jd.SalaryMax,
            jd.Currency,
            jd.ParseStatus.ToString().ToLowerInvariant(),
            jd.ParsedAt,
            hard,
            soft,
            pathDto,
            jd.CreatedAt);
    }
}
