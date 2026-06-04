using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Application.Interfaces.Storage;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.RagDocuments;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.RagDocuments.Commands;

public record UploadRagDocumentCommand(
    Stream Content,
    string FileName,
    long FileSize,
    string MimeType,
    string Title,
    string SourceType,
    IReadOnlyList<Guid> RelatedSkillIds) : IRequest<RagDocumentAcceptedDto>;

public class UploadRagDocumentCommandHandler : IRequestHandler<UploadRagDocumentCommand, RagDocumentAcceptedDto>
{
    private const long MaxFileSize = 50 * 1024 * 1024;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly IRagIngestionQueue _queue;
    private readonly IAdminAuditLogger _audit;

    public UploadRagDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        IRagIngestionQueue queue,
        IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _queue = queue;
        _audit = audit;
    }

    public async Task<RagDocumentAcceptedDto> Handle(UploadRagDocumentCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        if (string.IsNullOrWhiteSpace(request.Title)) throw new Exception("422 TITLE_REQUIRED");
        if (request.Content == null || request.FileSize <= 0) throw new Exception("422 EMPTY_FILE");
        if (request.FileSize > MaxFileSize) throw new Exception("422 FILE_TOO_LARGE");

        var ext = Path.GetExtension(request.FileName)?.ToLowerInvariant();
        if (ext != ".pdf") throw new Exception("422 INVALID_FILE_TYPE");
        if (!string.Equals(request.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new Exception("422 INVALID_FILE_TYPE");

        var sourceType = ParseSourceType(request.SourceType);

        var fileUrl = await _fileStorage.SaveAsync(request.Content, "uploads/rag", ".pdf", cancellationToken);

        var doc = new RagDocument
        {
            UploadedBy = adminId,
            Title = request.Title.Trim(),
            SourceType = sourceType,
            FileUrl = fileUrl,
            RelatedSkillIds = request.RelatedSkillIds?.ToList() ?? new List<Guid>(),
            ChunksCount = 0,
            EmbeddingStatus = EmbeddingStatus.Pending,
        };

        _unitOfWork.RagDocuments.Add(doc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _queue.Enqueue(doc.Id);

        await _audit.LogAsync(adminId, "upload_rag_document", "rag_document", doc.Id,
            new { title = doc.Title, sourceType = request.SourceType, relatedSkillCount = doc.RelatedSkillIds.Count },
            cancellationToken);

        return new RagDocumentAcceptedDto(
            doc.Id,
            doc.Title,
            doc.EmbeddingStatus.ToString().ToLowerInvariant(),
            doc.ChunksCount);
    }

    private static RagDocumentSourceType ParseSourceType(string raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "fptu_curriculum" => RagDocumentSourceType.FptuCurriculum,
        "fptu_syllabus" => RagDocumentSourceType.FptuSyllabus,
        "external_doc" => RagDocumentSourceType.ExternalDoc,
        _ => throw new Exception("422 INVALID_SOURCE_TYPE")
    };
}
