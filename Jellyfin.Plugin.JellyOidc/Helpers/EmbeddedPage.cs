using System.Reflection;
using System.Text.Json;

namespace Jellyfin.Plugin.JellyOidc.Helpers;

/// <summary>
/// Loads embedded HTML templates and substitutes tokens.
/// </summary>
public static class EmbeddedPage
{
    /// <summary>
    /// Renders an embedded resource with token replacement.
    /// </summary>
    public static string Render(string resourceName, IReadOnlyDictionary<string, string> tokens)
    {
        var assembly = typeof(EmbeddedPage).Assembly;
        var fullName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded resource {resourceName} not found");

        using var stream = assembly.GetManifestResourceStream(fullName)!;
        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd();
        foreach (var token in tokens)
        {
            html = html.Replace(token.Key, token.Value, StringComparison.Ordinal);
        }

        return html;
    }

    /// <summary>
    /// Serializes page bootstrap data for JSON script islands.
    /// </summary>
    public static string SerializeData<T>(T data) => JsonSerializer.Serialize(data);
}