namespace PDFVT;

/// <summary>
/// PDF/VT version enumeration
/// </summary>
public enum PdfVtVersion
{
    /// <summary>PDF/VT-1 based on PDF 1.6 and PDF/X-4</summary>
    VT1,
    
    /// <summary>PDF/VT-3 based on PDF 2.0 and PDF/X-6</summary>
    VT3
}

/// <summary>
/// Command-line options for PDF/VT generation
/// </summary>
public class CommandLineOptions
{
    public PdfVtVersion Version { get; set; } = PdfVtVersion.VT1;
    public string OutputPath { get; set; } = "output.pdf";
}

/// <summary>
/// Parser for command-line arguments
/// </summary>
public static class CommandLineParser
{
    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--version":
                case "-v":
                    if (i + 1 < args.Length)
                    {
                        options.Version = ParseVersion(args[++i]);
                    }
                    break;
                    
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputPath = args[++i];
                    }
                    break;
            }
        }
        
        return options;
    }
    
    private static PdfVtVersion ParseVersion(string value)
    {
        return value.ToLower() switch
        {
            "vt1" or "1" => PdfVtVersion.VT1,
            "vt3" or "3" => PdfVtVersion.VT3,
            _ => throw new ArgumentException($"Invalid PDF/VT version: '{value}'. Use 'vt1' or 'vt3'.")
        };
    }
    
    public static void PrintHelp()
    {
        Console.WriteLine("PDF/VT Document Generator");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --version, -v <vt1|vt3>  PDF/VT version (default: vt1)");
        Console.WriteLine("  --output, -o <path>      Output file path (default: output.pdf)");
        Console.WriteLine("  --help, -h               Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- --version vt1");
        Console.WriteLine("  dotnet run -- -v vt3 -o my_document.pdf");
    }
}
