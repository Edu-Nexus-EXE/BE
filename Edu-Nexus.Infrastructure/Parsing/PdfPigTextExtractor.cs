using Edu_Nexus.Application.Interfaces.Parsing;
using UglyToad.PdfPig;

namespace Edu_Nexus.Infrastructure.Parsing;

public class PdfPigTextExtractor : IPdfTextExtractor
{
    public string Extract(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var sb = new System.Text.StringBuilder();
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }
}
