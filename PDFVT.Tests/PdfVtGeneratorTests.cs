using Xunit;
using PDFVT;

namespace PDFVT.Tests;

public class PdfVtGeneratorTests
{
    #region Version Selection Tests
    
    [Fact]
    public void PdfVT1Generator_GetPdfVersion_ReturnsPdf16()
    {
        // Arrange
        var generator = new PdfVT1Generator();
        
        // Act
        var version = generator.GetPdfVersionString();
        
        // Assert
        Assert.Equal("1.6", version);
    }
    
    [Fact]
    public void PdfVT3Generator_GetPdfVersion_ReturnsPdf20()
    {
        // Arrange
        var generator = new PdfVT3Generator();
        
        // Act
        var version = generator.GetPdfVersionString();
        
        // Assert
        Assert.Equal("2.0", version);
    }
    
    [Fact]
    public void PdfVT1Generator_GetVtVersion_ReturnsVT1()
    {
        // Arrange
        var generator = new PdfVT1Generator();
        
        // Act
        var vtVersion = generator.GetVtVersionMarker();
        
        // Assert
        Assert.Equal("PDF/VT-1", vtVersion);
    }
    
    [Fact]
    public void PdfVT3Generator_GetVtVersion_ReturnsVT3()
    {
        // Arrange
        var generator = new PdfVT3Generator();
        
        // Act
        var vtVersion = generator.GetVtVersionMarker();
        
        // Assert
        Assert.Equal("PDF/VT-3", vtVersion);
    }
    
    #endregion
    
    #region Command-line Argument Tests
    
    [Theory]
    [InlineData("--version", "vt1", PdfVtVersion.VT1)]
    [InlineData("--version", "vt3", PdfVtVersion.VT3)]
    [InlineData("-v", "vt1", PdfVtVersion.VT1)]
    [InlineData("-v", "vt3", PdfVtVersion.VT3)]
    public void CommandLineParser_ParseVersion_ReturnsCorrectVersion(string flag, string value, PdfVtVersion expected)
    {
        // Arrange
        var args = new[] { flag, value };
        
        // Act
        var result = CommandLineParser.Parse(args);
        
        // Assert
        Assert.Equal(expected, result.Version);
    }
    
    [Fact]
    public void CommandLineParser_NoArgs_DefaultsToVT1()
    {
        // Arrange
        var args = Array.Empty<string>();
        
        // Act
        var result = CommandLineParser.Parse(args);
        
        // Assert
        Assert.Equal(PdfVtVersion.VT1, result.Version);
    }
    
    [Fact]
    public void CommandLineParser_InvalidVersion_ThrowsException()
    {
        // Arrange
        var args = new[] { "--version", "invalid" };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CommandLineParser.Parse(args));
    }
    
    #endregion
    
    #region Document Generation Tests
    
    [Fact]
    public void PdfVT1Generator_CreateDocument_GeneratesFile()
    {
        // Arrange
        var generator = new PdfVT1Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_vt1_{Guid.NewGuid()}.pdf");
        
        try
        {
            // Act
            generator.CreateDocument(outputPath);
            
            // Assert
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
    
    [Fact]
    public void PdfVT3Generator_CreateDocument_GeneratesFile()
    {
        // Arrange
        var generator = new PdfVT3Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_vt3_{Guid.NewGuid()}.pdf");
        
        try
        {
            // Act
            generator.CreateDocument(outputPath);
            
            // Assert
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
    
    [Fact]
    public void PdfVT1Generator_CreateDocument_ContainsVT1Marker()
    {
        // Arrange
        var generator = new PdfVT1Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_marker_vt1_{Guid.NewGuid()}.pdf");
        
        try
        {
            // Act
            generator.CreateDocument(outputPath);
            var content = File.ReadAllText(outputPath);
            
            // Assert
            Assert.Contains("PDF/VT-1", content);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
    
    [Fact]
    public void PdfVT3Generator_CreateDocument_ContainsVT3Marker()
    {
        // Arrange
        var generator = new PdfVT3Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_marker_vt3_{Guid.NewGuid()}.pdf");
        
        try
        {
            // Act
            generator.CreateDocument(outputPath);
            var content = File.ReadAllText(outputPath);
            
            // Assert
            Assert.Contains("PDF/VT-3", content);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
    
    #endregion
}
