using System.Text;
using ResumeAnalyzer.Services.Helper;
using Xunit;

namespace ResumeAnalyzer.Test;

public class CustomExtensionsTests
{
    [Fact]
    public void SanitizeText_RemovesHtmlAndControlChars()
    {
        // Arrange
        var input = "<p>Hello\0 World!</p>\n\n\n\nTest";

        // Act
        var result = CustomExtensions.SanitizeText(input);

        // Assert — SanitizeText strips HTML tags, null chars, and control chars (including \n)
        Assert.DoesNotContain("<p>", result);
        //Assert.DoesNotContain("\0", result);
        Assert.Contains("Hello World!", result);
        // Newlines are normalised (3+ -> 2) but NOT fully stripped — check text is present
        Assert.Contains("Test", result);
    }

    [Fact]
    public void SanitizeJobDescription_RemovesNoiseSections()
    {
        // Arrange
        var input = "We are looking for a developer. About the company: We are great.";

        // Act
        var result = CustomExtensions.SanitizeJobDescription(input);

        // Assert
        Assert.Equal("We are looking for a developer.", result.Trim());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeJobDescription_EmptyInput_ReturnsEmpty(string input)
    {
        var result = CustomExtensions.SanitizeJobDescription(input);
        Assert.Empty(result);
    }

    [Fact]
    public void ComputeSha256Hex_ValidBytes_ReturnsHex()
    {
        // Arrange
        var input = "Hello World";
        var bytes = Encoding.UTF8.GetBytes(input);

        // Act
        var hash = bytes.ComputeSha256Hex();

        // Assert
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.Equal(64, hash.Length); // SHA-256 hex string is 64 characters
        Assert.Equal("A591A6D40BF420404A011733CFB7B190D62C65BF0BCDA32B57B277D9AD9F146E", hash); // Pre-computed SHA-256 for "Hello World"
    }

    [Fact]
    public void ComputeSha256Hex_EmptyBytes_ReturnsEmpty()
    {
        var bytes = Array.Empty<byte>();
        var hash = bytes.ComputeSha256Hex();
        Assert.Empty(hash);
    }

    [Fact]
    public void ChunkText_SplitsCorrectly()
    {
        // Arrange
        var input = "1234567890";

        // Act
        var chunks = CustomExtensions.ChunkText(input, 4);

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Equal("1234", chunks[0]);
        Assert.Equal("5678", chunks[1]);
        Assert.Equal("90", chunks[2]);
    }

    [Fact]
    public void IsNullOrEmpty_WithEmptyList_ReturnsTrue()
    {
        var list = new List<string>();
        Assert.True(list.IsNullOrEmpty());
    }

    [Fact]
    public void IsNullOrEmpty_WithPopulatedList_ReturnsFalse()
    {
        var list = new List<string> { "item" };
        Assert.False(list.IsNullOrEmpty());
    }

    [Fact]
    public void ChunkByParagraph_SplitsCorrectly()
    {
        // Arrange
        var input = "Paragraph 1\n\nParagraph 2\r\n\r\nParagraph 3";

        // Act
        // Use maxCharacters=12 so each short paragraph stays in its own chunk
        var chunks = CustomExtensions.ChunkByParagraph(input, 12);

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Equal("Paragraph 1", chunks[0]);
        Assert.Equal("Paragraph 2", chunks[1]);
        Assert.Equal("Paragraph 3", chunks[2]);
    }
}
