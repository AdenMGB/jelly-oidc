using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JellyOidc.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyOidc.Services;

/// <summary>
/// Resolves and persists OIDC-to-Jellyfin account links.
/// </summary>
public sealed class AccountLinkService
{
    private readonly IUserManager _userManager;
    private readonly ILogger<AccountLinkService> _logger;

    public AccountLinkService(IUserManager userManager, ILogger<AccountLinkService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Finds a Jellyfin user for this OIDC session (existing link, email match, or explicit link target).
    /// </summary>
    public User? ResolveExistingUser(AuthorizedSession session, PluginConfiguration config)
    {
        if (session.LinkTargetUserId is Guid targetId)
        {
            try
            {
                return _userManager.GetUserById(targetId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Manual link target user {UserId} not found", targetId);
                return null;
            }
        }

        var linked = config.AccountLinks.FirstOrDefault(l => string.Equals(l.Subject, session.Subject, StringComparison.Ordinal));
        if (linked != null)
        {
            try
            {
                return _userManager.GetUserById(linked.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stored OIDC link for subject {Subject} points to missing user", session.Subject);
            }
        }

        if (!config.LinkAccountsByEmail || string.IsNullOrWhiteSpace(session.Email))
        {
            return null;
        }

        var email = session.Email.Trim();
        try
        {
            var byName = _userManager.GetUserByName(email);
            if (byName != null)
            {
                return byName;
            }
        }
        catch
        {
            // not found by exact username
        }

        return _userManager.Users.FirstOrDefault(u =>
            string.Equals(u.Username, email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Persists a link and updates plugin configuration.
    /// </summary>
    public void SaveLink(PluginConfiguration config, string subject, string email, Guid userId)
    {
        var links = config.AccountLinks?.ToList() ?? new List<OidcAccountLink>();
        links.RemoveAll(l => string.Equals(l.Subject, subject, StringComparison.Ordinal)
            || l.UserId == userId);
        links.Add(new OidcAccountLink
        {
            Subject = subject,
            Email = email,
            UserId = userId
        });
        config.AccountLinks = links.ToArray();
        Plugin.Instance.UpdateConfiguration(config);
        _logger.LogInformation("Linked OIDC subject to Jellyfin user {UserId}", userId);
    }
}