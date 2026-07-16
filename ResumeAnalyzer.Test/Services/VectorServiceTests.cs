using Microsoft.Extensions.Configuration;
using Moq;
using ResumeAnalyzer.Services;
using Xunit;

namespace ResumeAnalyzer.Test.Services;

public class VectorServiceTests
{
    [Fact]
    public void VectorService_Initializes_WithMockedQdrantWrapper()
    {
        var mockConfig = new Mock<IConfiguration>();
        var mockWrapper = new Mock<IQdrantClientWrapper>();

        var service = new VectorService(mockConfig.Object, mockWrapper.Object);

        Assert.NotNull(service);
    }
}
