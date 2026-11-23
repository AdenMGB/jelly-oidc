using System.Text.Json;

namespace Jellyfin.Plugin.JellyOidc.Helpers;

/// <summary>
/// Parses role and username claims from OIDC tokens.
/// </summary>
public static class ClaimParser
{
    /// <summary>
    /// Resolves a username from claim types in priority order.
    /// </summary>
    public static string? ResolveUsername(
        IEnumerable<System.Security.Claims.Claim> claims,
        string primaryClaim,
        params string[] fallbacks)
    {
        var types = new[] { primaryClaim }.Concat(fallbacks).Where(t => !string.IsNullOrWhiteSpace(t));
        foreach (var type in types)
        {
            var value = claims.FirstOrDefault(c => c.Type == type)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return SanitizeUsername(value);
            }
        }

        return SanitizeUsername(claims.FirstOrDefault(c => c.Type == "sub")?.Value);
    }

    /// <summary>
    /// Extracts role names from a claim, including JSON object keys (Authentik/Zitadel style).
    /// </summary>
    public static IReadOnlyList<string> ExtractRoles(string? claimType, IEnumerable<System.Security.Claims.Claim> claims)
    {
        if (string.IsNullOrWhiteSpace(claimType))
        {
            return Array.Empty<string>();
        }

        var segments = claimType.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var raw = ResolveClaimValue(claims, segments.Length > 0 ? segments[0] : claimType);
        if (raw == null && segments.Length > 1)
        {
            raw = claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return ParseRoleValue(raw);
    }

    private static string? ResolveClaimValue(IEnumerable<System.Security.Claims.Claim> claims, string root)
    {
        return claims.FirstOrDefault(c => c.Type == root)?.Value;
    }

    /// <summary>
    /// Parses a raw role claim string.
    /// </summary>
    public static IReadOnlyList<string> ParseRoleValue(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(raw);
                return list?.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    ?? new List<string>();
            }
            catch (JsonException)
            {
                return Array.Empty<string>();
            }
        }

        if (raw.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return doc.RootElement.EnumerateObject()
                        .Select(p => p.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
            catch (JsonException)
            {
                return Array.Empty<string>();
            }
        }

        if (raw.Contains(',', StringComparison.Ordinal))
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new List<string> { raw };
    }

    private static string? SanitizeUsername(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = new string(value.Where(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-' or '@').ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized.ToLowerInvariant();
    }
}