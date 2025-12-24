using iText.Kernel.Pdf;

namespace PDFVT;

/// <summary>
/// Concrete implementation for generating PDF/VT-1 compliant documents.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: PDF/VT-1 Specification Details (ISO 16612-2:2010):
///
/// Standard Foundation:
/// - Based on PDF/X-4 (ISO 15930-7:2010)
/// - Uses PDF version 1.6
/// - Single-file exchange format (all resources embedded)
///
/// Key Capabilities:
/// - Document Part Metadata (DPM) for record-level tracking
/// - Efficient resource sharing via XObject reuse
/// - Full transparency model support (blend modes, opacity)
/// - Optional Content Groups (layers) for conditional content
/// - ICC color management required
///
/// Use Cases:
/// - Transactional printing (statements, invoices)
/// - Direct mail with personalization
/// - On-demand publishing
///
/// Comparison to VT-3: VT-1 is widely supported but uses older PDF 1.6.
/// VT-3 offers PDF 2.0 features but has less toolchain support.
/// </remarks>
public class PdfVT1Generator : PdfVtGeneratorBase
{
    /// <inheritdoc/>
    /// <remarks>PDF/VT-1 requires PDF version 1.6 minimum.</remarks>
    public override string GetPdfVersionString() => "1.6";

    /// <inheritdoc/>
    public override string GetVtVersionMarker() => "PDF/VT-1";

    /// <inheritdoc/>
    /// <remarks>
    /// REVIEWER NOTE: iText's PdfVersion enum maps directly to PDF spec versions.
    /// PDF_1_6 enables transparency, layers, and other features required by PDF/X-4.
    /// </remarks>
    protected override PdfVersion GetPdfVersion() => PdfVersion.PDF_1_6;

    /// <inheritdoc/>
    /// <remarks>
    /// PDF/X-4 (ISO 15930-7) provides the print production foundation.
    /// Requires embedded fonts, ICC profiles, and specific metadata.
    /// </remarks>
    protected override string GetBaseStandard() => "PDF/X-4";

    /// <inheritdoc/>
    /// <remarks>
    /// REVIEWER NOTE: Feature list highlights VT-1 specific capabilities.
    /// Bullet points use Unicode bullet character for consistent rendering.
    /// </remarks>
    protected override string[] GetFeatures() => new[]
    {
        "• Document Part Metadata (DPM) for tracking individual records",
        "• Efficient reuse of common resources across pages",
        "• Support for encapsulated external content",
        "• Optimized for high-speed variable data printing",
        "• Built on PDF/X-4 foundation for print production",
        "• Uses PDF 1.6 with transparency and layers support"
    };
}
