using System.Text.RegularExpressions;
using Edu_Nexus.Application.Interfaces.Parsing;

namespace Edu_Nexus.Infrastructure.Parsing;

/// Heuristic CV parser: scans extracted CV text for skill keywords and seniority signals.
/// Replace with an LLM-backed parser by swapping the DI registration of ICvParser.
public class FakeCvParser : ICvParser
{
    private static readonly string[] SkillCandidates =
    {
        "Java", "Spring Boot", "Spring", "Hibernate", "JPA",
        ".NET", "ASP.NET", "C#", "Entity Framework",
        "Node.js", "Express", "NestJS",
        "Python", "Django", "FastAPI", "Flask",
        "React", "Next.js", "Vue", "Angular", "TypeScript", "JavaScript", "HTML", "CSS", "Tailwind",
        "PostgreSQL", "MySQL", "MongoDB", "Redis", "SQL Server",
        "Docker", "Kubernetes", "AWS", "Azure", "GCP",
        "Git", "GitHub Actions", "Jenkins", "CI/CD",
        "REST API", "GraphQL", "gRPC", "Kafka", "RabbitMQ",
        "Flutter", "Dart", "Kotlin", "Swift", "Android", "iOS",
        "Linux", "Bash", "Unit Test", "JUnit", "xUnit"
    };

    private static readonly Regex YearsRegex = new(
        @"(\d+(?:[\.,]\d+)?)\s*(?:\+)?\s*(years?|năm)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<ParsedCvResult> ParseAsync(string anonymizedText, CancellationToken cancellationToken = default)
    {
        var text = anonymizedText ?? string.Empty;

        var totalYears = DetectTotalYears(text);

        var skills = SkillCandidates
            .Where(s => Mentions(text, s))
            .Take(15)
            .Select(s =>
            {
                var mentions = CountMentions(text, s);
                var level = mentions >= 3 ? "advanced"
                          : mentions == 2 ? "intermediate"
                          : "basic";
                var yearsExp = totalYears.HasValue
                    ? Math.Min(totalYears.Value, mentions * 0.5m + 0.5m)
                    : (decimal?)null;
                var evidence = ExtractEvidence(text, s);
                return new ParsedCvSkill(s, level, yearsExp, evidence);
            })
            .ToList();

        return Task.FromResult(new ParsedCvResult(totalYears, skills));
    }

    private static decimal? DetectTotalYears(string text)
    {
        var matches = YearsRegex.Matches(text);
        if (matches.Count == 0) return null;

        decimal max = 0;
        foreach (Match m in matches)
        {
            var raw = m.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var v))
            {
                if (v > max && v < 50) max = v;
            }
        }
        return max == 0 ? null : max;
    }

    private static bool Mentions(string text, string keyword)
        => text.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static int CountMentions(string text, string keyword)
    {
        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(keyword, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += keyword.Length;
        }
        return count;
    }

    private static string? ExtractEvidence(string text, string keyword)
    {
        var idx = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var start = Math.Max(0, idx - 30);
        var end = Math.Min(text.Length, idx + keyword.Length + 60);
        var snippet = text.Substring(start, end - start)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();
        return snippet.Length > 120 ? snippet[..120] + "…" : snippet;
    }
}
