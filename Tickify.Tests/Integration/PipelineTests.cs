using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Tickify.Tests.Integration;

/// <summary>
/// The real API with a database that cannot be reached, so a real unhandled exception happens.
/// Needs no PostgreSQL, no Docker and no configuration: it always runs.
/// </summary>
public class PipelineTests : IClassFixture<UnreachableDatabaseApiFactory>
{
    private readonly UnreachableDatabaseApiFactory _factory;

    public PipelineTests(UnreachableDatabaseApiFactory factory) => _factory = factory;

    [Fact]
    public async Task UnhandledException_ReturnsUniform500_WithCorrelationIdAndCorsHeaders_AndLeaksNothing()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = "someone@example.com", password = "secret12" })
        };
        request.Headers.Add("Origin", TestApiFactory.ReactOrigin);
        request.Headers.Add("X-Correlation-Id", "pipeline-1");

        var response = await _factory.CreateClient().SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var body = JsonNode.Parse(text)!;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(500, body["status"]!.GetValue<int>());
        Assert.Equal("pipeline-1", body["correlationId"]!.GetValue<string>());
        Assert.Equal("pipeline-1", response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal(TestApiFactory.ReactOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());   // React can read the 500
        Assert.DoesNotContain("Npgsql", text);
        Assert.DoesNotContain("Host=", text);
        Assert.DoesNotContain("secret12", text);
    }

    [Fact]
    public async Task Unauthorized_HasTheSameBodyShapeAndACorrelationId()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events");
        var body = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(401, body["status"]!.GetValue<int>());
        Assert.Equal(response.Headers.GetValues("X-Correlation-Id").Single(), body["correlationId"]!.GetValue<string>());
    }
}
