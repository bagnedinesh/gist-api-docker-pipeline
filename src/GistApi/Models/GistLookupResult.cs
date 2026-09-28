using System.Net;

namespace GistApi.Models;

/// <summary>
/// Contains the HTTP status and gist data returned from a lookup operation.
/// </summary>
public sealed record GistLookupResult(HttpStatusCode StatusCode, IReadOnlyList<Gist>? Gists)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and <= 299;
}
