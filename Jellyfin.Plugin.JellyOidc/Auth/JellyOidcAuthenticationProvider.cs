using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Authentication;

namespace Jellyfin.Plugin.JellyOidc.Auth;

/// <summary>
/// Authentication provider bound to OIDC-provisioned accounts.
/// </summary>
public class JellyOidcAuthenticationProvider : IAuthenticationProvider
{
    /// <inheritdoc />
    public string Name => "Jelly-OIDC";

    /// <inheritdoc />
    public bool IsEnabled => Plugin.Instance.Configuration.Enabled;

    /// <inheritdoc />
    public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
    {
        throw new AuthenticationException("This account uses OpenID Connect. Sign in with the SSO button or Quick Connect.");
    }

    /// <inheritdoc />
    public bool HasPassword(User user) => false;

    /// <inheritdoc />
    public Task ChangePassword(User user, string newPassword)
    {
        throw new AuthenticationException("Password changes are managed by your identity provider.");
    }
}