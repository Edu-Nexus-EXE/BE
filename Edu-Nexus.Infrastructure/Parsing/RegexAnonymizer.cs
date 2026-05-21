using System.Text.RegularExpressions;
using Edu_Nexus.Application.Interfaces.Parsing;

namespace Edu_Nexus.Infrastructure.Parsing;

public class RegexAnonymizer : IAnonymizer
{
    private static readonly Regex EmailRegex = new(
        @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex VietnamesePhoneRegex = new(
        @"(?:\+?84|0)\d{9,10}",
        RegexOptions.Compiled);

    private static readonly Regex UrlRegex = new(
        @"https?://[^\s]+",
        RegexOptions.Compiled);

    public string Mask(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var result = EmailRegex.Replace(text, "[EMAIL]");
        result = VietnamesePhoneRegex.Replace(result, "[PHONE]");
        result = UrlRegex.Replace(result, "[URL]");
        return result;
    }
}
