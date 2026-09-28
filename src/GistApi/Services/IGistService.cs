using GistApi.Models;

namespace GistApi.Services;

/// <summary>
/// Defines the contract for retrieving a user's public gists.
/// </summary>
public interface IGistService
{
    Task<GistLookupResult> GetUserGistsAsync(
        string user,
        int page,
        int perPage,
        CancellationToken cancellationToken);
}
