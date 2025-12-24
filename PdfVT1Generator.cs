using iText.Kernel.Pdf;

namespace PDFVT;

/// <summary>
/// PDF/VT-1 document generator
/// Based on PDF 1.6 and PDF/X-4
/// </summary>
public class PdfVT1Generator : PdfVtGeneratorBase
{
    public override string GetPdfVersionString() => "1.6";
    
    public override string GetVtVersionMarker() => "PDF/VT-1";
    
    protected override PdfVersion GetPdfVersion() => PdfVersion.PDF_1_6;
    
    protected override string GetBaseStandard() => "PDF/X-4";
    
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
