using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.JdSubmissions.Commands;

public record SubmitJdCommand(SubmitJdRequest Request) : IRequest<JdSubmissionAcceptedDto>;

public class SubmitJdCommandHandler : IRequestHandler<SubmitJdCommand, JdSubmissionAcceptedDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJdParseQueue _jdParseQueue;

    public SubmitJdCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IJdParseQueue jdParseQueue)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _jdParseQueue = jdParseQueue;
    }

    public async Task<JdSubmissionAcceptedDto> Handle(SubmitJdCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var sourceType = ParseSourceType(request.Request.SourceType);
        ValidateContent(sourceType, request.Request);

        var user = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, "", cancellationToken)
            ?? throw new Exception("401 UNAUTHORIZED");

        if (!user.IsSurveyCompleted)
        {
            throw new Exception("422 ONBOARDING_REQUIRED");
        }

        await EnforceJdQuotaAsync(userId, cancellationToken);

        var jd = new JdSubmission
        {
            UserId = userId,
            SourceType = sourceType,
            SourceUrl = sourceType == JdSourceType.Url ? request.Request.SourceUrl : null,
            RawContent = request.Request.RawContent,
            ParseStatus = ParseStatus.Pending,
        };

        _unitOfWork.JdSubmissions.Add(jd);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _jdParseQueue.Enqueue(jd.Id);

        return new JdSubmissionAcceptedDto(
            jd.Id,
            sourceType.ToString().ToLowerInvariant(),
            jd.ParseStatus.ToString().ToLowerInvariant(),
            jd.CreatedAt == default ? DateTime.UtcNow : jd.CreatedAt);
    }

    private static JdSourceType ParseSourceType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new Exception("422 INVALID_SOURCE_TYPE");
        return raw.Trim().ToLowerInvariant() switch
        {
            "url" => JdSourceType.Url,
            "text" => JdSourceType.Text,
            _ => throw new Exception("422 INVALID_SOURCE_TYPE")
        };
    }

    private static void ValidateContent(JdSourceType sourceType, SubmitJdRequest req)
    {
        if (sourceType == JdSourceType.Url && string.IsNullOrWhiteSpace(req.SourceUrl))
        {
            throw new Exception("422 SOURCE_URL_REQUIRED");
        }
        if (string.IsNullOrWhiteSpace(req.RawContent))
        {
            throw new Exception("422 RAW_CONTENT_REQUIRED");
        }
        if (req.RawContent.Length > 50_000)
        {
            throw new Exception("422 CONTENT_TOO_LONG");
        }
    }

    private async Task EnforceJdQuotaAsync(Guid userId, CancellationToken ct)
    {
        var subscription = await _unitOfWork.UserSubscriptions
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active,
                includeProperties: nameof(UserSubscription.Tier),
                cancellationToken: ct);

        var jdQuota = subscription?.Tier?.JdQuota ?? 3;
        if (jdQuota < 0) return;

        var existing = await _unitOfWork.JdSubmissions
            .FindAsync(j => j.UserId == userId && j.DeletedAt == null, "", ct);

        if (existing.Count() >= jdQuota)
        {
            throw new Exception($"403 QUOTA_EXCEEDED|jd|{existing.Count()}|{jdQuota}");
        }
    }
}
