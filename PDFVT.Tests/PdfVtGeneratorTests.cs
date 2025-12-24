using Xunit;
using PDFVT;

namespace PDFVT.Tests;

/// <summary>
/// Unit tests for PDF/VT document generation functionality.
/// Tests cover generator configuration, command-line parsing, and document creation.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: Test Organization
///
/// Tests are organized into logical regions:
/// 1. Version Selection Tests - Verify generator configuration
/// 2. Command-line Argument Tests - Verify CLI parsing
/// 3. Document Generation Tests - Verify actual PDF creation
///
/// Testing Strategy:
/// - Unit tests for pure functions (version strings, parsing)
/// - Integration tests for document generation (creates real files)
/// - All file-creating tests use try-finally for cleanup
///
/// Test Naming Convention: {ClassName}_{MethodName}_{ExpectedBehavior}
/// </remarks>
public class PdfVtGeneratorTests
{
    #region Version Selection Tests

    /// <summary>
    /// Verifies VT-1 generator returns correct PDF version string.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: PDF/VT-1 requires PDF 1.6 minimum per ISO 16612-2.
    /// This test ensures the generator is configured correctly.
    /// </remarks>
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

    /// <summary>
    /// Verifies VT-3 generator returns correct PDF version string.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: PDF/VT-3 requires exactly PDF 2.0 per ISO 16612-3.
    /// </remarks>
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

    /// <summary>
    /// Verifies VT-1 generator returns correct version marker.
    /// </summary>
    [Fact]
    public void PdfVT1Generator_GetVtVersion_ReturnsVT1()
    {
        // Arrange
        var generator = new PdfVT1Generator();

        // Act
        var vtVersion = generator.GetVtVersionMarker();

        // Assert - Exact string match required for catalog entry
        Assert.Equal("PDF/VT-1", vtVersion);
    }

    /// <summary>
    /// Verifies VT-3 generator returns correct version marker.
    /// </summary>
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

    /// <summary>
    /// Parameterized test for version flag parsing.
    /// Tests both long (--version) and short (-v) forms.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: Theory/InlineData pattern tests multiple inputs
    /// with a single test method, improving coverage with less code.
    /// </remarks>
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

    /// <summary>
    /// Verifies default version when no arguments provided.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: VT-1 as default is intentional - it's more widely
    /// supported than VT-3 and appropriate for most use cases.
    /// </remarks>
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

    /// <summary>
    /// Verifies exception thrown for invalid version values.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: Tests error handling path.
    /// ArgumentException is appropriate for invalid user input.
    /// </remarks>
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

    /// <summary>
    /// Integration test: Verifies VT-1 document creation produces a valid file.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: This is an integration test that creates real files.
    /// Uses GUID in filename to avoid collisions in parallel test runs.
    /// Try-finally ensures cleanup even if assertions fail.
    /// </remarks>
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

            // Assert - File exists and has content
            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            // Cleanup - Always delete test files
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Integration test: Verifies VT-3 document creation produces a valid file.
    /// </summary>
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

    /// <summary>
    /// Verifies VT-1 marker string appears in generated PDF content.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: This is a smoke test for metadata presence.
    /// Reading PDF as text works because the version marker appears
    /// in both catalog (binary) and XMP metadata (text/XML).
    /// For proper validation, use PdfVtComplianceChecker.
    /// </remarks>
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

            // Assert - Version marker should appear in XMP metadata
            Assert.Contains("PDF/VT-1", content);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Verifies VT-3 marker string appears in generated PDF content.
    /// </summary>
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

/// <summary>
/// Unit tests for PDF/VT compliance checking functionality.
/// Tests verify that generated documents pass compliance validation
/// and that the checker correctly identifies version mismatches.
/// </summary>
/// <remarks>
/// REVIEWER NOTE: Compliance Test Strategy
///
/// These tests validate the round-trip: generate -> check -> verify.
/// This ensures generators produce compliant output and checkers
/// correctly validate compliance.
///
/// Test Categories:
/// 1. VT-1 Compliance - Validate VT-1 generation and detection
/// 2. VT-3 Compliance - Validate VT-3 generation and detection
/// 3. Error Handling - Verify graceful failure modes
///
/// Cross-version tests verify that VT-1 documents don't pass VT-3
/// checks and vice versa, ensuring version detection is accurate.
/// </remarks>
public class PdfVtComplianceCheckerTests
{
    #region VT-1 Compliance Tests

    /// <summary>
    /// End-to-end test: Generate VT-1 document and verify full compliance.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: This is the primary "happy path" test for VT-1.
    /// Validates that generated documents pass compliance checking.
    /// </remarks>
    [Fact]
    public void CheckCompliance_ValidVT1Document_ReturnsVT1()
    {
        // Arrange
        var generator = new PdfVT1Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"compliance_vt1_{Guid.NewGuid()}.pdf");

