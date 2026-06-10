using Edu_Nexus.Application.Helpers;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using Edu_Nexus.Domain.Enums.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Data;

public class DataSeeder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger<DataSeeder> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public static IReadOnlyList<SubscriptionTier> BuildDefaultTiers() =>
    [
        new SubscriptionTier
        {
            TierCode = SubscriptionTierCode.Free,
            DisplayName = "Free",
            PriceMonthly = 0m,
            Currency = "VND",
            JdQuota = 3,
            GapAnalysisQuota = 3,
            AssessmentQuota = 3,
            RoadmapActiveQuota = 3,
            CareerTrackQuota = 1,
            PortfolioCertificateQuota = 3,
            PortfolioProjectQuota = 3,
            FullGapHistory = false,
            IsActive = true
        },
        new SubscriptionTier
        {
            TierCode = SubscriptionTierCode.Student,
            DisplayName = "Student",
            PriceMonthly = 79000m,
            Currency = "VND",
            JdQuota = -1,
            GapAnalysisQuota = -1,
            AssessmentQuota = -1,
            RoadmapActiveQuota = -1,
            CareerTrackQuota = -1,
            PortfolioCertificateQuota = -1,
            PortfolioProjectQuota = -1,
            FullGapHistory = true,
            IsActive = true
        }
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedSubscriptionTiersAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedSubscriptionTiersAsync(CancellationToken cancellationToken)
    {
        foreach (var tier in BuildDefaultTiers())
        {
            var existingTier = await _unitOfWork.SubscriptionTiers.FirstOrDefaultAsync(
                item => item.TierCode == tier.TierCode,
                "",
                cancellationToken);

            if (existingTier is not null)
            {
                continue;
            }

            _unitOfWork.SubscriptionTiers.Add(tier);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        try
        {
            var email = _configuration["Seed:Admin:Email"];
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            var existingAdmin = await _unitOfWork.Users.FirstOrDefaultAsync(
                user => user.Email == email,
                "",
                cancellationToken);
            if (existingAdmin is not null)
            {
                return;
            }

            var password = _configuration["Seed:Admin:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Seed admin email is configured but password is missing. Skipping admin seed.");
                return;
            }

            var fullName = _configuration["Seed:Admin:FullName"];
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = "Edu-Nexus Admin";
            }

            var baseSlug = SlugHelper.GenerateSlug(fullName);
            var finalSlug = baseSlug;
            while (true)
            {
                var slugExists = await _unitOfWork.Users.FirstOrDefaultAsync(
                    user => user.PortfolioUrlSlug == finalSlug,
                    "",
                    cancellationToken);
                if (slugExists is null)
                {
                    break;
                }

                finalSlug = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..4]}";
            }

            var admin = new User
            {
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password),
                FullName = fullName,
                AuthProvider = AuthProvider.Email,
                Role = UserRole.Admin,
                IsSurveyCompleted = true,
                PortfolioUrlSlug = finalSlug
            };

            _unitOfWork.Users.Add(admin);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed admin user.");
        }
    }
}
