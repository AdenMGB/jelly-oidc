using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.JellyOidc.Helpers;

/// <summary>
/// Builds external URLs for redirect URIs behind proxies.
/// </summary>
public static class RequestUrlHelper
{
    /// <summary>
    /// Gets the public base URL for redirects.
    /// </summary>
    public static string GetBaseUrl(HttpRequest request, string? overrideBase)
    {
        if (!string.IsNullOrWhiteSpace(overrideBase))
        {
            return overrideBase.TrimEnd('/');
        }

        var port = request.Host.Port ?? -1;
        if ((port == 80 && request.Scheme == "http") || (port == 443 && request.Scheme == "https"))
        {
            port = -1;
        }

        var builder = new UriBuilder
        {
            Scheme = request.Scheme,
            Host = request.Host.Host,
            Port = port,
            Path = request.PathBase
        };
        return builder.Uri.ToString().TrimEnd('/');
    }
}