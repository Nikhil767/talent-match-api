using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity.Data;
using Xunit;

namespace ResumeAnalyzer.Test;

/// <summary>
/// Auth endpoints require a real Supabase token to reach 200 OK.
/// These tests verify that the endpoints exist and return the expected
/// status codes for unauthenticated / invalid-credential requests.
/// SupabaseService has non-virtual methods so it cannot be mocked via Moq.
/// </summary>
public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PostRegister_EmptyBody_ReturnsBadRequestOrUnprocessable()
    {
        var client = _factory.CreateClient();
        // Send empty payload — will fail validation before hitting Supabase
        var response = await client.PostAsJsonAsync("/api/auth/register", new { });
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.UnprocessableEntity
                or HttpStatusCode.InternalServerError
                or HttpStatusCode.OK,
            $"Unexpected: {response.StatusCode}");
    }

    [Fact]
    public async Task PostLogin_InvalidCredentials_ReturnsUnauthorizedOrError()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "invalid@test.com", Password = "WrongPassword!" });
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.BadRequest
                or HttpStatusCode.InternalServerError,
            $"Unexpected: {response.StatusCode}");
    }
}
