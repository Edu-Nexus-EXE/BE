using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Application.Interfaces.Storage;
using Edu_Nexus.Infrastructure.BackgroundJobs;
using Edu_Nexus.Infrastructure.Data;
using Edu_Nexus.Infrastructure.Jobs;
using Edu_Nexus.Infrastructure.Parsing;
using Edu_Nexus.Infrastructure.Security;
using Edu_Nexus.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edu_Nexus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddSecurity();
        services.AddBackgroundJobs(configuration);
        services.AddParsing();
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EduNexusDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseVector()
            )
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    private static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        return services;
    }

    private static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection for Hangfire.");

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        services.AddScoped<IJdParseQueue, HangfireJdParseQueue>();
        services.AddScoped<JdParseJob>();

        services.AddScoped<ICvParseQueue, HangfireCvParseQueue>();
        services.AddScoped<CvParseJob>();

        services.AddScoped<IAssessmentGenerateQueue, HangfireAssessmentGenerateQueue>();
        services.AddScoped<AssessmentGenerateJob>();
        return services;
    }

    private static IServiceCollection AddParsing(this IServiceCollection services)
    {
        services.AddScoped<IJdParser, FakeJdParser>();
        services.AddScoped<ICvParser, FakeCvParser>();
        services.AddScoped<IAssessmentQuestionGenerator, FakeAssessmentQuestionGenerator>();
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<IAnonymizer, RegexAnonymizer>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        return services;
    }
}
