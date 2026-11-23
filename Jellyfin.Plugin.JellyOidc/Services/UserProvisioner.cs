using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.JellyOidc.Auth;
using Jellyfin.Plugin.JellyOidc.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyOidc.Services;

/// <summary>
/// Creates and updates Jellyfin users from OIDC logins.
/// </summary>
public sealed class UserProvisioner
{
    private readonly IUserManager _userManager;
    private readonly AccountLinkService _accountLinks;
    private readonly ILogger<UserProvisioner> _logger;

    public UserProvisioner(
        IUserManager userManager,
        AccountLinkService accountLinks,
        ILogger<UserProvisioner> logger)
    {
        _userManager = userManager;
        _accountLinks = accountLinks;
        _logger = logger;
    }

    /// <summary>
    /// Ensures a Jellyfin user exists and applies RBAC.
    /// </summary>
    public async Task<Guid> ProvisionAsync(AuthorizedSession session, PluginConfiguration config, CancellationToken cancellationToken = default)
    {
        if (config.EnableAuthorization && config.AllowedRoles.Length > 0)
        {
            var allowed = session.Roles.Any(r => config.AllowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            if (!allowed && !session.IsAdmin)
            {
                throw new InvalidOperationException("User is not a member of an allowed role.");
            }
        }

        var existing = _accountLinks.ResolveExistingUser(session, config);
        User? user = existing;

        if (user == null && !session.LinkExistingOnly)
        {
            try
            {
                user = _userManager.GetUserByName(session.Username);
            }
            catch
            {
                user = null;
            }
        }

        var providerId = typeof(JellyOidcAuthenticationProvider).FullName!;

        if (user == null)
        {
            if (session.LinkExistingOnly)
            {
                throw new InvalidOperationException(
                    "No Jellyfin account matched this identity. Use the same email as your Jellyfin username, or sign in with your password and link from the dashboard.");
            }

            if (!config.AutoCreateUsers)
            {
                throw new InvalidOperationException("User does not exist and auto-create is disabled.");
            }

            user = await _userManager.CreateUserAsync(session.Username).ConfigureAwait(false);
            user.AuthenticationProviderId = providerId;
            _logger.LogInformation("Created Jellyfin user {Username} via OIDC", session.Username);
        }
        else
        {
            _accountLinks.SaveLink(config, session.Subject, session.Email, user.Id);

            if (config.UpdateUserOnLogin
                && !string.Equals(user.AuthenticationProviderId, providerId, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(user.AuthenticationProviderId))
            {
                _logger.LogWarning("User {Username} uses provider {Provider}, re-binding to Jelly-OIDC", user.Username, user.AuthenticationProviderId);
            }

            user.AuthenticationProviderId = providerId;
            _logger.LogInformation("Signed in Jellyfin user {Username} via OIDC (linked)", user.Username);
        }

        ApplyPolicy(user, session, config);
        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
        return user.Id;
    }

    private static void ApplyPolicy(User user, AuthorizedSession session, PluginConfiguration config)
    {
        var isAdmin = session.IsAdmin
            || session.Roles.Any(r => config.AdminRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

        user.SetPermission(PermissionKind.IsAdministrator, isAdmin);
        user.SetPermission(PermissionKind.EnableAllFolders, config.EnableAllFolders);
        user.SetPermission(PermissionKind.EnableLiveTvAccess, true);
        user.SetPermission(PermissionKind.EnableLiveTvManagement, isAdmin);
    }
}