using GistApi.Configuration;
using GistApi.Endpoints;
using GistApi.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

// Create the application builder and load settings from appsettings.json and environment variables.
var builder = WebApplication.CreateBuilder(args);

// Keep JSON responses indented so they are easier to inspect during development.
builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.WriteIndented = true);

// Bind external-service and pagination settings to strongly typed options classes.
builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection(GitHubOptions.SectionName));
builder.Services.Configure<PaginationOptions>(builder.Configuration.GetSection(PaginationOptions.SectionName));

// Configure a typed GitHub HTTP client and register the service that consumes it.
builder.Services.AddHttpClient<IGistService, GitHubGistService>("GitHub", (services, client) =>
{
    var options = services.GetRequiredService<IOptions<GitHubOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
});

// Build the application after all services and configuration have been registered.
var app = builder.Build();

// Register the HTTP routes implemented by the endpoint module.
app.MapGistEndpoints();

// Start listening for HTTP requests.
app.Run();

// Expose the generated entry-point type so WebApplicationFactory can start the API in tests.
public partial class Program
{
}
