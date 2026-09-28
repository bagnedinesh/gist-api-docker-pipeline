using System.Text.Json;
using System.Text.Json.Serialization;

namespace GistApi.Models;

/// <summary>
/// Represents the GitHub gist fields returned by the API.
/// </summary>
public sealed class Gist
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    // Preserve GitHub fields not explicitly modeled so the response retains useful data.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
