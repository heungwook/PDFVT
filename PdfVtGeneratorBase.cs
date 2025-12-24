using SkiaSharp;
using iText.Kernel.Pdf;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.XMP;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;

namespace PDFVT;

/// <summary>
/// Base class for PDF/VT document generators
/// </summary>
public abstract class PdfVtGeneratorBase
{
    /// <summary>
    /// Gets the PDF version string (e.g., "1.6" or "2.0")
    /// </summary>
    public abstract string GetPdfVersionString();
    
    /// <summary>
    /// Gets the PDF/VT version marker (e.g., "PDF/VT-1" or "PDF/VT-3")
    /// </summary>
    public abstract string GetVtVersionMarker();
    
    /// <summary>
    /// Gets the iText PdfVersion object
    /// </summary>
    protected abstract PdfVersion GetPdfVersion();
    
    /// <summary>
    /// Gets the base standard name (e.g., "PDF/X-4" or "PDF/X-6")
    /// </summary>
    protected abstract string GetBaseStandard();
    
    /// <summary>
    /// Creates a PDF/VT document at the specified path
    /// </summary>
    public void CreateDocument(string outputPath)
    {
        string sampleImagePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sample_{Guid.NewGuid()}.png");
        
        try
        {
            CreateSampleImage(sampleImagePath);
            CreatePdfVtDocument(outputPath, sampleImagePath);
        }
        finally
        {
            if (File.Exists(sampleImagePath))
            {
                File.Delete(sampleImagePath);
            }
        }
    }
    
