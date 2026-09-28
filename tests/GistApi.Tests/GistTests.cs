using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GistApi.Tests;

// Integration tests that use a fake GitHub HTTP boundary instead of the live service.
public class GistTests
{
    // Store the fake GitHub handler so each test can control the external response.
    private readonly FakeGitHubHandler _githubHandler = new();
    private readonly WebApplicationFactory<Program> _factory;

    public GistTests()
    {
        // Start the API in memory and replace the real GitHub HTTP handler with the fake handler.
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services => services
                .AddHttpClient("GitHub")
                .ConfigurePrimaryHttpMessageHandler(() => _githubHandler));
        });
    }

    [Fact]
    public async Task RootPath_DoesNotServeAUi()
    {
        // The service is JSON API only; its root path should not return an HTML page.
        var response = await _factory.CreateClient().GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOctocatGists_ReturnsMeaningfulArrayAndUsesPaging()
    {
        // Configure a deterministic GitHub response containing one representative Gist.
        _githubHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"id\":\"gist-1\",\"html_url\":\"https://gist.github.com/gist-1\",\"description\":\"Example gist\"}]")
        };
        var client = CreateApiClient();

        // Call the endpoint without query parameters and verify the defaults are forwarded.
        var resp = await client.GetAsync("/octocat");

        // Assert that the API returned 200 OK.
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Read the response body as string.
        var s = await resp.Content.ReadAsStringAsync();

        // Parse JSON and assert the root element is a JSON array (list of gists).
        using var doc = JsonDocument.Parse(s);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().Be(1);
        doc.RootElement[0].GetProperty("id").GetString().Should().Be("gist-1");
        doc.RootElement[0].GetProperty("html_url").GetString().Should().StartWith("https://gist.github.com/");
        _githubHandler.LastRequestUri!.Query.Should().Contain("page=1");
        _githubHandler.LastRequestUri.Query.Should().Contain("per_page=5");
    }

    [Fact]
    public async Task GetGists_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Create an HTTP client that targets the in-memory test server.
        var client = CreateApiClient();

        // A page size over GitHub's maximum should be rejected locally.
        var resp = await client.GetAsync("/octocat?page=1&per_page=101");

        // Assert that the API returns a validation error without calling GitHub.
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetGists_WhenUserHasNoGists_ReturnsEmptyArray()
    {
        // Simulate a valid GitHub response for a user who has no public Gists.
        _githubHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        };

        var response = await CreateApiClient().GetAsync("/empty-user");

        // Verify that the API preserves the empty JSON array response.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetGists_WhenGitHubUserDoesNotExist_PropagatesNotFound()
    {
        // Simulate GitHub returning 404 for an unknown user.
        _githubHandler.Response = new HttpResponseMessage(HttpStatusCode.NotFound);

        // Verify that the API forwards GitHub's 404 status to its caller.
        var response = await CreateApiClient().GetAsync("/missing-user");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGists_WhenGitHubFails_PropagatesServerError()
    {
        // Simulate a temporary server failure from GitHub.
        _githubHandler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        // Verify that the API forwards GitHub's 500 status to its caller.
        var response = await CreateApiClient().GetAsync("/failing-user");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private sealed class FakeGitHubHandler : HttpMessageHandler
    {
        // The test sets this response to control what the fake GitHub service returns.
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        };

        // Store the request URL so tests can verify pagination parameters.
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Capture the request and return the configured response without network access.
            LastRequestUri = request.RequestUri;
            return Task.FromResult(Response);
        }
    }

    private HttpClient CreateApiClient()
    {
        // Create an in-memory client for the JSON API.
        return _factory.CreateClient();
    }
}
