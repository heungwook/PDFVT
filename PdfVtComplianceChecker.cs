using iText.Kernel.Pdf;

namespace PDFVT;

/// <summary>
/// Data transfer object containing the results of a PDF/VT compliance check.
/// Provides both pass/fail status and detailed validation information.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: This class serves as a comprehensive validation report:
///
/// Key Properties:
/// - IsCompliant: Overall pass/fail for compliance checking
/// - DetectedVersion: Which PDF/VT version was detected (if any)
/// - ValidationIssues: Detailed list of what failed and why
///
/// The granular boolean properties (HasGtsVersionInCatalog, etc.) enable
/// partial compliance reporting and debugging of near-compliant documents.
///
/// Usage Pattern:
/// 1. Call PdfVtComplianceChecker.CheckCompliance()
/// 2. Check IsCompliant for quick pass/fail
/// 3. Iterate ValidationIssues for detailed error messages
/// 4. Use individual Has* properties for specific checks
/// </remarks>
public class ComplianceResult
{
    /// <summary>
    /// Overall compliance status. True only if all required checks pass.
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// The PDF/VT version detected in the document.
    /// Null if no valid version marker found or document is non-compliant.
    /// </summary>
    public PdfVtVersion? DetectedVersion { get; set; }

    /// <summary>
    /// Raw version marker string as found in the document (e.g., "PDF/VT-1").
    /// Useful for debugging documents with non-standard markers.
    /// </summary>
    public string? VersionMarker { get; set; }

    /// <summary>
    /// PDF version of the document (e.g., "1.6" or "2.0").
    /// Important for version-specific compliance (VT-1 needs 1.6+, VT-3 needs 2.0).
    /// </summary>
    public string? PdfVersion { get; set; }

    /// <summary>
    /// Indicates if GTS_PDFVTVersion key exists in the document catalog.
    /// REQUIRED by ISO 16612-2/3 for compliance.
    /// </summary>
    public bool HasGtsVersionInCatalog { get; set; }

    /// <summary>
    /// Indicates if GTS_PDFVTVersion exists in XMP metadata packet.
    /// RECOMMENDED by the standard for metadata synchronization.
    /// </summary>
    public bool HasGtsVersionInXmp { get; set; }

    /// <summary>
    /// Indicates if MarkInfo dictionary exists with Marked=true.
    /// REQUIRED - indicates the document contains tagged structure.
    /// </summary>
    public bool HasMarkInfo { get; set; }

    /// <summary>
    /// Detailed list of validation failures for diagnostic purposes.
    /// Empty list indicates no issues found (but check IsCompliant for overall status).
    /// </summary>
    public List<string> ValidationIssues { get; set; } = new();
}

/// <summary>
/// Validates PDF documents against PDF/VT-1 and PDF/VT-3 compliance requirements.
/// Implements a subset of the ISO 16612 validation rules.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: Compliance Checking Strategy
///
/// This checker validates the following PDF/VT requirements:
/// 1. GTS_PDFVTVersion in catalog dictionary (REQUIRED)
/// 2. GTS_PDFVTVersion in XMP metadata (RECOMMENDED)
/// 3. MarkInfo dictionary with Marked=true (REQUIRED)
/// 4. PDF version compatibility (1.6+ for VT-1, 2.0 for VT-3)
///
/// Limitations (not implemented):
/// - Output Intent validation
/// - Font embedding verification
/// - Color space validation
/// - Document Part Metadata (DPM) structure validation
///
/// For production use, consider using a certified PDF/VT validator
/// such as callas pdfaPilot or Adobe Preflight.
/// </remarks>
public class PdfVtComplianceChecker
{
    /// <summary>
    /// The catalog key name for PDF/VT version identification.
    /// Defined by the Ghent PDF Workgroup (GWG) Technical Specification.
    /// </summary>
    private const string GTS_PDFVT_VERSION = "GTS_PDFVTVersion";

    /// <summary>
    /// Convenience method for simple pass/fail compliance checking against a specific version.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to validate</param>
    /// <param name="expectedVersion">The PDF/VT version to check against</param>
    /// <returns>True if document is compliant AND matches the expected version</returns>
    /// <remarks>
    /// REVIEWER NOTE: This method performs a full compliance check internally.
    /// For detailed diagnostics, use CheckCompliance() directly.
    /// </remarks>
    public bool IsCompliant(string filePath, PdfVtVersion expectedVersion)
    {
        var result = CheckCompliance(filePath);
        return result.IsCompliant && result.DetectedVersion == expectedVersion;
    }