    protected void CreateSampleImage(string imagePath)
    {
        int width = 400;
        int height = 300;
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        canvas.Clear(SKColors.White);
        
        using var bluePaint = new SKPaint { Color = new SKColor(66, 133, 244), IsAntialias = true };
        using var orangePaint = new SKPaint { Color = new SKColor(251, 188, 4), IsAntialias = true };
        using var greenPaint = new SKPaint { Color = new SKColor(52, 168, 83), IsAntialias = true };
        using var redPaint = new SKPaint { Color = new SKColor(234, 67, 53), IsAntialias = true };
        
        canvas.DrawCircle(125, 125, 75, bluePaint);
        canvas.DrawCircle(190, 150, 70, orangePaint);
        canvas.DrawCircle(265, 165, 65, greenPaint);
        
        canvas.DrawRect(280, 40, 80, 80, redPaint);
        canvas.DrawRect(100, 200, 200, 60, bluePaint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(imagePath);
        data.SaveTo(stream);
    }
    
    protected void CreatePdfVtDocument(string outputPath, string imagePath)
    {
        var writerProperties = new WriterProperties()
            .SetPdfVersion(GetPdfVersion())
            .AddXmpMetadata();
        
        using var writer = new PdfWriter(outputPath, writerProperties);
        using var pdfDoc = new PdfDocument(writer);
        
        SetPdfVtMetadata(pdfDoc);
        
        using var document = new Document(pdfDoc, PageSize.A4);
        document.SetMargins(50, 50, 50, 50);
        
        AddTextContent(document);
        AddImageContent(document, imagePath);
        AddFooterContent(document);
        
        document.Close();
    }
    
    protected void SetPdfVtMetadata(PdfDocument pdfDoc)
    {
        var info = pdfDoc.GetDocumentInfo();
        info.SetTitle($"{GetVtVersionMarker()} Sample Document");
        info.SetAuthor("PDFVT Generator");
        info.SetSubject($"Sample {GetVtVersionMarker()} document with text and image");
        info.SetKeywords($"{GetVtVersionMarker()}, Variable Data, Transactional Printing");
        info.SetCreator("iText 8.0.5 for .NET");
        
        byte[] xmpBytes = CreateXmpMetadata();
        var xmpMeta = XMPMetaFactory.ParseFromBuffer(xmpBytes);
        pdfDoc.SetXmpMetadata(xmpMeta);
        
        var catalog = pdfDoc.GetCatalog();
        catalog.Put(new PdfName("GTS_PDFVTVersion"), new PdfString(GetVtVersionMarker()));
        
        var markInfo = new PdfDictionary();
        markInfo.Put(PdfName.Marked, PdfBoolean.TRUE);
        catalog.Put(PdfName.MarkInfo, markInfo);
    }
    
    protected virtual byte[] CreateXmpMetadata()
    {
        string vtVersion = GetVtVersionMarker();
        string xmpTemplate = $@"<?xpacket begin=""﻿"" id=""W5M0MpCehiHzreSzNTczkc9d""?>
<x:xmpmeta xmlns:x=""adobe:ns:meta/"">
  <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
    <rdf:Description rdf:about=""""
        xmlns:dc=""http://purl.org/dc/elements/1.1/""
        xmlns:xmp=""http://ns.adobe.com/xap/1.0/""
        xmlns:pdf=""http://ns.adobe.com/pdf/1.3/""
        xmlns:pdfx=""http://ns.adobe.com/pdfx/1.3/""
        xmlns:pdfxid=""http://www.npes.org/pdfx/ns/id/""
        xmlns:pdfvtid=""http://www.npes.org/pdfvt/ns/id/"">
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
          <rdf:li xml:lang=""x-default"">Sample {vtVersion} document with text and image content for variable data printing</rdf:li>
        </rdf:Alt>
      </dc:description>
      <xmp:CreatorTool>iText 8.0.5 for .NET</xmp:CreatorTool>
      <xmp:CreateDate>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</xmp:CreateDate>
      <xmp:ModifyDate>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</xmp:ModifyDate>
      <pdf:Producer>iText® 8.0.5 ©2000-2024 Apryse Group NV</pdf:Producer>
      <pdfx:GTS_PDFVTVersion>{vtVersion}</pdfx:GTS_PDFVTVersion>
      <pdfvtid:GTS_PDFVTVersion>{vtVersion}</pdfvtid:GTS_PDFVTVersion>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end=""w""?>";
        
        return System.Text.Encoding.UTF8.GetBytes(xmpTemplate);
    }
    
    protected void AddTextContent(Document document)
    {
        var title = new Paragraph($"{GetVtVersionMarker()} Document Sample")
            .SetFontSize(28)
            .SetBold()
            .SetFontColor(new DeviceRgb(33, 37, 41))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(30);
        document.Add(title);
        
        var subtitle = new Paragraph("Variable Data & Transactional Printing")
            .SetFontSize(16)
            .SetFontColor(new DeviceRgb(108, 117, 125))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(40);
        document.Add(subtitle);
        
        var intro = new Paragraph()
            .SetFontSize(12)
            .SetFontColor(new DeviceRgb(33, 37, 41))
            .SetTextAlignment(TextAlignment.JUSTIFIED)
            .SetMarginBottom(20);
        intro.Add($"This document demonstrates {GetVtVersionMarker()} (Variable Data and Transactional Printing) " +
                  $"capabilities using iText 8.0.5 for .NET. This version is based on {GetBaseStandard()} " +
                  $"and uses PDF {GetPdfVersionString()} features.");
        document.Add(intro);
        
        var featuresTitle = new Paragraph($"Key Features of {GetVtVersionMarker()}:")
            .SetFontSize(14)
            .SetBold()
            .SetFontColor(new DeviceRgb(33, 37, 41))
            .SetMarginTop(20)
            .SetMarginBottom(10);
        document.Add(featuresTitle);
        
        var features = GetFeatures();
        foreach (var feature in features)
        {
            var featurePara = new Paragraph(feature)
                .SetFontSize(11)
                .SetFontColor(new DeviceRgb(73, 80, 87))
                .SetMarginLeft(20)
                .SetMarginBottom(5);
            document.Add(featurePara);
        }
    }
    
    protected abstract string[] GetFeatures();
    
    protected void AddImageContent(Document document, string imagePath)
    {
        var imageTitle = new Paragraph("Sample Embedded Image")
            .SetFontSize(14)
            .SetBold()
            .SetFontColor(new DeviceRgb(33, 37, 41))
            .SetMarginTop(30)
            .SetMarginBottom(15);
        document.Add(imageTitle);
        
        if (File.Exists(imagePath))
        {
            var imageData = ImageDataFactory.Create(imagePath);
            var image = new Image(imageData)
                .SetWidth(300)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetMarginBottom(15);
            document.Add(image);
            
            var caption = new Paragraph("Figure 1: Geometric design sample demonstrating embedded image support")
                .SetFontSize(10)
                .SetItalic()
                .SetFontColor(new DeviceRgb(108, 117, 125))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(caption);
        }
    }
    
    protected void AddFooterContent(Document document)
    {
        var separator = new Paragraph("─".PadRight(80, '─'))
            .SetFontColor(new DeviceRgb(206, 212, 218))
            .SetMarginTop(40)
            .SetMarginBottom(20);
        document.Add(separator);
        
        var footer = new Paragraph()
            .SetFontSize(9)
            .SetFontColor(new DeviceRgb(108, 117, 125))
            .SetTextAlignment(TextAlignment.CENTER);
        footer.Add($"Generated on {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:HH:mm:ss}\n");
        footer.Add($"Created with iText 8.0.5 for .NET | {GetVtVersionMarker()} Compliant");
        document.Add(footer);
    }
}
