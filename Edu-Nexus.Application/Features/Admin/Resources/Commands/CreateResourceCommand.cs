using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Admin.Resources.Queries;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Resources.Commands;

public record CreateResourceCommand(AdminResourceUpsertRequest Request) : IRequest<AdminResourceDto>;

public class CreateResourceCommandHandler : IRequestHandler<CreateResourceCommand, AdminResourceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public CreateResourceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<AdminResourceDto> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var dto = request.Request;
        if (string.IsNullOrWhiteSpace(dto.Title)) throw new Exception("422 TITLE_REQUIRED");
        if (string.IsNullOrWhiteSpace(dto.Url)) throw new Exception("422 URL_REQUIRED");
        if (string.IsNullOrWhiteSpace(dto.Language)) throw new Exception("422 LANGUAGE_REQUIRED");

        var type = ResourceMapping.ParseType(dto.Type) ?? throw new Exception("422 INVALID_RESOURCE_TYPE");
        var access = ResourceMapping.ParseAccessType(dto.AccessType);

        var resource = new LearningResource
        {
            Title = dto.Title.Trim(),
            Type = type,
            Provider = dto.Provider,
            Url = dto.Url.Trim(),
            Description = dto.Description,
            IsFree = dto.IsFree,
            AccessType = access,
            AffiliateLabel = dto.AffiliateLabel,
            AffiliateCommissionRate = dto.AffiliateCommissionRate,
            Language = dto.Language.Trim(),
            DurationMinutes = dto.DurationMinutes,
            NeedsAdminReview = dto.NeedsAdminReview ?? false,
            IsActive = dto.IsActive ?? true,
        };

        _unitOfWork.LearningResources.Add(resource);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (dto.SkillMappings is { Count: > 0 })
        {
            foreach (var mapping in dto.SkillMappings)
            {
                _unitOfWork.SkillResources.Add(new SkillResource
                {
                    SkillId = mapping.SkillId,
                    ResourceId = resource.Id,
                    IsPrimary = mapping.IsPrimary,
                    SequenceOrder = mapping.SequenceOrder,
                });
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var saved = await _unitOfWork.LearningResources.FirstOrDefaultAsync(
            r => r.Id == resource.Id, nameof(LearningResource.SkillResources), cancellationToken);

        await _audit.LogAsync(adminId, "create_resource", "learning_resource", resource.Id,
            new { title = resource.Title, type = ResourceMapping.TypeToString(resource.Type) },
            cancellationToken);

        return ResourceMapping.ToDto(saved!);
    }
}
