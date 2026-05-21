using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using MediatR;

namespace Edu_Nexus.Application.Features.AssessmentPaths.Commands;

public record DeleteAssessmentPathCommand(Guid JdId) : IRequest<Unit>;

public class DeleteAssessmentPathCommandHandler : IRequestHandler<DeleteAssessmentPathCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAssessmentPathCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteAssessmentPathCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var path = await _unitOfWork.AssessmentPaths
            .FirstOrDefaultAsync(p => p.JdId == request.JdId && p.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 PATH_NOT_FOUND");

        var hasCompletedGap = (await _unitOfWork.GapAnalyses.FindAsync(
            g => g.JdId == request.JdId && g.Status == GapAnalysisStatus.Completed,
            "", cancellationToken)).Any();

        if (hasCompletedGap)
        {
            throw new Exception("422 CANNOT_RESET_AFTER_GAP");
        }

        _unitOfWork.AssessmentPaths.Remove(path);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
