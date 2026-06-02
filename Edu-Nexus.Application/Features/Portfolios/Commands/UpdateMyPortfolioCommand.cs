using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using MediatR;

namespace Edu_Nexus.Application.Features.Portfolios.Commands;

public record UpdateMyPortfolioCommand(UpdatePortfolioRequest Request) : IRequest<PortfolioResponseData>;

public class UpdateMyPortfolioCommandHandler : IRequestHandler<UpdateMyPortfolioCommand, PortfolioResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public UpdateMyPortfolioCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<PortfolioResponseData> Handle(UpdateMyPortfolioCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var portfolio = await _unitOfWork.Portfolios.FirstOrDefaultAsync(p => p.UserId == userId, "", cancellationToken);

        if (portfolio == null)
        {
            portfolio = new Portfolio
            {
                UserId = userId,
                Headline = command.Request.Headline,
                Bio = command.Request.Bio,
                CoverImageUrl = command.Request.CoverImageUrl,
                ShowCompletedSkills = command.Request.ShowCompletedSkills,
                ShowCertificates = command.Request.ShowCertificates,
                ShowProjects = command.Request.ShowProjects,
                IsPublic = command.Request.IsPublic,
                UpdatedAt = DateTime.UtcNow
            };
            _unitOfWork.Portfolios.Add(portfolio);
        }
        else
        {
            portfolio.Headline = command.Request.Headline;
            portfolio.Bio = command.Request.Bio;
            portfolio.CoverImageUrl = command.Request.CoverImageUrl;
            portfolio.ShowCompletedSkills = command.Request.ShowCompletedSkills;
            portfolio.ShowCertificates = command.Request.ShowCertificates;
            portfolio.ShowProjects = command.Request.ShowProjects;
            portfolio.IsPublic = command.Request.IsPublic;
            portfolio.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Portfolios.Update(portfolio);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reuse query to get full updated response
        return await _mediator.Send(new Queries.GetMyPortfolioQuery(), cancellationToken);
    }
}
