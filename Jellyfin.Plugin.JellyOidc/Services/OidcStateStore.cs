using System.Collections.Concurrent;
using IdentityModel.OidcClient;

namespace Jellyfin.Plugin.JellyOidc.Services;

/// <summary>
/// In-memory authorization state and post-login session tokens.
/// </summary>
public sealed class OidcStateStore
{
    private readonly ConcurrentDictionary<string, PendingLoginState> _pending = new();
    private readonly ConcurrentDictionary<string, AuthorizedSession> _sessions = new();

    public static TimeSpan StateExpiry { get; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Stores an OIDC authorize flow state.
    /// </summary>
    public void StorePending(string key, PendingLoginState state) => _pending[key] = state;

    /// <summary>
    /// Retrieves pending state without removing it.
    /// </summary>
    public PendingLoginState? GetPending(string key)
    {
        PurgeExpired();
        return _pending.TryGetValue(key, out var state) ? state : null;
    }

    /// <summary>
    /// Removes pending state.
    /// </summary>
    public bool TryRemovePending(string key, out PendingLoginState? state) => _pending.TryRemove(key, out state);

    /// <summary>
    /// Stores a short-lived session token after IdP success.
    /// </summary>
    public string StoreSession(AuthorizedSession session)
    {
        PurgeExpired();
        var token = Guid.NewGuid().ToString("N");
        _sessions[token] = session;
        return token;
    }

    /// <summary>
    /// Consumes a one-time session token.
    /// </summary>
    public AuthorizedSession? ConsumeSession(string token)
    {
        PurgeExpired();
        return _sessions.TryRemove(token, out var session) ? session : null;
    }

    /// <summary>
    /// Peeks session token for Quick Connect retries.
    /// </summary>
    public AuthorizedSession? PeekSession(string token)
    {
        PurgeExpired();
        return _sessions.TryGetValue(token, out var session) ? session : null;
    }

  /// <summary>
  /// Invalidates a session token after successful Quick Connect.
  /// </summary>
  public void InvalidateSession(string token) => _sessions.TryRemove(token, out _);

    private void PurgeExpired()
    {
        var cutoff = DateTimeOffset.UtcNow - StateExpiry;
        foreach (var kvp in _pending)
        {
            if (kvp.Value.CreatedUtc < cutoff)
            {
                _pending.TryRemove(kvp.Key, out _);
            }
        }

        foreach (var kvp in _sessions)
        {
            if (kvp.Value.CreatedUtc < cutoff)
            {
                _sessions.TryRemove(kvp.Key, out _);
            }
        }
    }
}

/// <summary>
/// OIDC login state between Start and Callback.
/// </summary>
public sealed class PendingLoginState
{
    public PendingLoginState(AuthorizeState authorizeState, bool quickConnect)
    {
        AuthorizeState = authorizeState;
        QuickConnect = quickConnect;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public AuthorizeState AuthorizeState { get; }

    public bool QuickConnect { get; }

    public DateTimeOffset CreatedUtc { get; }

    public bool Valid { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public bool IsAdmin { get; set; }
}

/// <summary>
/// Authorized identity awaiting Jellyfin session minting.
/// </summary>
public sealed class AuthorizedSession
{
    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public bool IsAdmin { get; set; }

    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    public Guid? SyncedUserId { get; set; }
}