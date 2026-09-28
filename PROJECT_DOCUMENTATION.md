# GitHub Gist API Documentation

## Purpose

This is a JSON-only .NET 10 API. It accepts a GitHub username, requests that user's public Gists from GitHub, and returns them as a JSON array.

Example:

```text
GET http://localhost:8080/octocat
```

There is no browser interface in this project. Use an API tool, browser address bar, `curl`, or PowerShell to send requests.

## Project structure

```text
src/GistApi/
├── Configuration/
│   ├── GitHubOptions.cs
│   └── PaginationOptions.cs
├── Endpoints/
│   └── GistEndpoints.cs
├── Models/
│   ├── Gist.cs
│   └── GistLookupResult.cs
├── Services/
│   ├── IGistService.cs
│   └── GitHubGistService.cs
├── appsettings.json
├── GistApi.csproj
└── Program.cs

tests/GistApi.Tests/
├── GistApi.Tests.csproj
└── GistTests.cs

Dockerfile
.dockerignore
RUNNING.md
PROJECT_DOCUMENTATION.md
```

## Request flow

```text
HTTP client
    -> GistEndpoints validates username and paging values
    -> IGistService contract
    -> GitHubGistService calls GitHub REST API
    -> GitHub response becomes Gist models
    -> endpoint returns JSON or an HTTP status
```

The API endpoint is `GET /{user}`. For example, `/octocat` maps `octocat` to the route's `user` parameter. The service then calls:

```text
https://api.github.com/users/octocat/gists?page=1&per_page=5
```

## Components

### `Program.cs`

This is the application's startup/composition root. It:

- Creates the ASP.NET Core application builder.
- Reads settings and binds them to typed options.
- Registers the configured GitHub HTTP client and service implementation.
- Registers the API routes.
- Starts the HTTP server.

Keeping startup code here makes it easy to see how the application is assembled without mixing it with request and GitHub logic.

### `Endpoints/GistEndpoints.cs`

Defines `GET /{user}` and handles HTTP concerns:

- Reads the route username and optional query parameters.
- Uses configured defaults when paging parameters are omitted.
- Rejects invalid paging values with `400 Bad Request`.
- Calls the service layer.
- Returns Gists as JSON or propagates GitHub's failure status.

The endpoint does not make HTTP requests to GitHub itself.

### `Services/IGistService.cs`

Defines the operation needed by the endpoint: retrieve a page of a user's Gists. The endpoint depends on this interface, not the GitHub-specific implementation. This keeps HTTP handling separable from external-service access and makes it straightforward to substitute a fake service/client in tests.

### `Services/GitHubGistService.cs`

Implements `IGistService`. It builds the GitHub API path, sends the request using an injected `HttpClient`, handles unsuccessful HTTP statuses, and deserializes successful JSON into Gist models.

The client is created through `IHttpClientFactory`, which manages client lifetimes and connection reuse. The test suite replaces the HTTP message handler so tests do not make network calls.

### `Models/Gist.cs`

Represents the Gist data used by API consumers, including `id`, `html_url`, and `description`. `JsonExtensionData` retains other fields returned by GitHub, even if the application has not explicitly listed them as properties.

### `Models/GistLookupResult.cs`

Carries the upstream HTTP status and any returned Gists from the service to the endpoint. The endpoint uses it to decide whether to return JSON or an error status.

### Configuration classes and `appsettings.json`

`Configuration/GitHubOptions.cs` models the GitHub API base URL and User-Agent. `Configuration/PaginationOptions.cs` models paging defaults and the maximum page size.

Defaults are stored in `src/GistApi/appsettings.json`:

```json
{
  "GitHub": {
    "BaseUrl": "https://api.github.com/",
    "UserAgent": "GistApi/1.0"
  },
  "Pagination": {
    "DefaultPage": 1,
    "DefaultPerPage": 5,
    "MaximumPerPage": 100
  }
}
```

ASP.NET Core configuration can be overridden with environment variables. Double underscores represent nested sections. For example, `Pagination__DefaultPerPage=10` changes the default page size to 10 without editing the JSON file.

## API and pagination

Route:

```text
GET /{user}
```

Query parameters:

| Name | Default | Allowed values |
|---|---:|---|
| `page` | `1` | 1 or greater |
| `per_page` | `5` | 1 through 100 |

Examples:

```text
/octocat
/octocat?page=2
/octocat?page=1&per_page=10
```

When omitted, the endpoint uses the values from `appsettings.json`. A page less than 1, page size less than 1, or page size over 100 returns `400 Bad Request` before contacting GitHub.

The API uses unauthenticated GitHub access for public Gists. GitHub rate limits therefore apply. Each API page request makes a separate GitHub request; the automated tests do not use GitHub's quota because they use a fake HTTP handler.

## Automated tests

Tests are in `tests/GistApi.Tests/GistTests.cs`. The suite uses xUnit, FluentAssertions, and `WebApplicationFactory<Program>` to start the API in memory.

A fake `HttpMessageHandler` returns a fixed response instead of contacting GitHub. This makes the tests deterministic and repeatable, even when the computer has no network access or GitHub is unavailable.

The tests verify:

- A successful request returns a JSON array with a meaningful Gist ID and URL.
- Default pagination values are sent to the GitHub client.
- Invalid page size returns `400 Bad Request` without a GitHub request.
- A user with no Gists returns an empty JSON array.
- GitHub `404 Not Found` and `500 Internal Server Error` statuses are propagated.

Run them from the repository root:

```powershell
dotnet test .\tests\GistApi.Tests
```

## Run locally

Prerequisite: install the .NET 10 SDK and verify it is available:

```powershell
dotnet --info
```

Build the API:

```powershell
dotnet build .\src\GistApi
```

Start the API on the required port, `8080`:

```powershell
dotnet run --project .\src\GistApi --urls "http://localhost:8080"
```

Send a request from another PowerShell window:

```powershell
Invoke-WebRequest "http://localhost:8080/octocat"
```

To print and parse the JSON response:

```powershell
$data = (Invoke-WebRequest "http://localhost:8080/octocat").Content | ConvertFrom-Json
$data.Count
```

Stop the server with `Ctrl+C` in the terminal where it is running.

## Run in Docker

Start Docker Desktop, then run these commands from the repository root.

Build the image:

```powershell
docker build -t gistapi:local .
```

Run the container, publishing only the API port:

```powershell
docker run --rm -p 8080:8080 gistapi:local
```

Request Gists at:

```text
http://localhost:8080/octocat
```

If port 8080 is already allocated, find the process/container using it or select an alternate host port, for example `-p 8082:8080`. The application inside the container continues listening on 8080.

After changing source code, rebuild the image and recreate the container so it includes those changes:

```powershell
docker build --no-cache -t gistapi:local .
docker run --rm -p 8080:8080 gistapi:local
```

## Dockerfile overview

The `Dockerfile` uses a multi-stage build:

1. The .NET SDK image restores and publishes the application.
2. The smaller ASP.NET runtime image receives the published output.
3. The final image runs `GistApi.dll` and listens on port 8080.

This keeps build tools out of the runtime image. `EXPOSE 8080` documents the container port; `docker run -p 8080:8080` publishes it on the host.

The `.dockerignore` file excludes local build and IDE files such as `bin`, `obj`, and `.vs`, reducing the Docker build context.

## Troubleshooting

### `dotnet` is not recognized

Install the .NET 10 SDK, reopen PowerShell, and check `dotnet --info`.

### `docker` is not recognized or the daemon is unavailable

Install and start Docker Desktop, wait for the engine to report that it is running, then check `docker info`.

### Port 8080 is already allocated

Inspect running containers with `docker ps`. Stop the container using the port, or publish an alternate host port such as `-p 8082:8080`.

### GitHub returns an error

The username may not exist, GitHub may be rate-limiting unauthenticated requests, or GitHub/the network may be unavailable. The API forwards GitHub's HTTP error status to the caller.

## Scope and production considerations

The project separates HTTP endpoints, service logic, models, and configuration to make each responsibility easier to change and test. This organization alone does not make the service production-ready or automatically scalable.

Possible future production improvements include GitHub authentication, response caching, explicit upstream timeout/retry policies, structured logging, monitoring, and health checks. They are intentionally outside the current API-only exercise scope.
