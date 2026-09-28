# RUNNING the .NET Gist API

This file gives concise commands to build, test, and run the example API.

Prerequisites
- Install the .NET 10 SDK (dotnet CLI) — https://dotnet.microsoft.com/download/dotnet/10.0
- Docker is optional for running in a container.

Quick steps

1. Build and run locally (API listens on port 8080)

```powershell
dotnet build src\GistApi
dotnet run --project src\GistApi --urls "http://localhost:8080"
```

2. Run the automated tests (xUnit)

```powershell
dotnet test tests\GistApi.Tests
```

3. Build and run the Docker image

```bash
docker build -t gistapi:local .
docker run --rm -p 8080:8080 gistapi:local
```

Usage
- API endpoint: `GET /{user}` — returns the user's public Gists as a JSON array on port 8080.
- Example: `http://localhost:8080/octocat`
- Pagination parameters: `page` defaults to `1`; `per_page` defaults to `5` and accepts values from `1` to `100`.
- Example: `curl "http://localhost:8080/octocat?page=2&per_page=5"`

Notes
- Application settings are in `src\GistApi\appsettings.json`: GitHub URL/User-Agent and pagination defaults/maximum.
- `Program.cs` wires the endpoint, service, and configuration; GitHub HTTP behavior is in the service layer.
- The GitHub request is replaceable with a fake handler in tests, so tests do not require internet access.
- The service returns the GitHub Gists as a JSON array.
- No authentication is required for public gists; GitHub rate limits may apply.
