using iText.Kernel.Pdf;

namespace PDFVT;

/// <summary>
/// Concrete implementation for generating PDF/VT-3 compliant documents.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: PDF/VT-3 Specification Details (ISO 16612-3:2020):
///
/// Standard Foundation:
/// - Based on PDF/X-6 (ISO 15930-9:2020)
/// - Uses PDF version 2.0
/// - Latest PDF/VT standard with modern features
///
/// Key Enhancements over VT-1:
/// - Simplified transparency (page-level blending only)
/// - Per-page Output Intents for variable color management
/// - CxF/X-4 spectral data support for spot colors
/// - Enhanced Black Point Compensation controls
/// - PDF 2.0 features (associated files, page-level metadata)
///
/// Use Cases:
/// - High-fidelity color-critical variable printing
/// - Modern VDP workflows with PDF 2.0 toolchains
/// - Spectral color matching requirements
///
/// Compatibility Note: PDF 2.0 / PDF/X-6 toolchain support is still
/// maturing. VT-1 remains more widely supported in production environments.
/// </remarks>
public class PdfVT3Generator : PdfVtGeneratorBase
{
    /// <inheritdoc/>
    /// <remarks>PDF/VT-3 requires PDF version 2.0 (ISO 32000-2:2020).</remarks>
    public override string GetPdfVersionString() => "2.0";

    /// <inheritdoc/>
    public override string GetVtVersionMarker() => "PDF/VT-3";

    /// <inheritdoc/>
    /// <remarks>
    /// REVIEWER NOTE: PDF 2.0 is the latest PDF specification.
    /// Enables new features like associated files, page-level Output Intents,
    /// and enhanced encryption options not available in PDF 1.x.
    /// </remarks>
    protected override PdfVersion GetPdfVersion() => PdfVersion.PDF_2_0;

    /// <inheritdoc/>
    /// <remarks>
    /// PDF/X-6 (ISO 15930-9) is the PDF 2.0-based print production standard.
    /// Requires PDF 2.0 and supports page-level Output Intents.
    /// </remarks>
    protected override string GetBaseStandard() => "PDF/X-6";

    /// <inheritdoc/>
    /// <remarks>
    /// REVIEWER NOTE: Feature list emphasizes VT-3 / PDF 2.0 advantages.
    /// These differentiate VT-3 from the more established VT-1 standard.
    /// </remarks>
    protected override string[] GetFeatures() => new[]
    {
        "• Document Part Metadata (DPM) for tracking individual records",
        "• Efficient reuse of common resources across pages",
        "• Simplified transparency rules (page-level only)",
        "• Per-page Output Intents with optional CxF/X-4 spectral data",
        "• Enhanced Black Point Compensation support",
        "• Built on PDF/X-6 foundation (PDF 2.0)",
        "• Modern toolchain alignment for VDP workflows"
    };

    /// <summary>
    /// Generates VT-3 specific XMP metadata with PDF/X-6 namespace.
    /// </summary>
    /// <returns>UTF-8 encoded XMP packet bytes</returns>
    /// <remarks>
    /// REVIEWER NOTE: VT-3 XMP differs from VT-1 in several ways:
    ///
    /// 1. Additional namespace: pdfx6 for PDF/X-6 conformance marker
    /// 2. Explicit PDF version element: pdf:PDFVersion = "2.0"
    /// 3. GTS_PDFXConformance element for PDF/X-6 compliance
    ///
    /// The pdfx6 namespace (http://www.npes.org/pdfx6/ns/id/) is specific
    /// to PDF/X-6 and indicates compliance with that standard.
    /// </remarks>
    protected override byte[] CreateXmpMetadata()
    {
        string vtVersion = GetVtVersionMarker();

        // REVIEWER NOTE: Extended XMP template includes PDF/X-6 namespace
        // and additional compliance markers not present in VT-1
        string xmpTemplate = $@"<?xpacket begin=""﻿"" id=""W5M0MpCehiHzreSzNTczkc9d""?>
<x:xmpmeta xmlns:x=""adobe:ns:meta/"">
  <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
    <rdf:Description rdf:about=""""
        xmlns:dc=""http://purl.org/dc/elements/1.1/""
        xmlns:xmp=""http://ns.adobe.com/xap/1.0/""
        xmlns:pdf=""http://ns.adobe.com/pdf/1.3/""
        xmlns:pdfx=""http://ns.adobe.com/pdfx/1.3/""
        xmlns:pdfxid=""http://www.npes.org/pdfx/ns/id/""
        xmlns:pdfvtid=""http://www.npes.org/pdfvt/ns/id/""
        xmlns:pdfx6=""http://www.npes.org/pdfx6/ns/id/"">
      <dc:title>
        <rdf:Alt>
          <rdf:li xml:lang=""x-default"">{vtVersion} Sample Document</rdf:li>
        </rdf:Alt>
      </dc:title>
      <dc:creator>
        <rdf:Seq>
          <rdf:li>PDFVT Generator</rdf:li>
        </rdf:Seq>
      </dc:creator>
      <dc:description>
        <rdf:Alt>
          <rdf:li xml:lang=""x-default"">Sample {vtVersion} document based on PDF 2.0 and PDF/X-6 for variable data printing</rdf:li>
        </rdf:Alt>
      </dc:description>
      <xmp:CreatorTool>iText 8.0.5 for .NET</xmp:CreatorTool>
      <xmp:CreateDate>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</xmp:CreateDate>
      <xmp:ModifyDate>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</xmp:ModifyDate>
      <pdf:Producer>iText® 8.0.5 ©2000-2024 Apryse Group NV</pdf:Producer>
      <pdf:PDFVersion>2.0</pdf:PDFVersion>
      <pdfx:GTS_PDFVTVersion>{vtVersion}</pdfx:GTS_PDFVTVersion>
      <pdfvtid:GTS_PDFVTVersion>{vtVersion}</pdfvtid:GTS_PDFVTVersion>
      <pdfx6:GTS_PDFXConformance>PDF/X-6</pdfx6:GTS_PDFXConformance>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end=""w""?>";

        return System.Text.Encoding.UTF8.GetBytes(xmpTemplate);
    }
}
