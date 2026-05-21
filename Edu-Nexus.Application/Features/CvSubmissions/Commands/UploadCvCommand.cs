using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Application.Interfaces.Storage;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentPaths;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using MediatR;

namespace Edu_Nexus.Application.Features.CvSubmissions.Commands;

public record UploadCvCommand(
    Guid PathId,
    Stream Content,
    string FileName,
    long FileSize,
    string MimeType) : IRequest<CvUploadAcceptedDto>;

public class UploadCvCommandHandler : IRequestHandler<UploadCvCommand, CvUploadAcceptedDto>
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly ICvParseQueue _cvParseQueue;

    public UploadCvCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        ICvParseQueue cvParseQueue)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _cvParseQueue = cvParseQueue;
    }

    public async Task<CvUploadAcceptedDto> Handle(UploadCvCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        ValidateFile(request);

        var path = await _unitOfWork.AssessmentPaths
            .FirstOrDefaultAsync(p => p.Id == request.PathId && p.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 PATH_NOT_FOUND");

        if (path.PathType != PathType.Cv)
        {
            throw new Exception("422 PATH_TYPE_MISMATCH");
        }

        var existing = await _unitOfWork.CvSubmissions
            .FirstOrDefaultAsync(c => c.AssessmentPathId == request.PathId, "", cancellationToken);

        if (existing != null)
        {
            await _fileStorage.DeleteAsync(existing.FileUrl, cancellationToken);
            _unitOfWork.CvSubmissions.Remove(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var fileUrl = await _fileStorage.SaveAsync(request.Content, "uploads/cv", ".pdf", cancellationToken);

        var isReupload = await HasCompletedGapAnalysisAsync(path.JdId, cancellationToken);

        var cv = new CvSubmission
        {
            AssessmentPathId = request.PathId,
            UserId = userId,
            FileUrl = fileUrl,
            FileName = request.FileName,
            FileSizeBytes = (int)request.FileSize,
            MimeType = request.MimeType,
            ParseStatus = ParseStatus.Pending,
        };

        _unitOfWork.CvSubmissions.Add(cv);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cvParseQueue.Enqueue(cv.Id, isReupload);

        return new CvUploadAcceptedDto(
            cv.Id,
            cv.FileName ?? request.FileName,
            cv.FileSizeBytes ?? 0,
            cv.ParseStatus.ToString().ToLowerInvariant());
    }

    private static void ValidateFile(UploadCvCommand request)
    {
        if (request.Content == null || request.FileSize <= 0)
        {
            throw new Exception("422 EMPTY_FILE");
        }
        if (request.FileSize > MaxFileSize)
        {
            throw new Exception("422 FILE_TOO_LARGE");
        }
        var ext = Path.GetExtension(request.FileName)?.ToLowerInvariant();
        if (ext != ".pdf")
        {
            throw new Exception("422 INVALID_FILE_TYPE");
        }
        if (!string.Equals(request.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("422 INVALID_FILE_TYPE");
        }
    }

    private async Task<bool> HasCompletedGapAnalysisAsync(Guid jdId, CancellationToken ct)
    {
        var gaps = await _unitOfWork.GapAnalyses.FindAsync(
            g => g.JdId == jdId && g.Status == GapAnalysisStatus.Completed,
            "", ct);
        return gaps.Any();
    }
}
