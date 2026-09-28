using System.Net;
using System.Text.Json;
using GistApi.Models;

namespace GistApi.Services;

/// <summary>
/// Calls GitHub's API and converts a user's public gists into application models.
/// </summary>
public sealed class GitHubGistService : IGistService
{
    private readonly HttpClient _httpClient;

    public GitHubGistService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GistLookupResult> GetUserGistsAsync(
        string user,
        int page,
        int perPage,
        CancellationToken cancellationToken)
    {
        var url = $"users/{Uri.EscapeDataString(user)}/gists?page={page}&per_page={perPage}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new GistLookupResult(response.StatusCode, null);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var gists = await JsonSerializer.DeserializeAsync<List<Gist>>(
            responseStream,
            cancellationToken: cancellationToken) ?? new List<Gist>();

        return new GistLookupResult(HttpStatusCode.OK, gists);
    }
}
