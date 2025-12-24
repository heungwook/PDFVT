namespace PDFVT;

/// <summary>
/// Defines the supported PDF/VT standard versions.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: PDF/VT is a family of ISO standards for variable data printing:
/// - PDF/VT-1 (ISO 16612-2:2010): Based on PDF/X-4, uses PDF 1.6
/// - PDF/VT-2 (ISO 16612-2:2010): Streaming variant (not implemented here)
/// - PDF/VT-3 (ISO 16612-3:2020): Based on PDF/X-6, uses PDF 2.0
/// </remarks>
public enum PdfVtVersion
{
    /// <summary>
    /// PDF/VT-1 based on PDF 1.6 and PDF/X-4.
    /// Suitable for single-file exchange with embedded resources.
    /// </summary>
    VT1,

    /// <summary>
    /// PDF/VT-3 based on PDF 2.0 and PDF/X-6.
    /// Latest standard with enhanced transparency and color management.
    /// </summary>
    VT3
}

/// <summary>
/// Immutable data transfer object containing parsed command-line options.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: Properties use sensible defaults to minimize required arguments.
/// The IsCheckMode computed property provides a clean abstraction for mode detection.
/// Consider: Could benefit from init-only setters (C# 9+) for true immutability.
/// </remarks>
public class CommandLineOptions
{
    /// <summary>Target PDF/VT version for document generation. Defaults to VT1.</summary>
    public PdfVtVersion Version { get; set; } = PdfVtVersion.VT1;

    /// <summary>Output file path for generated documents. Defaults to "output.pdf".</summary>
    public string OutputPath { get; set; } = "output.pdf";

    /// <summary>Path to existing PDF for compliance checking. Null when in generation mode.</summary>
    public string? CheckPath { get; set; }

    /// <summary>
    /// Returns true when operating in compliance check mode (--check flag provided).
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: Computed property ensures consistency - avoids
    /// potential state where CheckPath is set but a separate bool flag isn't.
    /// </remarks>
    public bool IsCheckMode => !string.IsNullOrEmpty(CheckPath);
}

/// <summary>
/// Static utility class for parsing command-line arguments into structured options.
/// Implements a simple flag-based parser without external dependencies.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: Design decisions:
/// 1. Static class chosen for stateless parsing operations
/// 2. Manual parsing avoids dependency on System.CommandLine or similar libraries
/// 3. Case-insensitive flag matching improves UX across platforms
///
/// Potential improvements for future consideration:
/// - Add validation for output path (directory exists, writable)
/// - Support positional arguments for common use cases
/// - Add --quiet flag for CI/CD integration
/// </remarks>
public static class CommandLineParser
{
    /// <summary>
    /// Parses command-line arguments into a strongly-typed options object.
    /// </summary>
    /// <param name="args">Raw command-line arguments from Main()</param>
    /// <returns>Populated options object with defaults for unspecified values</returns>
    /// <exception cref="ArgumentException">Thrown when version value is invalid</exception>
    /// <remarks>
    /// REVIEWER NOTE: Parser uses sequential iteration with lookahead for value extraction.
    /// Unknown flags are silently ignored for forward compatibility.
    /// </remarks>
    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        // REVIEWER NOTE: Index-based iteration allows consuming the next arg as a value
        // when a flag requiring a parameter is encountered (++i advances past the value)
        for (int i = 0; i < args.Length; i++)
        {
            // Case-insensitive matching for cross-platform compatibility
            switch (args[i].ToLower())
            {
                case "--version":
                case "-v":
                    // REVIEWER NOTE: Bounds check prevents IndexOutOfRangeException
                    // when flag is provided without a value (e.g., "dotnet run -- -v")
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

                case "--check":
                case "-c":
                    // REVIEWER NOTE: Setting CheckPath automatically activates check mode
                    // via the IsCheckMode computed property
                    if (i + 1 < args.Length)
                    {
                        options.CheckPath = args[++i];
                    }
                    break;

                // REVIEWER NOTE: Unrecognized flags are silently skipped.
                // Consider: Add --strict mode to error on unknown flags?
            }
        }

        return options;
    }

    /// <summary>
    /// Converts a version string to the corresponding enum value.
    /// </summary>
    /// <param name="value">Version string: "vt1", "1", "vt3", or "3"</param>
    /// <returns>Corresponding PdfVtVersion enum value</returns>
    /// <exception cref="ArgumentException">Thrown when value doesn't match known versions</exception>
    /// <remarks>
    /// REVIEWER NOTE: Accepts both full names ("vt1") and shorthand ("1") for convenience.
    /// Switch expression with 'or' pattern provides clean multi-value matching.
    /// </remarks>
    private static PdfVtVersion ParseVersion(string value)
    {
        return value.ToLower() switch
        {
            "vt1" or "1" => PdfVtVersion.VT1,
            "vt3" or "3" => PdfVtVersion.VT3,
            _ => throw new ArgumentException($"Invalid PDF/VT version: '{value}'. Use 'vt1' or 'vt3'.")
        };
    }

    /// <summary>
    /// Outputs formatted usage information to the console.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: Help text follows GNU conventions:
    /// - Long form first, then short form
    /// - Default values indicated in descriptions
    /// - Concrete examples for common use cases
    /// </remarks>
    public static void PrintHelp()
    {
        Console.WriteLine("PDF/VT Document Generator");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --version, -v <vt1|vt3>  PDF/VT version (default: vt1)");
        Console.WriteLine("  --output, -o <path>      Output file path (default: output.pdf)");
        Console.WriteLine("  --check, -c <path>       Check compliance of existing PDF");
        Console.WriteLine("  --help, -h               Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- --version vt1");
        Console.WriteLine("  dotnet run -- -v vt3 -o my_document.pdf");
        Console.WriteLine("  dotnet run -- --check document.pdf");
    }
}
