using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Admin.Resources.Queries;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Resources.Commands;

public record ReviewResourceCommand(Guid Id, AdminResourceReviewRequest Request) : IRequest<AdminResourceDto>;

public class ReviewResourceCommandHandler : IRequestHandler<ReviewResourceCommand, AdminResourceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public ReviewResourceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<AdminResourceDto> Handle(ReviewResourceCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var resource = await _unitOfWork.LearningResources.FirstOrDefaultAsync(
            r => r.Id == request.Id, nameof(LearningResource.SkillResources), cancellationToken)
            ?? throw new Exception("404 RESOURCE_NOT_FOUND");

        resource.NeedsAdminReview = request.Request.NeedsAdminReview;
        resource.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.LearningResources.Update(resource);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, "review_resource", "learning_resource", resource.Id,
            new { needsAdminReview = request.Request.NeedsAdminReview }, cancellationToken);

        return ResourceMapping.ToDto(resource);
    }
}

public record RejectResourceCommand(Guid Id) : IRequest<Unit>;

public class RejectResourceCommandHandler : IRequestHandler<RejectResourceCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public RejectResourceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<Unit> Handle(RejectResourceCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var resource = await _unitOfWork.LearningResources.FirstOrDefaultAsync(
            r => r.Id == request.Id, "", cancellationToken)
            ?? throw new Exception("404 RESOURCE_NOT_FOUND");

        resource.IsActive = false;
        resource.NeedsAdminReview = false;
        resource.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.LearningResources.Update(resource);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, "reject_resource", "learning_resource", resource.Id, null, cancellationToken);
        return Unit.Value;
    }
}
