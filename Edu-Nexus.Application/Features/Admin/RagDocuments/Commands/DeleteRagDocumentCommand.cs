using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Application.Interfaces.Storage;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.RagDocuments.Commands;

public record DeleteRagDocumentCommand(Guid Id) : IRequest<Unit>;

public class DeleteRagDocumentCommandHandler : IRequestHandler<DeleteRagDocumentCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public DeleteRagDocumentCommandHandler(IUnitOfWork unitOfWork, IFileStorage fileStorage, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<Unit> Handle(DeleteRagDocumentCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var doc = await _unitOfWork.RagDocuments.FirstOrDefaultAsync(
            d => d.Id == request.Id, "", cancellationToken)
            ?? throw new Exception("404 DOCUMENT_NOT_FOUND");

        if (!string.IsNullOrEmpty(doc.FileUrl))
        {
            await _fileStorage.DeleteAsync(doc.FileUrl, cancellationToken);
        }

        _unitOfWork.RagDocuments.Remove(doc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, "delete_rag_document", "rag_document", doc.Id, null, cancellationToken);
        return Unit.Value;
    }
}
