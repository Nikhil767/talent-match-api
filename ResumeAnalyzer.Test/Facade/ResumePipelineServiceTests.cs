using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ResumeAnalyzer.Domain.Entities;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Services;
using ResumeAnalyzer.Services.Facade;
using ResumeAnalyzer.Services.Strategy;
using Xunit;

namespace ResumeAnalyzer.Test.Facade;

public class ResumePipelineServiceTests
{
    [Fact]
    public async Task ProcessResumeAsync_ServiceInitializes_WithMockedDependencies()
    {
        // Arrange — use interfaces now that concrete classes are decoupled
        var mockLogger  = new Mock<ILogger<ResumePipelineService>>();
        var mockResumeRepo = new Mock<IResumeRepository>();
        var mockResumeAnalysisRepo = new Mock<IResumeAnalysisRepository>();
        var mockStorage = new Mock<ISupabaseStorageRestService>(); // interface
        var mockAnalysis = new Mock<IAnalysisStrategy>();
        var mockEmbedding = new Mock<IEmbeddingStrategy>();
        var mockVector = new Mock<IVectorService>();               // interface
        var mockConfig = new Mock<IConfiguration>();
        var mockSseBroker = new Mock<ResumeAnalyzer.Services.Sse.ISseBroker>();

        var resumeId = Guid.NewGuid();
        mockResumeRepo.Setup(r => r.GetByIdAsync(resumeId))
            .ReturnsAsync(new Resume { Id = resumeId, Status = "Queued", StoragePath = "test/path.pdf" });

        mockStorage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        mockAnalysis.Setup(a => a.ExtractSkillsAsync(It.IsAny<string>()))
            .ReturnsAsync("{\"summary\": \"Test summary\"}");

        var service = new ResumePipelineService(
            mockVector.Object,
            mockStorage.Object,
            mockResumeRepo.Object,
            mockResumeAnalysisRepo.Object,
            mockEmbedding.Object,
            mockAnalysis.Object,
            mockConfig.Object,
            mockSseBroker.Object,
            mockLogger.Object
        );

        Assert.NotNull(service);
    }
}
