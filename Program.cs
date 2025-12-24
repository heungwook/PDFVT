namespace PDFVT;

class Program
{
    static void Main(string[] args)
    {
        // Handle help request
        if (args.Any(a => a == "--help" || a == "-h"))
        {
            CommandLineParser.PrintHelp();
            return;
        }
        
        try
        {
            // Parse command-line arguments
            var options = CommandLineParser.Parse(args);
            
            Console.WriteLine($"🔮 PDF/VT Document Generator");
            Console.WriteLine($"   Version: {options.Version}");
            Console.WriteLine($"   Output: {options.OutputPath}");
            Console.WriteLine();
            
            // Create appropriate generator based on version
            PdfVtGeneratorBase generator = options.Version switch
            {
                PdfVtVersion.VT1 => new PdfVT1Generator(),
                PdfVtVersion.VT3 => new PdfVT3Generator(),
                _ => throw new ArgumentException($"Unknown version: {options.Version}")
            };
            
            // Generate document
            Console.WriteLine($"Creating {generator.GetVtVersionMarker()} document...");
            Console.WriteLine($"  - PDF Version: {generator.GetPdfVersionString()}");
            
            generator.CreateDocument(options.OutputPath);
            
            Console.WriteLine();
            Console.WriteLine($"✓ {generator.GetVtVersionMarker()} document created successfully: {options.OutputPath}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            CommandLineParser.PrintHelp();
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating PDF: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
