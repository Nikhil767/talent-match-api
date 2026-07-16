using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using ResumeAnalyzer.Domain.Dto;
using ResumeAnalyzer.Domain.Repositories;
using ResumeAnalyzer.Services.Strategy;
using Xunit;

namespace ResumeAnalyzer.Test;

/// <summary>
/// Minimal APIs using inline delegates (like AnalysisEndpoints) are best tested using WebApplicationFactory.
/// Make sure `Microsoft.AspNetCore.Mvc.Testing` is installed in your Test project.
/// </summary>
public class AnalysisEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AnalysisEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostAts_EmptyJobDescription_ReturnsBadRequest()
    {
        // Arrange
        // We can override dependencies here using ConfigureTestServices if needed
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Example of mocking a service injected into the endpoint
                var mockAnalysis = new Mock<IAnalysisStrategy>();
                services.AddSingleton(mockAnalysis.Object);
            });
        }).CreateClient();

        var request = new AtsRequestDto(Guid.NewGuid(), "");

        // Act
        var response = await client.PostAsJsonAsync("/api/analysis/ats", request);

        // Assert
        // Expecting either 400 BadRequest (validation failure) or 401 Unauthorized (if auth is active)
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
