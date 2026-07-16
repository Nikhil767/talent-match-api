using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using ResumeAnalyzer.Endpoints;
using Xunit;

namespace ResumeAnalyzer.Test;

public class ResumeEndpointsTests
{
    private readonly IConfiguration _configuration;

    public ResumeEndpointsTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Files:AllowedExtensions:0", ".pdf"},
            {"Files:AllowedExtensions:1", ".docx"},
            {"Files:AllowedMimeTypes:0", "application/pdf"},
            {"Files:AllowedMimeTypes:1", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {"Files:MinSizeKb", "10"},
            {"Files:MaxSizeMb", "5"},
            {"Files:MaxFileNameLength", "100"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void Validate_ValidFile_ReturnsTrue()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(20 * 1024); // 20 KB

        // Act
        var result = ResumeEndpoints.Validate(fileMock.Object, _configuration, out string error);

        // Assert
        Assert.True(result);
        Assert.Empty(error);
    }

    [Fact]
    public void Validate_FileTooSmall_ReturnsFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(5 * 1024); // 5 KB (less than min 10KB)

        // Act
        var result = ResumeEndpoints.Validate(fileMock.Object, _configuration, out string error);

        // Assert
        Assert.False(result);
        Assert.Contains("too small", error);
    }

    [Fact]
    public void Validate_InvalidExtension_ReturnsFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("malware.exe");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(20 * 1024);

        // Act
        var result = ResumeEndpoints.Validate(fileMock.Object, _configuration, out string error);

        // Assert
        Assert.False(result);
        Assert.Contains("Invalid file type", error);
    }
}
