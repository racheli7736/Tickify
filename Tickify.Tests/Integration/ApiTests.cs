using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tickify.Tests.Integration;

/// <summary>
/// Hosts the real API in memory (Testing environment: no user secrets, so the development database
/// can never be reached) against the isolated "_test" PostgreSQL database.
/// </summary>
public sealed class TickifyApiFactory : WebApplicationFactory<Program>
{
    public const string ReactOrigin = "http://localhost:5173";

    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "tickify-api-tests-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connection = DatabaseFixture.ConnectionString
            ?? throw new InvalidOperationException($"{DatabaseFixture.EnvVarName} is not set.");
        DatabaseFixture.EnsureIsTestDatabase(connection);

        Directory.CreateDirectory(_contentRoot);   // keeps the NLog files of the tests out of the project folder
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(_contentRoot);
        builder.UseSetting("ConnectionStrings:Default", connection);
        builder.UseSetting("Jwt:Key", new string('t', 64));
        builder.UseSetting("Jwt:Issuer", "Tickify");
        builder.UseSetting("Jwt:Audience", "Tickify.Client");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
        builder.UseSetting("Cors:AllowedOrigins:0", ReactOrigin);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(_contentRoot, recursive: true); } catch (IOException) { /* log files may still be closing */ }
    }
}

[Collection(IntegrationCollection.Name)]
public class ApiTests : IClassFixture<TickifyApiFactory>
{
    private readonly TickifyApiFactory _factory;
    private readonly DatabaseFixture _database;

    public ApiTests(TickifyApiFactory factory, DatabaseFixture database)
    {
        _factory = factory;
        _database = database;
    }

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<JsonNode>())!["token"]!.GetValue<string>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private Task<HttpClient> AdminAsync() => LoginAsync("admin@tickets.local", "Admin123!");
    private Task<HttpClient> DemoUserAsync() => LoginAsync("user@tickets.local", "User123!");

    // ---------- authentication / authorization ----------

    [PostgresFact]
    public async Task Anonymous_ProtectedEndpoint_Returns401WithUniformErrorBody()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal(401, body["status"]!.GetValue<int>());
        Assert.False(string.IsNullOrEmpty(body["correlationId"]?.GetValue<string>()));
    }

    [PostgresFact]
    public async Task User_CannotUseAdminEndpoints_Returns403()
    {
        var user = await DemoUserAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/api/categories", new { name = "Nope" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.DeleteAsync("/api/events/1")).StatusCode);
    }

    [PostgresFact]
    public async Task Admin_CannotBuyTickets_Returns403()
    {
        var admin = await AdminAsync();

        var response = await admin.PostAsJsonAsync("/api/orders", new { eventId = 1, seatIds = new[] { 1 } });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- duplicate registration / category over real HTTP ----------

    [PostgresFact]
    public async Task Register_SameEmailInParallel_NeverReturns500()
    {
        for (var round = 0; round < 5; round++)
        {
            var body = new { fullName = "Race Test", email = $"http-race-{Guid.NewGuid():N}@example.com", password = "secret12" };

            var codes = (await Task.WhenAll(
                _factory.CreateClient().PostAsJsonAsync("/api/auth/register", body),
                _factory.CreateClient().PostAsJsonAsync("/api/auth/register", body)))
                .Select(r => (int)r.StatusCode).OrderBy(c => c).ToArray();

            Assert.Equal(new[] { 201, 409 }, codes);
        }
    }

    [PostgresFact]
    public async Task CreateCategory_SameNameInParallel_NeverReturns500()
    {
        var admin = await AdminAsync();
        for (var round = 0; round < 5; round++)
        {
            var body = new { name = $"HttpRace-{Guid.NewGuid():N}" };

            var codes = (await Task.WhenAll(
                admin.PostAsJsonAsync("/api/categories", body),
                admin.PostAsJsonAsync("/api/categories", body)))
                .Select(r => (int)r.StatusCode).OrderBy(c => c).ToArray();

            Assert.Equal(new[] { 201, 409 }, codes);
        }
    }

    // ---------- CORS for the React dev server ----------

    [PostgresFact]
    public async Task Cors_PreflightFromTheReactOrigin_IsAllowed()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/orders");
        request.Headers.Add("Origin", TickifyApiFactory.ReactOrigin);
        request.Headers.Add("Access-Control-Request-Method", "DELETE");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(TickifyApiFactory.ReactOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("DELETE", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
    }

    [PostgresFact]
    public async Task Cors_ResponsesExposeTheCorrelationIdHeaderToTheBrowser()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/events");
        request.Headers.Add("Origin", TickifyApiFactory.ReactOrigin);

        var response = await _factory.CreateClient().SendAsync(request);   // 401, but CORS headers still apply

        Assert.Contains("X-Correlation-Id", response.Headers.GetValues("Access-Control-Expose-Headers").Single());
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [PostgresFact]
    public async Task Cors_OtherOrigins_AreNotAllowed()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/events");
        request.Headers.Add("Origin", "http://evil.example");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // ---------- admin orders screen ----------

    [PostgresFact]
    public async Task AdminOrders_ShowWhoBought_WithoutExposingTheEmail()
    {
        await _database.ResetAsync();
        var user = await DemoUserAsync();
        var buy = await user.PostAsJsonAsync("/api/orders", new { eventId = 1, seatIds = new[] { 1 } });
        Assert.Equal(HttpStatusCode.Created, buy.StatusCode);
        var orderId = (await buy.Content.ReadFromJsonAsync<JsonNode>())!["id"]!.GetValue<int>();

        var admin = await AdminAsync();
        var json = await admin.GetStringAsync("/api/orders?pageSize=50");
        var order = JsonNode.Parse(json)!["items"]!.AsArray().Single(o => o!["id"]!.GetValue<int>() == orderId)!;

        Assert.Equal("Demo User", order["userFullName"]!.GetValue<string>());
        Assert.True(order["userId"]!.GetValue<int>() > 0);
        Assert.DoesNotContain("user@tickets.local", json);   // no e-mail address anywhere in the response
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);

        var single = await admin.GetStringAsync($"/api/orders/{orderId}");
        Assert.Contains("Demo User", single);
        Assert.DoesNotContain("user@tickets.local", single);
    }

    [PostgresFact]
    public async Task AnotherUser_CannotReadSomeoneElsesOrder_Returns403()
    {
        await _database.ResetAsync();
        var owner = await DemoUserAsync();
        var buy = await owner.PostAsJsonAsync("/api/orders", new { eventId = 1, seatIds = new[] { 2 } });
        var orderId = (await buy.Content.ReadFromJsonAsync<JsonNode>())!["id"]!.GetValue<int>();

        var other = _factory.CreateClient();
        var register = await other.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Other Person", email = $"other-{Guid.NewGuid():N}@example.com", password = "secret12" });
        var token = (await register.Content.ReadFromJsonAsync<JsonNode>())!["token"]!.GetValue<string>();
        other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/orders/{orderId}")).StatusCode);
    }
}
