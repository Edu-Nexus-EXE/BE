using Edu_Nexus.Application.Interfaces.Parsing;

namespace Edu_Nexus.Infrastructure.Parsing;

/// Heuristic parser: scans rawContent for keywords to produce realistic-looking fixtures.
/// Replace with real LLM-backed parser by swapping the DI registration.
public class FakeJdParser : IJdParser
{
    private static readonly Dictionary<string, (string Category, string Title)> RoleKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["java"]       = ("backend_java", "Backend Java Developer"),
        ["spring"]     = ("backend_java", "Backend Java Developer"),
        [".net"]       = ("backend_dotnet", "Backend .NET Developer"),
        ["dotnet"]     = ("backend_dotnet", "Backend .NET Developer"),
        ["c#"]         = ("backend_dotnet", "Backend .NET Developer"),
        ["node"]       = ("backend_node", "Backend Node.js Developer"),
        ["express"]    = ("backend_node", "Backend Node.js Developer"),
        ["python"]     = ("backend_python", "Backend Python Developer"),
        ["django"]     = ("backend_python", "Backend Python Developer"),
        ["react"]      = ("frontend_react", "Frontend React Developer"),
        ["next.js"]    = ("frontend_react", "Frontend Next.js Developer"),
        ["vue"]        = ("frontend_vue", "Frontend Vue Developer"),
        ["angular"]    = ("frontend_angular", "Frontend Angular Developer"),
        ["flutter"]    = ("mobile_flutter", "Mobile Flutter Developer"),
        ["react native"] = ("mobile_react_native", "Mobile React Native Developer"),
        ["android"]    = ("mobile_android", "Android Developer"),
        ["ios"]        = ("mobile_ios", "iOS Developer"),
        ["devops"]     = ("devops", "DevOps Engineer"),
        ["data engineer"] = ("data_engineer", "Data Engineer"),
        ["data scientist"] = ("data_scientist", "Data Scientist"),
        ["machine learning"] = ("ml_engineer", "Machine Learning Engineer"),
        ["qa"]         = ("qa_engineer", "QA Engineer"),
        ["tester"]     = ("qa_engineer", "QA Engineer"),
    };

    private static readonly string[] HardSkillCandidates =
    {
        "Java", "Spring Boot", "Spring", "Hibernate", "JPA",
        ".NET", "ASP.NET", "C#", "Entity Framework",
        "Node.js", "Express", "NestJS",
        "Python", "Django", "FastAPI", "Flask",
        "React", "Next.js", "Vue", "Angular", "TypeScript", "JavaScript",
        "PostgreSQL", "MySQL", "MongoDB", "Redis",
        "Docker", "Kubernetes", "AWS", "Azure", "GCP",
        "Git", "CI/CD", "Jenkins", "GitHub Actions",
        "REST API", "GraphQL", "gRPC", "Kafka", "RabbitMQ",
        "Flutter", "Dart", "Kotlin", "Swift",
        "Linux", "Bash"
    };

    private static readonly string[] SoftSkillCandidates =
    {
        "Teamwork", "Communication", "Problem Solving",
        "English", "Tiếng Anh", "Time Management", "Critical Thinking"
    };

    private static readonly (string Keyword, string Level)[] SeniorityKeywords =
    {
        ("intern",  "intern"),
        ("fresher", "fresher"),
        ("junior",  "junior"),
        ("middle",  "middle"),
        ("senior",  "senior"),
        ("lead",    "lead"),
    };

    public Task<ParsedJdResult> ParseAsync(string rawContent, CancellationToken cancellationToken = default)
    {
        var content = rawContent ?? string.Empty;

        var role = DetectRole(content);
        var seniority = DetectSeniority(content);
        var (salaryMin, salaryMax, currency) = DetectSalary(content);

        var hardSkills = HardSkillCandidates
            .Where(s => ContainsKeyword(content, s))
            .Take(10)
            .Select(s => new ParsedJdSkill(s, IsMandatory: true))
            .ToList();

        if (hardSkills.Count == 0)
        {
            hardSkills.Add(new ParsedJdSkill("Programming Fundamentals", IsMandatory: true));
        }

        var softSkills = SoftSkillCandidates
            .Where(s => ContainsKeyword(content, s))
            .Take(5)
            .Select(s => new ParsedJdSkill(s, IsMandatory: false))
            .ToList();

        return Task.FromResult(new ParsedJdResult(
            JobTitle: role.Title,
            JobRoleCategory: role.Category,
            SeniorityLevel: seniority,
            SalaryMin: salaryMin,
            SalaryMax: salaryMax,
            Currency: currency,
            HardSkills: hardSkills,
            SoftSkills: softSkills));
    }

    private static (string Category, string Title) DetectRole(string content)
    {
        foreach (var kvp in RoleKeywords)
        {
            if (ContainsKeyword(content, kvp.Key))
            {
                return kvp.Value;
            }
        }
        return ("general_software", "Software Developer");
    }

    private static string DetectSeniority(string content)
    {
        foreach (var (keyword, level) in SeniorityKeywords)
        {
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return level;
            }
        }
        return "junior";
    }

    private static (int? Min, int? Max, string? Currency) DetectSalary(string content)
    {
        var lower = content.ToLowerInvariant();
        var hasVnd = lower.Contains("vnd") || lower.Contains("triệu") || lower.Contains("trieu");
        var hasUsd = lower.Contains("usd") || lower.Contains("$");

        if (!hasVnd && !hasUsd) return (null, null, null);

        var currency = hasUsd && !hasVnd ? "USD" : "VND";
        return currency == "VND"
            ? (8_000_000, 15_000_000, "VND")
            : (500, 1500, "USD");
    }

    private static bool ContainsKeyword(string content, string keyword)
    {
        return content.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}