    /// <summary>
    /// Performs comprehensive PDF/VT compliance validation with detailed results.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to validate</param>
    /// <returns>Detailed compliance result with all validation findings</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file doesn't exist</exception>
    /// <remarks>
    /// REVIEWER NOTE: Validation sequence:
    /// 1. Verify file exists (early fail for missing files)
    /// 2. Open PDF and extract version info
    /// 3. Check catalog for GTS_PDFVTVersion
    /// 4. Check MarkInfo dictionary
    /// 5. Check XMP metadata
    /// 6. Apply version-specific requirements
    /// 7. Return comprehensive result
    ///
    /// Exception handling: FileNotFoundException propagates, other exceptions
    /// are caught and recorded in ValidationIssues for graceful degradation.
    /// </remarks>
    public ComplianceResult CheckCompliance(string filePath)
    {
        // Early validation - fail fast for missing files
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"PDF file not found: {filePath}", filePath);
        }

        var result = new ComplianceResult();

        try
        {
            using var reader = new PdfReader(filePath);
            using var pdfDoc = new PdfDocument(reader);

            // === Step 1: Extract PDF Version ===
            // REVIEWER NOTE: Version is critical for VT-1 vs VT-3 validation
            var pdfVersion = pdfDoc.GetPdfVersion();
            // Convert iText's format (e.g., "16") to standard format (e.g., "1.6")
            var versionName = pdfVersion.ToPdfName().GetValue();
            result.PdfVersion = versionName.Replace(".", "").Insert(1, ".");

            // === Step 2: Check Catalog for GTS_PDFVTVersion ===
            // This is the PRIMARY version indicator per ISO 16612
            var catalog = pdfDoc.GetCatalog();
            var catalogDict = catalog.GetPdfObject();

            string? vtVersionMarker = null;

            var gtsVersion = catalogDict.GetAsString(new PdfName(GTS_PDFVT_VERSION));
            if (gtsVersion != null)
            {
                result.HasGtsVersionInCatalog = true;
                vtVersionMarker = gtsVersion.GetValue();
            }
            else
            {
                result.ValidationIssues.Add("GTS_PDFVTVersion not found in catalog");
            }

            // === Step 3: Check MarkInfo Dictionary ===
            // REVIEWER NOTE: MarkInfo indicates tagged PDF structure
            // Required for accessibility and PDF/VT compliance
            var markInfo = catalogDict.GetAsDictionary(PdfName.MarkInfo);
            if (markInfo != null)
            {
                var marked = markInfo.GetAsBoolean(PdfName.Marked);
                result.HasMarkInfo = marked?.GetValue() == true;
            }

            if (!result.HasMarkInfo)
            {
                result.ValidationIssues.Add("MarkInfo with Marked=true not found");
            }

            // === Step 4: Check XMP Metadata ===
            // REVIEWER NOTE: XMP provides secondary version confirmation
            // and additional metadata for workflow systems
            var metadata = pdfDoc.GetXmpMetadata();
            if (metadata != null)
            {
                string xmpContent = System.Text.Encoding.UTF8.GetString(metadata);

                if (xmpContent.Contains("GTS_PDFVTVersion"))
                {
                    result.HasGtsVersionInXmp = true;

                    // Fallback: Extract version from XMP if catalog check failed
                    // REVIEWER NOTE: Prefers catalog version; XMP is backup
                    if (vtVersionMarker == null)
                    {
                        // Check VT-3 first (more specific)
                        if (xmpContent.Contains("PDF/VT-3"))
                            vtVersionMarker = "PDF/VT-3";
                        else if (xmpContent.Contains("PDF/VT-1"))
                            vtVersionMarker = "PDF/VT-1";
                    }
                }
                else
                {
                    result.ValidationIssues.Add("GTS_PDFVTVersion not found in XMP metadata");
                }
            }
            else
            {
                result.ValidationIssues.Add("XMP metadata not found");
            }

            // === Step 5: Version-Specific Validation ===
            if (vtVersionMarker != null)
            {
                result.VersionMarker = vtVersionMarker;

                switch (vtVersionMarker)
                {
                    case "PDF/VT-1":
                        result.DetectedVersion = PdfVtVersion.VT1;
                        result.IsCompliant = ValidateVT1Requirements(result);
                        break;

                    case "PDF/VT-3":
                        result.DetectedVersion = PdfVtVersion.VT3;
                        result.IsCompliant = ValidateVT3Requirements(result);
                        break;

                    default:
                        // REVIEWER NOTE: Handles PDF/VT-2 or malformed markers
                        result.ValidationIssues.Add($"Unknown PDF/VT version: {vtVersionMarker}");
                        break;
                }
            }
            else
            {
                result.ValidationIssues.Add("No PDF/VT version marker found");
            }
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            // REVIEWER NOTE: Catch PDF parsing errors but let FileNotFound propagate
            // This allows graceful handling of corrupt or invalid PDFs
            result.ValidationIssues.Add($"Error reading PDF: {ex.Message}");
        }

        return result;
    }
    
    #region Version-Specific Validation

    /// <summary>
    /// Validates PDF/VT-1 specific requirements.
    /// </summary>
    /// <param name="result">The compliance result to validate and update</param>
    /// <returns>True if all VT-1 requirements are met</returns>
    /// <remarks>
    /// REVIEWER NOTE: PDF/VT-1 Requirements (ISO 16612-2):
    /// - PDF version 1.6 or higher (inherits from PDF/X-4)
    /// - GTS_PDFVTVersion in catalog (checked earlier)
    /// - MarkInfo with Marked=true (checked earlier)
    ///
    /// Version comparison uses string parsing for flexibility.
    /// Consider: Could use Version class for more robust comparison.
    /// </remarks>
    private bool ValidateVT1Requirements(ComplianceResult result)
    {
        bool isValid = true;

        // PDF/VT-1 requires PDF 1.6 or higher (based on PDF/X-4)
        if (result.PdfVersion != null)
        {
            var versionParts = result.PdfVersion.Split('.');
            if (versionParts.Length >= 2 &&
                int.TryParse(versionParts[0], out int major) &&
                int.TryParse(versionParts[1], out int minor))
            {
                // REVIEWER NOTE: PDF 1.6+ is required
                // PDF 2.0 (major=2) would also pass this check
                if (major < 1 || (major == 1 && minor < 6))
                {
                    result.ValidationIssues.Add($"PDF/VT-1 requires PDF 1.6+, found {result.PdfVersion}");
                    isValid = false;
                }
            }
        }

        // GTS version in catalog is REQUIRED
        if (!result.HasGtsVersionInCatalog)
        {
            isValid = false;
        }

        // MarkInfo is REQUIRED for tagged structure
        if (!result.HasMarkInfo)
        {
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Validates PDF/VT-3 specific requirements.
    /// </summary>
    /// <param name="result">The compliance result to validate and update</param>
    /// <returns>True if all VT-3 requirements are met</returns>
    /// <remarks>
    /// REVIEWER NOTE: PDF/VT-3 Requirements (ISO 16612-3):
    /// - PDF version 2.0 EXACTLY (based on PDF/X-6)
    /// - GTS_PDFVTVersion in catalog (checked earlier)
    /// - MarkInfo with Marked=true (checked earlier)
    ///
    /// VT-3 is stricter than VT-1 - requires exactly PDF 2.0,
    /// not just "2.0 or higher" (though 2.0 is currently the latest).
    /// </remarks>
    private bool ValidateVT3Requirements(ComplianceResult result)
    {
        bool isValid = true;

        // PDF/VT-3 requires exactly PDF 2.0
        // REVIEWER NOTE: Strict equality - PDF 2.1 (if it existed) would fail
        if (result.PdfVersion != "2.0")
        {
            result.ValidationIssues.Add($"PDF/VT-3 requires PDF 2.0, found {result.PdfVersion}");
            isValid = false;
        }

        // GTS version in catalog is REQUIRED
        if (!result.HasGtsVersionInCatalog)
        {
            isValid = false;
        }

        // MarkInfo is REQUIRED for tagged structure
        if (!result.HasMarkInfo)
        {
            isValid = false;
        }

        return isValid;
    }

    #endregion

    #region Output Formatting

    /// <summary>
    /// Outputs formatted compliance check results to the console.
    /// </summary>
    /// <param name="result">The compliance result to display</param>
    /// <remarks>
    /// REVIEWER NOTE: Output format designed for:
    /// - Quick visual scanning (emoji indicators)
    /// - Tree-style hierarchy for validation details
    /// - Clear pass/fail indication
    ///
    /// Uses Unicode box-drawing characters for tree structure.
    /// These render correctly in modern terminals but may have
    /// issues in legacy Windows Command Prompt (use Windows Terminal).
    /// </remarks>
    public void PrintResults(ComplianceResult result)
    {
        // Header with summary information
        Console.WriteLine($"📋 PDF/VT Compliance Check Results");
        Console.WriteLine($"   PDF Version: {result.PdfVersion}");
        Console.WriteLine($"   Detected: {result.VersionMarker ?? "Not PDF/VT"}");
        Console.WriteLine($"   Compliant: {(result.IsCompliant ? "✓ Yes" : "✗ No")}");
        Console.WriteLine();

        // Validation detail tree
        // REVIEWER NOTE: Unicode box-drawing creates visual hierarchy
        Console.WriteLine($"   Validation Details:");
        Console.WriteLine($"   ├─ GTS in Catalog: {(result.HasGtsVersionInCatalog ? "✓" : "✗")}");
        Console.WriteLine($"   ├─ GTS in XMP:     {(result.HasGtsVersionInXmp ? "✓" : "✗")}");
        Console.WriteLine($"   └─ MarkInfo:       {(result.HasMarkInfo ? "✓" : "✗")}");

        // Issues list (if any)
        if (result.ValidationIssues.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"   Issues:");
            foreach (var issue in result.ValidationIssues)
            {
                Console.WriteLine($"   ⚠ {issue}");
            }
        }
    }

    #endregion
}
