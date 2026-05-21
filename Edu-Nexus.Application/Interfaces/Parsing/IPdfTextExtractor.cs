namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IPdfTextExtractor
{
    string Extract(Stream pdfStream);
}
