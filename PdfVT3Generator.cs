using iText.Kernel.Pdf;

namespace PDFVT;

/// <summary>
/// PDF/VT-3 document generator
/// Based on PDF 2.0 and PDF/X-6 (ISO 16612-3:2020)
/// </summary>
public class PdfVT3Generator : PdfVtGeneratorBase
{
    public override string GetPdfVersionString() => "2.0";
    
    public override string GetVtVersionMarker() => "PDF/VT-3";
    
    protected override PdfVersion GetPdfVersion() => PdfVersion.PDF_2_0;
    
    protected override string GetBaseStandard() => "PDF/X-6";
    
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
    
    protected override byte[] CreateXmpMetadata()
    {
        string vtVersion = GetVtVersionMarker();
        // PDF/VT-3 uses updated namespace references for PDF 2.0
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
