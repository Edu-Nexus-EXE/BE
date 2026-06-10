using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Configuration;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Application.Interfaces.Portfolios;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Application.Interfaces.Storage;
using Edu_Nexus.Infrastructure.Admin;
using Edu_Nexus.Infrastructure.BackgroundJobs;
using Edu_Nexus.Infrastructure.Configuration;
using Edu_Nexus.Infrastructure.Data;
using Edu_Nexus.Infrastructure.Jobs;
using Edu_Nexus.Infrastructure.Parsing;
using Edu_Nexus.Infrastructure.Portfolios;
using Edu_Nexus.Infrastructure.Security;
using Edu_Nexus.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Edu_Nexus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddSecurity();
        services.AddBackgroundJobs(configuration);
        services.AddParsing(configuration);
        services.AddSingleton<ISePaySettings>(sp => new SePaySettings(configuration));
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
        services.AddScoped<DataSeeder>();
        services.AddScoped<IRagService, RagService>();

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = redis;
                o.InstanceName = "edunexus:";
            });
        }

        return services;
    }

    private static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPortfolioUrlBuilder, PortfolioUrlBuilder>();
        services.AddScoped<IAdminAuditLogger, AdminAuditLogger>();
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

        services.AddScoped<IGapAnalysisQueue, HangfireGapAnalysisQueue>();
        services.AddScoped<GapAnalysisJob>();
        
        services.AddScoped<IRoadmapGenerateQueue, RoadmapGenerateQueue>();
        services.AddScoped<RoadmapGenerateJob>();

        services.AddScoped<IRagIngestionQueue, HangfireRagIngestionQueue>();
        services.AddScoped<RagIngestionJob>();

        services.AddScoped<ExpirePendingPaymentOrdersJob>();

        services.AddScoped<SubscriptionExpirationJob>();
        services.AddScoped<RenewalNotificationJob>();

        return services;
    }

    private static IServiceCollection AddParsing(this IServiceCollection services, IConfiguration configuration)
    {
        // Stateless / infrastructure-only services
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<IAnonymizer, RegexAnonymizer>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<ILlmResponseValidator, LlmResponseValidator>();
        services.AddScoped<IResourceSuggestionService, ResourceSuggestionService>();
        services.AddScoped<ISkillMatcherService, SkillMatcherService>();
        services.AddScoped<ISkillMatcherBatchService, SkillMatcherBatchService>();
        services.AddScoped<IRoadmapGeneratorService, RoadmapGeneratorService>();
        services.AddHttpClient<IJdUrlFetcherService, JdUrlFetcherService>();
        services.AddHttpClient<IUrlVerificationService, UrlVerificationService>();

        // Semantic Kernel: 2 chat models (fast/smart) + embedding
        var openAiApiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(openAiApiKey))
        {
            var fast = configuration["OpenAI:Models:Fast"] ?? "gpt-4o-mini";
            var smart = configuration["OpenAI:Models:Smart"] ?? "gpt-4o";
            var embedding = configuration["OpenAI:Embedding"] ?? "text-embedding-3-small";

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOpenAIChatCompletion(fast, openAiApiKey, serviceId: "fast");
            kernelBuilder.AddOpenAIChatCompletion(smart, openAiApiKey, serviceId: "smart");
#pragma warning disable SKEXP0010
            kernelBuilder.AddOpenAITextEmbeddingGeneration(embedding, openAiApiKey);
#pragma warning restore SKEXP0010
            services.AddSingleton(kernelBuilder.Build());

            // Embedding service resolves ITextEmbeddingGenerationService from the kernel
#pragma warning disable SKEXP0001
            services.AddSingleton(sp =>
                sp.GetRequiredService<Kernel>().GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());
#pragma warning restore SKEXP0001
            services.AddScoped<IEmbeddingService, EmbeddingService>();
        }

        services.AddScoped<ILlmService, LlmService>();

        // Always register both fake and AI parsers; the binding for the I* interface
        // is decided by the "Ai:Enabled" flag (or per-pipeline overrides) below.
        services.AddScoped<FakeJdParser>();
        services.AddScoped<FakeCvParser>();
        services.AddScoped<FakeAssessmentQuestionGenerator>();
        services.AddScoped<FakeGapAnalyzer>();
        services.AddScoped<OpenAiGapAnalyzer>();

        var aiEnabled = configuration.GetValue<bool>("Ai:Enabled", false);

        // JD / CV / Question generators stay on the fake heuristic until their OpenAI
        // implementations land. Wiring them follows the exact same pattern as the
        // gap analyzer below.
        services.AddScoped<OpenAiJdParser>();
        services.AddScoped<IJdParser>(sp => UsePipeline(configuration, "JdParse")
            ? sp.GetRequiredService<OpenAiJdParser>()
            : sp.GetRequiredService<FakeJdParser>());
        services.AddScoped<OpenAiCvParser>();
        services.AddScoped<ICvParser>(sp => UsePipeline(configuration, "CvParse")
            ? sp.GetRequiredService<OpenAiCvParser>()
            : sp.GetRequiredService<FakeCvParser>());
        services.AddScoped<OpenAiAssessmentQuestionGenerator>();
        services.AddScoped<IAssessmentQuestionGenerator>(sp => UsePipeline(configuration, "AssessmentGen")
            ? sp.GetRequiredService<OpenAiAssessmentQuestionGenerator>()
            : sp.GetRequiredService<FakeAssessmentQuestionGenerator>());

        services.AddScoped<IGapAnalyzer>(sp =>
            aiEnabled
                ? sp.GetRequiredService<OpenAiGapAnalyzer>()
                : sp.GetRequiredService<FakeGapAnalyzer>());

        return services;
    }

    private static bool UsePipeline(IConfiguration config, string name)
    {
        var perPipeline = config.GetValue<bool?>($"Ai:Pipelines:{name}");
        if (perPipeline.HasValue) return perPipeline.Value;
        return config.GetValue<bool>("Ai:Enabled", false);
    }
}
