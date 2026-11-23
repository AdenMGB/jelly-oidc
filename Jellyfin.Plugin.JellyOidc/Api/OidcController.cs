using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using IdentityModel.OidcClient;
using Jellyfin.Plugin.JellyOidc.Configuration;
using Jellyfin.Plugin.JellyOidc.Helpers;
using Jellyfin.Plugin.JellyOidc.Services;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyOidc.Api;

/// <summary>
/// OIDC login and Quick Connect API.
/// </summary>
[ApiController]
[Route("JellyOidc/OIDC")]
public class OidcController : ControllerBase
{
    private const string JellyfinWebDeviceIdStorageKey = "_deviceId2";
    private const string JellyfinWebCredentialsStorageKey = "jellyfin_credentials";
    private const string JellyfinWebAppName = "Jellyfin Web";
    private const string JellyfinWebAppVersion = "10.11.0";

    private readonly OidcStateStore _stateStore;
    private readonly UserProvisioner _userProvisioner;
    private readonly ISessionManager _sessionManager;
    private readonly IQuickConnect _quickConnect;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<OidcController> _logger;

    public OidcController(
        OidcStateStore stateStore,
        UserProvisioner userProvisioner,
        ISessionManager sessionManager,
        IQuickConnect quickConnect,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILogger<OidcController> logger)
    {
        _stateStore = stateStore;
        _userProvisioner = userProvisioner;
        _sessionManager = sessionManager;
        _quickConnect = quickConnect;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    [HttpGet("Start")]
    [AllowAnonymous]
    public Task<IActionResult> Start() => BeginAuthorizeAsync(quickConnect: false);

    [HttpGet("QuickConnect")]
    [AllowAnonymous]
    public Task<IActionResult> QuickConnectStart() => BeginAuthorizeAsync(quickConnect: true);

    [HttpGet("Callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? state)
    {
        var config = Plugin.Instance.Configuration;
        if (!config.Enabled)
        {
            return BadRequest("Jelly-OIDC is disabled");
        }

        if (string.IsNullOrEmpty(state))
        {
            return BadRequest("Missing state");
        }

        if (!_stateStore.TryRemovePending(state, out var pending) || pending == null)
        {
            return BadRequest("Invalid or expired state");
        }

        var client = CreateOidcClient(config);
        var result = await client.ProcessResponseAsync(Request.QueryString.Value, pending.AuthorizeState).ConfigureAwait(false);
        if (result.IsError)
        {
            _logger.LogWarning("OIDC error: {Error} {Description}", result.Error, result.ErrorDescription);
            return BadRequest($"Authentication failed: {result.Error}");
        }

        var claims = result.User.Claims;
        var username = ClaimParser.ResolveUsername(claims, config.UsernameClaim, "email", "preferred_username")
            ?? throw new InvalidOperationException("No username claim found");

        var roles = ClaimParser.ExtractRoles(config.RoleClaim, claims);
        var isAdmin = roles.Any(r => config.AdminRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

        if (config.EnableAuthorization && config.AllowedRoles.Length > 0)
        {
            var allowed = roles.Any(r => config.AllowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase)) || isAdmin;
            if (!allowed)
            {
                return Forbid();
            }
        }

        var displayName = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? username;
        var subject = claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? username;

        var sessionToken = _stateStore.StoreSession(new AuthorizedSession
        {
            Username = username,
            DisplayName = displayName,
            Subject = subject,
            Roles = roles,
            IsAdmin = isAdmin
        });

        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Content-Security-Policy"] =
            $"default-src 'none'; script-src 'nonce-{nonce}'; style-src 'nonce-{nonce}'; connect-src 'self'; frame-ancestors 'none'";

        var pageData = EmbeddedPage.SerializeData(new
        {
            token = sessionToken,
            deviceIdKey = JellyfinWebDeviceIdStorageKey,
            credentialsKey = JellyfinWebCredentialsStorageKey,
            appName = JellyfinWebAppName,
            appVersion = JellyfinWebAppVersion
        });

        var html = pending.QuickConnect
            ? EmbeddedPage.Render("QuickConnectPage.html", new Dictionary<string, string>
            {
                ["__CSP_NONCE__"] = nonce,
                ["__DATA_JSON__"] = pageData
            })
            : EmbeddedPage.Render("CallbackPage.html", new Dictionary<string, string>
            {
                ["__CSP_NONCE__"] = nonce,
                ["__DATA_JSON__"] = pageData
            });

        return Content(html, MediaTypeNames.Text.Html);
    }

    [HttpPost("Complete")]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<IActionResult> Complete([FromBody] ClientAuthRequest request)
    {
        var config = Plugin.Instance.Configuration;
        var session = _stateStore.ConsumeSession(request.Token ?? string.Empty);
        if (session == null)
        {
            return Unauthorized();
        }

        try
        {
            var userId = await _userProvisioner.ProvisionAsync(session, config).ConfigureAwait(false);
            var authRequest = new AuthenticationRequest
            {
                UserId = userId,
                Username = session.Username,
                App = request.AppName ?? JellyfinWebAppName,
                AppVersion = request.AppVersion ?? JellyfinWebAppVersion,
                DeviceId = request.DeviceId ?? Guid.NewGuid().ToString("N"),
                DeviceName = request.DeviceName ?? "Web Browser",
                RemoteEndPoint = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var authResult = await _sessionManager.AuthenticateDirect(authRequest).ConfigureAwait(false);
            return Ok(authResult);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("OIDC provision denied: {Message}", ex.Message);
            return Forbid();
        }
    }

    [HttpPost("QuickConnect/Authorize")]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<IActionResult> QuickConnectAuthorize([FromBody] QuickConnectAuthorizeRequest request)
    {
        var config = Plugin.Instance.Configuration;
        var session = _stateStore.PeekSession(request.Token ?? string.Empty);
        if (session == null)
        {
            return Unauthorized();
        }

        if (!_quickConnect.IsEnabled)
        {
            return BadRequest("Quick Connect is disabled. Enable it in Dashboard → General.");
        }

        var code = (request.Code ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("Missing Quick Connect code");
        }

        Guid userId;
        if (session.SyncedUserId is Guid synced)
        {
            userId = synced;
        }
        else
        {
            try
            {
                userId = await _userProvisioner.ProvisionAsync(session, config).ConfigureAwait(false);
                session.SyncedUserId = userId;
            }
            catch (InvalidOperationException ex)
            {
                return Forbid(ex.Message);
            }
        }

        try
        {
            var authorized = await _quickConnect.AuthorizeRequest(userId, code).ConfigureAwait(false);
            if (!authorized)
            {
                return BadRequest("Quick Connect code rejected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick Connect authorize failed");
            return BadRequest("Quick Connect authorization failed");
        }

        _stateStore.InvalidateSession(request.Token ?? string.Empty);
        return Ok(new { success = true });
    }

    private async Task<IActionResult> BeginAuthorizeAsync(bool quickConnect)
    {
        var config = Plugin.Instance.Configuration;
        if (!config.Enabled)
        {
            return BadRequest("Jelly-OIDC is disabled");
        }

        if (string.IsNullOrWhiteSpace(config.Authority) || string.IsNullOrWhiteSpace(config.ClientId))
        {
            return BadRequest("OIDC is not configured");
        }

        var client = CreateOidcClient(config);
        var authorizeState = await client.PrepareLoginAsync().ConfigureAwait(false);
        var stateKey = authorizeState.State;
        _stateStore.StorePending(stateKey, new PendingLoginState(authorizeState, quickConnect));
        return Redirect(authorizeState.StartUrl);
    }

    private OidcClient CreateOidcClient(PluginConfiguration config)
    {
        var redirectUri = RequestUrlHelper.GetBaseUrl(Request, config.RedirectBaseOverride)
            + "/JellyOidc/OIDC/Callback";

        var scopes = string.Join(' ', config.Scopes.Concat(new[] { "openid" }).Distinct());
        var options = new OidcClientOptions
        {
            Authority = config.Authority.Trim(),
            ClientId = config.ClientId.Trim(),
            ClientSecret = config.ClientSecret?.Trim(),
            RedirectUri = redirectUri,
            Scope = scopes,
            LoggerFactory = _loggerFactory,
            HttpClientFactory = _ => _httpClientFactory.CreateClient(),
            Policy =
            {
                Discovery =
                {
                    RequireHttps = !config.DisableHttpsDiscovery,
                    ValidateIssuerName = true,
                    ValidateEndpoints = true
                }
            }
        };

        return new OidcClient(options);
    }
}

/// <summary>
/// Client request to mint a Jellyfin session after callback.
/// </summary>
public class ClientAuthRequest
{
    public string? Token { get; set; }

    public string? DeviceId { get; set; }

    public string? DeviceName { get; set; }

    public string? AppName { get; set; }

    public string? AppVersion { get; set; }
}

/// <summary>
/// Quick Connect authorize body.
/// </summary>
public class QuickConnectAuthorizeRequest
{
    public string? Token { get; set; }

    public string? Code { get; set; }
}