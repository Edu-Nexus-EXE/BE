using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Edu_Nexus.Application.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("N")[..8];

        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var slug = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        
        // Replace special Vietnamese characters (like đ -> d)
        slug = slug.Replace('đ', 'd');

        // Replace invalid characters with space
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        
        // Replace multiple spaces or hyphens with a single hyphen
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');

        if (string.IsNullOrEmpty(slug)) return Guid.NewGuid().ToString("N")[..8];

        // Ensure it's between 3 and 50 characters (if it's too short, it doesn't matter much for regex, but let's pad it if < 3)
        if (slug.Length < 3)
        {
            slug = slug.PadRight(3, 'a'); // just pad with 'a' or append random
        }
        else if (slug.Length > 40)
        {
            slug = slug[..40].Trim('-');
        }

        return slug;
    }
}
