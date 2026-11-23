namespace Jellyfin.Plugin.JellyOidc.Configuration;

/// <summary>
/// Maps an OIDC subject to a Jellyfin user.
/// </summary>
public class OidcAccountLink
{
    public string Subject { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Guid UserId { get; set; }
}