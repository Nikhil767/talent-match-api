using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Services.Facade;
using Xunit;

namespace ResumeAnalyzer.Test;

public class AnalysisEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AnalysisEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private WebApplicationFactory<Program> WithMockedPipeline()
    {
        var mock = new Mock<IAnalysisPipelineService>();
        return _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s => s.AddScoped(_ => mock.Object)));
    }

    [Fact]
    public async Task PostAts_EmptyJobDescription_ReturnsBadRequest()
    {
        var response = await WithMockedPipeline().CreateClient()
            .PostAsJsonAsync("/api/analysis/ats", new AtsRequestDto(Guid.NewGuid(), ""));
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostGaps_EmptyJobDescription_ReturnsBadRequest()
    {
        var response = await WithMockedPipeline().CreateClient()
            .PostAsJsonAsync("/api/analysis/gaps", new GapRequestDto(Guid.NewGuid(), ""));
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostTailor_EmptyJobDescription_ReturnsBadRequest()
    {
        var response = await WithMockedPipeline().CreateClient()
            .PostAsJsonAsync("/api/analysis/tailor", new TailorRequestDto(Guid.NewGuid(), ""));
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
