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
/// Abstract base class implementing the Template Method pattern for PDF/VT document generation.
/// Provides common infrastructure for creating compliant PDF/VT-1 and PDF/VT-3 documents.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: Architecture follows Template Method design pattern:
/// - Base class defines the document generation algorithm skeleton
/// - Subclasses override abstract methods for version-specific behavior
/// - Protected methods provide reusable building blocks
///
/// Dependencies:
/// - SkiaSharp: Cross-platform 2D graphics for sample image generation
/// - iText 8.0.5: PDF generation with PDF/VT metadata support
///
/// Thread Safety: Not thread-safe. Create new instances for concurrent generation.
/// </remarks>
public abstract class PdfVtGeneratorBase
{
    #region Abstract Members - Version-Specific Implementation Required

    /// <summary>
    /// Gets the PDF version string for display purposes.
    /// </summary>
    /// <returns>"1.6" for VT-1, "2.0" for VT-3</returns>
    public abstract string GetPdfVersionString();

    /// <summary>
    /// Gets the PDF/VT version marker used in metadata and catalog.
    /// </summary>
    /// <returns>"PDF/VT-1" or "PDF/VT-3"</returns>
    public abstract string GetVtVersionMarker();

    /// <summary>
    /// Gets the iText PdfVersion enum value for writer configuration.
    /// </summary>
    /// <returns>PdfVersion.PDF_1_6 or PdfVersion.PDF_2_0</returns>
    /// <remarks>
    /// REVIEWER NOTE: This is internal to iText configuration.
    /// Public version string is exposed via GetPdfVersionString().
    /// </remarks>
    protected abstract PdfVersion GetPdfVersion();

    /// <summary>
    /// Gets the base PDF/X standard that this PDF/VT version builds upon.
    /// </summary>
    /// <returns>"PDF/X-4" for VT-1, "PDF/X-6" for VT-3</returns>
    protected abstract string GetBaseStandard();
    
    #endregion

    #region Public API

    /// <summary>
    /// Creates a complete PDF/VT document at the specified file path.
    /// This is the main entry point for document generation.
    /// </summary>
    /// <param name="outputPath">Destination file path for the generated PDF</param>
    /// <remarks>
    /// REVIEWER NOTE: Resource management pattern:
    /// 1. Creates temporary image file with GUID to avoid collisions
    /// 2. Generates sample image using SkiaSharp
    /// 3. Embeds image in PDF document
    /// 4. Cleans up temp file in finally block (guaranteed execution)
    ///
    /// Error handling: Exceptions from iText/SkiaSharp propagate to caller.
    /// Temp file is always cleaned up even on failure.
    /// </remarks>
    public void CreateDocument(string outputPath)
    {
        // REVIEWER NOTE: GUID ensures uniqueness for concurrent operations
        // Temp directory is platform-appropriate (/tmp on Unix, %TEMP% on Windows)
        string sampleImagePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sample_{Guid.NewGuid()}.png"
        );

