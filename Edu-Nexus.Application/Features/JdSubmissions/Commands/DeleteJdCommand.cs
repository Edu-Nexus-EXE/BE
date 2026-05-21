using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;

namespace Edu_Nexus.Application.Features.JdSubmissions.Commands;

public record DeleteJdCommand(Guid JdId) : IRequest<Unit>;

public class DeleteJdCommandHandler : IRequestHandler<DeleteJdCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteJdCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteJdCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var jd = await _unitOfWork.JdSubmissions
            .FirstOrDefaultAsync(j => j.Id == request.JdId && j.UserId == userId && j.DeletedAt == null, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        jd.DeletedAt = DateTime.UtcNow;
        _unitOfWork.JdSubmissions.Update(jd);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
