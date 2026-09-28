using GistApi.Configuration;
using GistApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace GistApi.Endpoints;

/// <summary>
/// Maps HTTP routes and handles requests for a user's public gists.
/// </summary>
public static class GistEndpoints
{
    public static IEndpointRouteBuilder MapGistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{user}", HandleGetUserGists);
        return endpoints;
    }

    private static async Task<IResult> HandleGetUserGists(
        string user,
        int? page,
        int? per_page,
        IOptions<PaginationOptions> paginationOptions,
        IGistService gistService,
        CancellationToken cancellationToken)
    {
        var settings = paginationOptions.Value;
        var requestedPage = page ?? settings.DefaultPage;
        var requestedPerPage = per_page ?? settings.DefaultPerPage;

        if (requestedPage < 1 || requestedPerPage < 1 || requestedPerPage > settings.MaximumPerPage)
        {
            return Results.BadRequest(
                $"page must be at least 1 and per_page must be between 1 and {settings.MaximumPerPage}.");
        }

        var result = await gistService.GetUserGistsAsync(
            user,
            requestedPage,
            requestedPerPage,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.StatusCode((int)result.StatusCode);
        }

        return Results.Json(result.Gists);
    }
}
