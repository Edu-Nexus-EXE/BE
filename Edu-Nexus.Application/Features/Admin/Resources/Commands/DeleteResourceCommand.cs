using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Resources.Commands;

public record DeleteResourceCommand(Guid Id) : IRequest<Unit>;

public class DeleteResourceCommandHandler : IRequestHandler<DeleteResourceCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public DeleteResourceCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<Unit> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var resource = await _unitOfWork.LearningResources.FirstOrDefaultAsync(
            r => r.Id == request.Id, "", cancellationToken)
            ?? throw new Exception("404 RESOURCE_NOT_FOUND");

        _unitOfWork.LearningResources.Remove(resource);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, "delete_resource", "learning_resource", resource.Id, null, cancellationToken);

        return Unit.Value;
    }
}
