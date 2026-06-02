using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;
using System;

namespace Edu_Nexus.Application.Features.Portfolios.Commands;

// ADD
public record AddCertificateCommand(AddCertificateRequest Request) : IRequest<PortfolioCertificateDto>;

public class AddCertificateCommandHandler : IRequestHandler<AddCertificateCommand, PortfolioCertificateDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddCertificateCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PortfolioCertificateDto> Handle(AddCertificateCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        // Quota check
        var subscription = await _unitOfWork.UserSubscriptions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active,
            "Tier", cancellationToken);
            
        var quota = subscription?.Tier?.PortfolioCertificateQuota ?? 3;
        
        if (quota >= 0)
        {
            var count = (await _unitOfWork.PortfolioCertificates.FindAsync(c => c.UserId == userId, "", cancellationToken)).Count();
            if (count >= quota)
            {
                throw new Exception($"403 QUOTA_EXCEEDED|certificate|{count}|{quota}");
            }
        }

        var cert = new PortfolioCertificate
        {
            UserId = userId,
            Name = command.Request.Name,
            Issuer = command.Request.Issuer,
            IssuedDate = string.IsNullOrWhiteSpace(command.Request.IssuedDate) ? null : DateOnly.Parse(command.Request.IssuedDate),
            ExpiresDate = string.IsNullOrWhiteSpace(command.Request.ExpiresDate) ? null : DateOnly.Parse(command.Request.ExpiresDate),
            CredentialUrl = command.Request.CredentialUrl,
            FileUrl = command.Request.FileUrl,
            IsVisible = command.Request.IsVisible,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.PortfolioCertificates.Add(cert);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PortfolioCertificateDto
        {
            Id = cert.Id,
            Name = cert.Name,
            Issuer = cert.Issuer,
            IssuedDate = cert.IssuedDate?.ToString("yyyy-MM-dd"),
            ExpiresDate = cert.ExpiresDate?.ToString("yyyy-MM-dd"),
            CredentialUrl = cert.CredentialUrl,
            FileUrl = cert.FileUrl,
            IsVisible = cert.IsVisible
        };
    }
}

// UPDATE
public record UpdateCertificateCommand(Guid Id, AddCertificateRequest Request) : IRequest<PortfolioCertificateDto>;

public class UpdateCertificateCommandHandler : IRequestHandler<UpdateCertificateCommand, PortfolioCertificateDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCertificateCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PortfolioCertificateDto> Handle(UpdateCertificateCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var cert = await _unitOfWork.PortfolioCertificates.FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        cert.Name = command.Request.Name;
        cert.Issuer = command.Request.Issuer;
        cert.IssuedDate = string.IsNullOrWhiteSpace(command.Request.IssuedDate) ? null : DateOnly.Parse(command.Request.IssuedDate);
        cert.ExpiresDate = string.IsNullOrWhiteSpace(command.Request.ExpiresDate) ? null : DateOnly.Parse(command.Request.ExpiresDate);
        cert.CredentialUrl = command.Request.CredentialUrl;
        cert.FileUrl = command.Request.FileUrl;
        cert.IsVisible = command.Request.IsVisible;

        _unitOfWork.PortfolioCertificates.Update(cert);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PortfolioCertificateDto
        {
            Id = cert.Id,
            Name = cert.Name,
            Issuer = cert.Issuer,
            IssuedDate = cert.IssuedDate?.ToString("yyyy-MM-dd"),
            ExpiresDate = cert.ExpiresDate?.ToString("yyyy-MM-dd"),
            CredentialUrl = cert.CredentialUrl,
            FileUrl = cert.FileUrl,
            IsVisible = cert.IsVisible
        };
    }
}

// DELETE
public record DeleteCertificateCommand(Guid Id) : IRequest;

public class DeleteCertificateCommandHandler : IRequestHandler<DeleteCertificateCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCertificateCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteCertificateCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var cert = await _unitOfWork.PortfolioCertificates.FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        _unitOfWork.PortfolioCertificates.Remove(cert);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// TOGGLE VISIBILITY
public record ToggleCertificateVisibilityCommand(Guid Id, bool IsVisible) : IRequest<PortfolioCertificateDto>;

public class ToggleCertificateVisibilityCommandHandler : IRequestHandler<ToggleCertificateVisibilityCommand, PortfolioCertificateDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ToggleCertificateVisibilityCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PortfolioCertificateDto> Handle(ToggleCertificateVisibilityCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var cert = await _unitOfWork.PortfolioCertificates.FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == userId, "", cancellationToken)
            ?? throw new Exception("404 NOT_FOUND");

        cert.IsVisible = command.IsVisible;

        _unitOfWork.PortfolioCertificates.Update(cert);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PortfolioCertificateDto
        {
            Id = cert.Id,
            Name = cert.Name,
            Issuer = cert.Issuer,
            IssuedDate = cert.IssuedDate?.ToString("yyyy-MM-dd"),
            ExpiresDate = cert.ExpiresDate?.ToString("yyyy-MM-dd"),
            CredentialUrl = cert.CredentialUrl,
            FileUrl = cert.FileUrl,
            IsVisible = cert.IsVisible
        };
    }
}
