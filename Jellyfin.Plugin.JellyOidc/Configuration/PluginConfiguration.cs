using System.ComponentModel;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JellyOidc.Configuration;

/// <summary>
/// Plugin settings persisted by Jellyfin.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    public const string DefaultProviderId = "default";

    public bool Enabled { get; set; }

    public string DisplayName { get; set; } = "Authentik";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string[] Scopes { get; set; } = ["openid", "profile", "email"];

    public string RoleClaim { get; set; } = "groups";

    public string UsernameClaim { get; set; } = "preferred_username";

    public string[] AllowedRoles { get; set; } = ["jellyfin_users"];

    public string[] AdminRoles { get; set; } = ["jellyfin_admins"];

    public bool EnableAuthorization { get; set; } = true;

    public bool AutoCreateUsers { get; set; } = true;

    public bool UpdateUserOnLogin { get; set; } = true;

    public bool EnableAllFolders { get; set; } = true;

    public bool ShowLoginButton { get; set; } = true;

    public bool OidcOnly { get; set; }

    public bool AutoRedirectToOidc { get; set; }

    public string ButtonLabel { get; set; } = string.Empty;

    public bool LinkAccountsByEmail { get; set; } = true;

    public bool AllowManualAccountLinking { get; set; } = true;

    public OidcAccountLink[] AccountLinks { get; set; } = [];

    public bool UseJellyfinThemeVars { get; set; } = true;

    public string? RedirectBaseOverride { get; set; }

    public bool DisableHttpsDiscovery { get; set; }

    public bool InjectLoginScript { get; set; } = true;

    public bool SyncLoginDisclaimer { get; set; } = true;
}