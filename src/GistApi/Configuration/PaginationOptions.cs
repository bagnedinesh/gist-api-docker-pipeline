namespace GistApi.Configuration;

/// <summary>
/// Defines the default and maximum pagination values for gist requests.
/// </summary>
public sealed class PaginationOptions
{
    public const string SectionName = "Pagination";

    public int DefaultPage { get; set; } = 1;

    public int DefaultPerPage { get; set; } = 5;

    public int MaximumPerPage { get; set; } = 100;
}
