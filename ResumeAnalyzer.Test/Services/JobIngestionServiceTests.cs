using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Services;
using Xunit;

namespace ResumeAnalyzer.Test.Services;

public class JobIngestionServiceTests
{
    [Fact]
    public async Task FetchJobsByGetAsync_ReturnsMappedJobs_OnSuccess()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JobIngestionService>>();

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                // JSON must match: json.GetProperty("data").GetProperty("jobs") used in production code
                Content = new StringContent(
                    "{ \"data\": { \"jobs\": [ { \"job_id\": \"123\", \"job_title\": \"Developer\", \"employer_name\": \"TechCorp\", \"job_description\": \"C# Dev\" } ] } }")
            });

        // Set BaseAddress so relative URL calls inside JobIngestionService succeed
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://jsearch.p.rapidapi.com")
        };

        var mockHttpFactory = new Mock<IHttpClientFactory>();
        mockHttpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var service = new JobIngestionService(mockHttpFactory.Object, mockLogger.Object);

        var request = new JobSearchRequestDto { Query = "Developer" };

        // Act
        var result = await service.FetchJobsByGetAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("123", result.First().JobId);
        Assert.Equal("Developer", result.First().Title);
    }
}