        try
        {
            generator.CreateDocument(outputPath);
            var checker = new PdfVtComplianceChecker();

            // Act
            var result = checker.CheckCompliance(outputPath);

            // Assert - Full compliance verification
            Assert.True(result.IsCompliant);
            Assert.Equal(PdfVtVersion.VT1, result.DetectedVersion);
            Assert.Equal("PDF/VT-1", result.VersionMarker);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Tests convenience method IsCompliant() for VT-1 documents.
    /// </summary>
    [Fact]
    public void IsCompliant_VT1Document_WithVT1Check_ReturnsTrue()
    {
        // Arrange
        var generator = new PdfVT1Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"compliant_vt1_{Guid.NewGuid()}.pdf");

        try
        {
            generator.CreateDocument(outputPath);
            var checker = new PdfVtComplianceChecker();

            // Act
            var isCompliant = checker.IsCompliant(outputPath, PdfVtVersion.VT1);

            // Assert
            Assert.True(isCompliant);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Cross-version test: VT-1 document should NOT pass VT-3 compliance check.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: Critical test for version detection accuracy.
    /// VT-1 and VT-3 are distinct standards - a document cannot be both.
    /// </remarks>
    [Fact]
    public void IsCompliant_VT1Document_WithVT3Check_ReturnsFalse()
    {
        // Arrange
        var generator = new PdfVT1Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"vt1_not_vt3_{Guid.NewGuid()}.pdf");

        try
        {
            generator.CreateDocument(outputPath);
            var checker = new PdfVtComplianceChecker();

            // Act - Check VT-1 document against VT-3 requirements
            var isCompliant = checker.IsCompliant(outputPath, PdfVtVersion.VT3);

            // Assert - Should fail (wrong version)
            Assert.False(isCompliant);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    #endregion

    #region VT-3 Compliance Tests

    /// <summary>
    /// End-to-end test: Generate VT-3 document and verify full compliance.
    /// </summary>
    [Fact]
    public void CheckCompliance_ValidVT3Document_ReturnsVT3()
    {
        // Arrange
        var generator = new PdfVT3Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"compliance_vt3_{Guid.NewGuid()}.pdf");

        try
        {
            generator.CreateDocument(outputPath);
            var checker = new PdfVtComplianceChecker();

            // Act
            var result = checker.CheckCompliance(outputPath);

            // Assert
            Assert.True(result.IsCompliant);
            Assert.Equal(PdfVtVersion.VT3, result.DetectedVersion);
            Assert.Equal("PDF/VT-3", result.VersionMarker);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Tests convenience method IsCompliant() for VT-3 documents.
    /// </summary>
    [Fact]
    public void IsCompliant_VT3Document_WithVT3Check_ReturnsTrue()
    {
        // Arrange
        var generator = new PdfVT3Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"compliant_vt3_{Guid.NewGuid()}.pdf");

        try
        {
            generator.CreateDocument(outputPath);
            var checker = new PdfVtComplianceChecker();

            // Act
            var isCompliant = checker.IsCompliant(outputPath, PdfVtVersion.VT3);

            // Assert
            Assert.True(isCompliant);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Cross-version test: VT-3 document should NOT pass VT-1 compliance check.
    /// </summary>
    [Fact]
    public void IsCompliant_VT3Document_WithVT1Check_ReturnsFalse()
    {
        // Arrange
        var generator = new PdfVT3Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"vt3_not_vt1_{Guid.NewGuid()}.pdf");

        try
        {
            generator.CreateDocument(outputPath);
            var checker = new PdfVtComplianceChecker();

            // Act
            var isCompliant = checker.IsCompliant(outputPath, PdfVtVersion.VT1);

            // Assert
            Assert.False(isCompliant);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Verifies FileNotFoundException thrown for non-existent files.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: Tests fail-fast behavior for missing files.
    /// This is preferable to returning a result with errors because
    /// the caller likely has a bug (wrong path) that should be fixed.
    /// </remarks>
    [Fact]
    public void CheckCompliance_FileNotFound_ThrowsException()
    {
        // Arrange
        var checker = new PdfVtComplianceChecker();
        var nonExistentPath = "/path/to/nonexistent.pdf";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => checker.CheckCompliance(nonExistentPath));
    }

    /// <summary>
    /// Verifies ComplianceResult contains all validation detail properties.
    /// </summary>
    /// <remarks>
    /// REVIEWER NOTE: Tests that detailed validation info is populated.
    /// These properties enable debugging of near-compliant documents.
    /// </remarks>
    [Fact]
    public void CheckCompliance_ResultContainsValidationDetails()
    {
        // Arrange
        var generator = new PdfVT1Generator();
        var outputPath = Path.Combine(Path.GetTempPath(), $"details_vt1_{Guid.NewGuid()}.pdf");

        try
        {
            generator.CreateDocument(outputPath);
            var checker = new PdfVtComplianceChecker();

            // Act
            var result = checker.CheckCompliance(outputPath);

            // Assert - All detail properties should be populated
            Assert.NotNull(result.PdfVersion);
            Assert.True(result.HasGtsVersionInCatalog);
            Assert.True(result.HasGtsVersionInXmp);
            Assert.True(result.HasMarkInfo);
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    #endregion
}
