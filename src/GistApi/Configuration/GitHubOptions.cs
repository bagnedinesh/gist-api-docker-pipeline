namespace GistApi.Configuration;

/// <summary>
/// Stores configuration values used when calling the GitHub API.
/// </summary>
public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";

    public string BaseUrl { get; set; } = "https://api.github.com/";

    public string UserAgent { get; set; } = "GistApi/1.0";
}
