using Jellyfin.Plugin.JellyOidc.Configuration;

namespace Jellyfin.Plugin.JellyOidc.Helpers;

/// <summary>
/// Login page labels and provider naming.
/// </summary>
public static class LoginUiHelper
{
    /// <summary>
    /// Returns the SSO button label using the configured provider display name.
    /// </summary>
    public static string GetSignInButtonLabel(PluginConfiguration config)
    {
        var name = config.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "SSO";
        }

        var custom = config.ButtonLabel?.Trim();
        if (!string.IsNullOrWhiteSpace(custom) && custom.Contains("{provider}", StringComparison.OrdinalIgnoreCase))
        {
            return custom.Replace("{provider}", name, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(custom) && !custom.Equals("Sign in with SSO", StringComparison.OrdinalIgnoreCase))
        {
            return custom;
        }

        return $"Sign in with {name}";
    }
}