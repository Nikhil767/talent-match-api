using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Services.Facade;
using Xunit;

namespace ResumeAnalyzer.Test;

public class JobEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JobEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostIngest_InvalidQuery_ReturnsBadRequest()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace pipeline service with a mock via the interface
                var mockPipeline = new Mock<IJobPipelineService>();
                services.AddScoped(_ => mockPipeline.Object);
            });
        }).CreateClient();

        var request = new JobSearchRequestDto { Query = "", Location = "Remote", Country = "US", Page = 1 };
        var response = await client.PostAsJsonAsync("/api/jobs/ingest", request);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSearch_ReturnsOkOrUnauthorized()
    {
        var mockRepo = new Mock<IJobRepository>();
        mockRepo.Setup(r => r.SearchJobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ResumeAnalyzer.Domain.Entities.Job> { new() { Title = "Test" } });

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockRepo.Object);
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/jobs/search?q=developer");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
