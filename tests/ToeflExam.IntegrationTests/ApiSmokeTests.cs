using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ToeflExam.IntegrationTests;

public sealed class ApiSmokeTests
{
    [Fact]
    public async Task Writing_frontend_is_served_with_security_headers()
    {
        await using var factory = CreateFactory("Production");
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("TOEFL Studio", html);
        Assert.Contains("/assets/", html);
        Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Unknown_api_route_is_not_replaced_by_frontend_html()
    {
        await using var factory = CreateFactory("Production");
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/api/unknown-route");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Liveness_returns_healthy()
    {
        await using var factory = CreateFactory("Production");
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task OpenApi_is_only_available_in_development(string environment, HttpStatusCode expected)
    {
        await using var factory = CreateFactory(environment);
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(document.RootElement.TryGetProperty("openapi", out _));
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment(environment));

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
}
