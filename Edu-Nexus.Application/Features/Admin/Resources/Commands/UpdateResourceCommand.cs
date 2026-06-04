using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Admin.Resources.Queries;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Resources.Commands;

public record UpdateResourceCommand(Guid Id, AdminResourceUpsertRequest Request) : IRequest<AdminResourceDto>;

public class UpdateResourceCommandHandler : IRequestHandler<UpdateResourceCommand, AdminResourceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public UpdateResourceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<AdminResourceDto> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var resource = await _unitOfWork.LearningResources.FirstOrDefaultAsync(
            r => r.Id == request.Id, nameof(LearningResource.SkillResources), cancellationToken)
            ?? throw new Exception("404 RESOURCE_NOT_FOUND");

        var dto = request.Request;
        if (string.IsNullOrWhiteSpace(dto.Title)) throw new Exception("422 TITLE_REQUIRED");
        if (string.IsNullOrWhiteSpace(dto.Url)) throw new Exception("422 URL_REQUIRED");
        if (string.IsNullOrWhiteSpace(dto.Language)) throw new Exception("422 LANGUAGE_REQUIRED");

        resource.Title = dto.Title.Trim();
        resource.Type = ResourceMapping.ParseType(dto.Type) ?? throw new Exception("422 INVALID_RESOURCE_TYPE");
        resource.Provider = dto.Provider;
        resource.Url = dto.Url.Trim();
        resource.Description = dto.Description;
        resource.IsFree = dto.IsFree;
        resource.AccessType = ResourceMapping.ParseAccessType(dto.AccessType);
        resource.AffiliateLabel = dto.AffiliateLabel;
        resource.AffiliateCommissionRate = dto.AffiliateCommissionRate;
        resource.Language = dto.Language.Trim();
        resource.DurationMinutes = dto.DurationMinutes;
        if (dto.NeedsAdminReview.HasValue) resource.NeedsAdminReview = dto.NeedsAdminReview.Value;
        if (dto.IsActive.HasValue) resource.IsActive = dto.IsActive.Value;
        resource.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.LearningResources.Update(resource);

        if (dto.SkillMappings != null)
        {
            foreach (var existing in resource.SkillResources.ToList())
            {
                _unitOfWork.SkillResources.Remove(existing);
            }
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
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, "update_resource", "learning_resource", resource.Id,
            new { title = resource.Title }, cancellationToken);

        var saved = await _unitOfWork.LearningResources.FirstOrDefaultAsync(
            r => r.Id == resource.Id, nameof(LearningResource.SkillResources), cancellationToken);
        return ResourceMapping.ToDto(saved!);
    }
}
