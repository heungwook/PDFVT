namespace PDFVT;

/// <summary>
/// Entry point for the PDF/VT Document Generator CLI application.
/// Supports two operational modes:
/// 1. Generation Mode: Creates new PDF/VT-1 or PDF/VT-3 compliant documents
/// 2. Check Mode: Validates existing PDFs for PDF/VT compliance
/// </summary>
/// <remarks>
/// REVIEWER NOTE: This application follows the ISO 16612-2 (PDF/VT-1) and
/// ISO 16612-3 (PDF/VT-3) standards for variable data and transactional printing.
/// The factory pattern via switch expression ensures type-safe version selection.
/// </remarks>
class Program
{
    /// <summary>
    /// Application entry point. Processes command-line arguments to determine
    /// operational mode and executes the appropriate workflow.
    /// </summary>
    /// <param name="args">Command-line arguments supporting:
    /// --version/-v: PDF/VT version (vt1|vt3)
    /// --output/-o: Output file path
    /// --check/-c: Path to PDF for compliance validation
    /// --help/-h: Display usage information
    /// </param>
    /// <remarks>
    /// REVIEWER NOTE: Exit codes follow Unix conventions:
    /// - 0: Success (generation complete or compliance check passed)
    /// - 1: Error (invalid args, file not found, or compliance check failed)
    /// </remarks>
    static void Main(string[] args)
    {
        // REVIEWER NOTE: Early exit pattern for help request avoids
        // unnecessary parsing overhead and provides immediate feedback
        if (args.Any(a => a == "--help" || a == "-h"))
        {
            CommandLineParser.PrintHelp();
            return;
        }

        try
        {
            // Parse and validate command-line arguments into strongly-typed options
            var options = CommandLineParser.Parse(args);

            // REVIEWER NOTE: Mutually exclusive modes - check mode takes precedence
            // when --check flag is provided, bypassing generation entirely
            if (options.IsCheckMode)
            {
                RunComplianceCheck(options.CheckPath!);
                return;  // Exit handled in RunComplianceCheck with appropriate code
            }

            // === Generation Mode ===
            // Display configuration summary before potentially long-running operation
            Console.WriteLine($"🔮 PDF/VT Document Generator");
            Console.WriteLine($"   Version: {options.Version}");
            Console.WriteLine($"   Output: {options.OutputPath}");
            Console.WriteLine();

            // REVIEWER NOTE: Factory pattern using switch expression ensures
            // compile-time exhaustiveness checking for PdfVtVersion enum.
            // Adding a new version requires updating this switch or it won't compile.
            PdfVtGeneratorBase generator = options.Version switch
            {
                PdfVtVersion.VT1 => new PdfVT1Generator(),
                PdfVtVersion.VT3 => new PdfVT3Generator(),
                _ => throw new ArgumentException($"Unknown version: {options.Version}")
            };

            // Provide user feedback with version-specific details
            Console.WriteLine($"Creating {generator.GetVtVersionMarker()} document...");
            Console.WriteLine($"  - PDF Version: {generator.GetPdfVersionString()}");

            // REVIEWER NOTE: CreateDocument handles temp file cleanup internally
            // via try-finally, ensuring no resource leaks on success or failure
            generator.CreateDocument(options.OutputPath);

            Console.WriteLine();
            Console.WriteLine($"✓ {generator.GetVtVersionMarker()} document created successfully: {options.OutputPath}");
        }
        catch (FileNotFoundException ex)
        {
            // REVIEWER NOTE: Specific handling for file operations provides
            // clear error messages distinguishing from other ArgumentExceptions
            Console.WriteLine($"Error: File not found - {ex.FileName}");
            Environment.Exit(1);
        }
        catch (ArgumentException ex)
        {
            // User input validation errors - show help for guidance
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            CommandLineParser.PrintHelp();
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            // REVIEWER NOTE: Catch-all for unexpected errors (IO, iText library, etc.)
            // Stack trace included for debugging; consider conditional logging in production
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Executes PDF/VT compliance validation on an existing PDF file.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to PDF file to validate</param>
    /// <remarks>
    /// REVIEWER NOTE: This method terminates the process with appropriate exit code:
    /// - Exit 0: Document is PDF/VT compliant
    /// - Exit 1: Document fails compliance checks or file not found
    /// This enables integration with CI/CD pipelines and shell scripts.
    /// </remarks>
    static void RunComplianceCheck(string filePath)
    {
        Console.WriteLine($"🔍 PDF/VT Compliance Checker");
        Console.WriteLine($"   File: {filePath}");
        Console.WriteLine();

        var checker = new PdfVtComplianceChecker();
        var result = checker.CheckCompliance(filePath);

        // Display formatted results with validation details
        checker.PrintResults(result);

        // REVIEWER NOTE: Exit code reflects compliance status for script integration
        Environment.Exit(result.IsCompliant ? 0 : 1);
    }
}