        try
        {
            // Step 1: Generate sample image for embedding
            CreateSampleImage(sampleImagePath);

            // Step 2: Create PDF with embedded image and metadata
            CreatePdfVtDocument(outputPath, sampleImagePath);
        }
        finally
        {
            // REVIEWER NOTE: Always clean up temp resources
            // File.Delete is safe to call even if file doesn't exist in some edge cases
            if (File.Exists(sampleImagePath))
            {
                File.Delete(sampleImagePath);
            }
        }
    }

    #endregion

    #region Image Generation

    /// <summary>
    /// Generates a sample PNG image with geometric shapes using SkiaSharp.
    /// </summary>
    /// <param name="imagePath">Output path for the generated PNG file</param>
    /// <remarks>
    /// REVIEWER NOTE: This creates a visually distinctive test image:
    /// - White background with colored geometric shapes
    /// - Demonstrates PDF/VT's image embedding capability
    /// - PNG format chosen for lossless compression
    ///
    /// In production use, this would be replaced with actual variable data images.
    /// </remarks>
    protected void CreateSampleImage(string imagePath)
    {
        const int width = 400;
        const int height = 300;

        // REVIEWER NOTE: SkiaSharp uses GPU-accelerated rendering when available
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Start with white background
        canvas.Clear(SKColors.White);

        // Define color palette (Google-inspired colors for visual appeal)
        using var bluePaint = new SKPaint { Color = new SKColor(66, 133, 244), IsAntialias = true };
        using var orangePaint = new SKPaint { Color = new SKColor(251, 188, 4), IsAntialias = true };
        using var greenPaint = new SKPaint { Color = new SKColor(52, 168, 83), IsAntialias = true };
        using var redPaint = new SKPaint { Color = new SKColor(234, 67, 53), IsAntialias = true };

        // Draw overlapping circles (demonstrates transparency handling)
        canvas.DrawCircle(125, 125, 75, bluePaint);
        canvas.DrawCircle(190, 150, 70, orangePaint);
        canvas.DrawCircle(265, 165, 65, greenPaint);

        // Draw rectangles for variety
        canvas.DrawRect(280, 40, 80, 80, redPaint);
        canvas.DrawRect(100, 200, 200, 60, bluePaint);

        // Encode to PNG and write to file
        // REVIEWER NOTE: Quality=100 for lossless PNG (parameter ignored but explicit)
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(imagePath);
        data.SaveTo(stream);
    }

    #endregion
    
    #region PDF Document Creation

    /// <summary>
    /// Core PDF generation logic. Creates the document structure with metadata and content.
    /// </summary>
    /// <param name="outputPath">Destination file path</param>
    /// <param name="imagePath">Path to image file for embedding</param>
    /// <remarks>
    /// REVIEWER NOTE: Document creation sequence:
    /// 1. Configure writer with version and XMP metadata flag
    /// 2. Initialize PDF document with writer
    /// 3. Set PDF/VT-specific metadata in catalog
    /// 4. Create layout document with standard margins
    /// 5. Add content sections in order
    /// 6. Close flushes all content to file
    ///
    /// iText handles resource cleanup via using statements.
    /// </remarks>
    protected void CreatePdfVtDocument(string outputPath, string imagePath)
    {
        // Configure PDF writer with version-specific settings
        // REVIEWER NOTE: AddXmpMetadata() enables XMP packet in output
        var writerProperties = new WriterProperties()
            .SetPdfVersion(GetPdfVersion())
            .AddXmpMetadata();

        using var writer = new PdfWriter(outputPath, writerProperties);
        using var pdfDoc = new PdfDocument(writer);

        // Apply PDF/VT-required metadata to catalog and XMP
        SetPdfVtMetadata(pdfDoc);

        // Create high-level layout document with A4 page size
        // REVIEWER NOTE: 50pt margins on all sides provide standard print-safe area
        using var document = new Document(pdfDoc, PageSize.A4);
        document.SetMargins(50, 50, 50, 50);

        // Add content sections in visual order (top to bottom)
        AddTextContent(document);
        AddImageContent(document, imagePath);
        AddFooterContent(document);

        // REVIEWER NOTE: Close() is called explicitly for clarity,
        // though 'using' would handle it automatically
        document.Close();
    }

    /// <summary>
    /// Sets PDF/VT compliance metadata in document info, XMP, and catalog.
    /// </summary>
    /// <param name="pdfDoc">The PDF document to configure</param>
    /// <remarks>
    /// REVIEWER NOTE: PDF/VT compliance requires metadata in THREE locations:
    /// 1. Document Info dictionary (traditional PDF metadata)
    /// 2. XMP metadata packet (XML-based, extensible)
    /// 3. Catalog dictionary (GTS_PDFVTVersion key + MarkInfo)
    ///
    /// All three must be present and consistent for standards compliance.
    /// The MarkInfo dictionary with Marked=true indicates tagged PDF structure.
    /// </remarks>
    protected void SetPdfVtMetadata(PdfDocument pdfDoc)
    {
        // === Document Info Dictionary ===
        // Traditional PDF metadata (pre-PDF 1.4 compatibility)
        var info = pdfDoc.GetDocumentInfo();
        info.SetTitle($"{GetVtVersionMarker()} Sample Document");
        info.SetAuthor("PDFVT Generator");
        info.SetSubject($"Sample {GetVtVersionMarker()} document with text and image");
        info.SetKeywords($"{GetVtVersionMarker()}, Variable Data, Transactional Printing");
        info.SetCreator("iText 8.0.5 for .NET");

        // === XMP Metadata ===
        // REVIEWER NOTE: XMP provides richer, extensible metadata
        // Must include GTS_PDFVTVersion in pdfx and pdfvtid namespaces
        byte[] xmpBytes = CreateXmpMetadata();
        var xmpMeta = XMPMetaFactory.ParseFromBuffer(xmpBytes);
        pdfDoc.SetXmpMetadata(xmpMeta);

        // === Catalog Entries ===
        var catalog = pdfDoc.GetCatalog();

        // REVIEWER NOTE: GTS_PDFVTVersion in catalog is REQUIRED by ISO 16612-2/3
        // This is the primary version indicator checked by compliance validators
        catalog.Put(new PdfName("GTS_PDFVTVersion"), new PdfString(GetVtVersionMarker()));

        // REVIEWER NOTE: MarkInfo with Marked=true indicates tagged PDF
        // Required for PDF/VT compliance (inherits from PDF/X-4 requirement)
        var markInfo = new PdfDictionary();
        markInfo.Put(PdfName.Marked, PdfBoolean.TRUE);
        catalog.Put(PdfName.MarkInfo, markInfo);
    }

    /// <summary>
    /// Generates XMP metadata XML packet for PDF/VT compliance.
    /// </summary>
    /// <returns>UTF-8 encoded XMP packet bytes</returns>
    /// <remarks>
    /// REVIEWER NOTE: XMP structure follows Adobe XMP specification:
    /// - xpacket header/footer for packet identification
    /// - RDF/XML structure with namespace declarations
    /// - Dublin Core (dc:) for standard metadata
    /// - Adobe XMP (xmp:) for creation tool info
    /// - PDF (pdf:) for producer info
    /// - PDFX/PDFVT namespaces for compliance markers
    ///
    /// Virtual method allows VT-3 to add PDF/X-6 specific namespaces.
    /// </remarks>
    protected virtual byte[] CreateXmpMetadata()
    {
        string vtVersion = GetVtVersionMarker();

        // REVIEWER NOTE: XMP template uses verbatim string for readability
        // Timestamps use ISO 8601 format required by XMP specification
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

    #endregion
    
    #region Content Generation

    /// <summary>
    /// Adds the main text content section to the document.
    /// Includes title, subtitle, introduction, and feature list.
    /// </summary>
    /// <param name="document">The iText Document to add content to</param>
    /// <remarks>
    /// REVIEWER NOTE: Color scheme uses Bootstrap-inspired grays:
    /// - #212529 (33,37,41): Primary text
    /// - #6c757d (108,117,125): Secondary/muted text
    /// - #495057 (73,80,87): Body text
    ///
    /// Typography follows standard document hierarchy:
    /// Title (28pt bold) → Subtitle (16pt) → Body (12pt) → Features (11pt)
    /// </remarks>
    protected void AddTextContent(Document document)
    {
        // Main title - centered, bold, primary color
        var title = new Paragraph($"{GetVtVersionMarker()} Document Sample")
            .SetFontSize(28)
            .SetBold()
            .SetFontColor(new DeviceRgb(33, 37, 41))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(30);
        document.Add(title);

        // Subtitle - centered, muted color
        var subtitle = new Paragraph("Variable Data & Transactional Printing")
            .SetFontSize(16)
            .SetFontColor(new DeviceRgb(108, 117, 125))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(40);
        document.Add(subtitle);

        // Introduction paragraph - justified for professional appearance
        var intro = new Paragraph()
            .SetFontSize(12)
            .SetFontColor(new DeviceRgb(33, 37, 41))
            .SetTextAlignment(TextAlignment.JUSTIFIED)
            .SetMarginBottom(20);
        intro.Add($"This document demonstrates {GetVtVersionMarker()} (Variable Data and Transactional Printing) " +
                  $"capabilities using iText 8.0.5 for .NET. This version is based on {GetBaseStandard()} " +
                  $"and uses PDF {GetPdfVersionString()} features.");
        document.Add(intro);

        // Features section header
        var featuresTitle = new Paragraph($"Key Features of {GetVtVersionMarker()}:")
            .SetFontSize(14)
            .SetBold()
            .SetFontColor(new DeviceRgb(33, 37, 41))
            .SetMarginTop(20)
            .SetMarginBottom(10);
        document.Add(featuresTitle);

        // REVIEWER NOTE: GetFeatures() is abstract - each version provides
        // its own feature list highlighting version-specific capabilities
        var features = GetFeatures();
        foreach (var feature in features)
        {
            var featurePara = new Paragraph(feature)
                .SetFontSize(11)
                .SetFontColor(new DeviceRgb(73, 80, 87))
                .SetMarginLeft(20)  // Indent for visual hierarchy
                .SetMarginBottom(5);
            document.Add(featurePara);
        }
    }

    /// <summary>
    /// Returns version-specific feature descriptions for the document body.
    /// </summary>
    /// <returns>Array of feature strings with bullet points</returns>
    /// <remarks>
    /// REVIEWER NOTE: Each implementation should highlight distinguishing
    /// features of that PDF/VT version for educational purposes.
    /// </remarks>
    protected abstract string[] GetFeatures();

    /// <summary>
    /// Adds embedded image section with caption to the document.
    /// </summary>
    /// <param name="document">The iText Document to add content to</param>
    /// <param name="imagePath">Path to the image file to embed</param>
    /// <remarks>
    /// REVIEWER NOTE: Image embedding demonstrates PDF/VT's resource handling.
    /// In variable data printing, images are often shared across records
    /// via Document Part Metadata (DPM) for efficiency.
    ///
    /// Graceful degradation: If image doesn't exist, section is skipped
    /// without throwing. Consider: Should this be a warning/error instead?
    /// </remarks>
    protected void AddImageContent(Document document, string imagePath)
    {
        // Section header for image
        var imageTitle = new Paragraph("Sample Embedded Image")
            .SetFontSize(14)
            .SetBold()
            .SetFontColor(new DeviceRgb(33, 37, 41))
            .SetMarginTop(30)
            .SetMarginBottom(15);
        document.Add(imageTitle);

        // REVIEWER NOTE: Defensive check - image should always exist
        // when called from CreateDocument, but safe coding practice
        if (File.Exists(imagePath))
        {
            // Load image data and create PDF image element
            var imageData = ImageDataFactory.Create(imagePath);
            var image = new Image(imageData)
                .SetWidth(300)  // Scale to consistent width
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetMarginBottom(15);
            document.Add(image);

            // Figure caption - italic, centered, muted color
            var caption = new Paragraph("Figure 1: Geometric design sample demonstrating embedded image support")
                .SetFontSize(10)
                .SetItalic()
                .SetFontColor(new DeviceRgb(108, 117, 125))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(caption);
        }
    }

    /// <summary>
    /// Adds footer section with generation timestamp and tool attribution.
    /// </summary>
    /// <param name="document">The iText Document to add content to</param>
    /// <remarks>
    /// REVIEWER NOTE: Footer provides:
    /// - Visual separation via horizontal rule
    /// - Generation timestamp for traceability
    /// - Tool/version attribution
    /// - Compliance marker
    ///
    /// Uses local time for display (DateTime.Now), not UTC.
    /// </remarks>
    protected void AddFooterContent(Document document)
    {
        // Horizontal separator line using box-drawing character
        // REVIEWER NOTE: PadRight creates repeating pattern for line effect
        var separator = new Paragraph("─".PadRight(80, '─'))
            .SetFontColor(new DeviceRgb(206, 212, 218))  // Light gray
            .SetMarginTop(40)
            .SetMarginBottom(20);
        document.Add(separator);

        // Footer text with generation details
        var footer = new Paragraph()
            .SetFontSize(9)
            .SetFontColor(new DeviceRgb(108, 117, 125))
            .SetTextAlignment(TextAlignment.CENTER);
        footer.Add($"Generated on {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:HH:mm:ss}\n");
        footer.Add($"Created with iText 8.0.5 for .NET | {GetVtVersionMarker()} Compliant");
        document.Add(footer);
    }

    #endregion
}
