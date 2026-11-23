using Jellyfin.Plugin.JellyOidc.Configuration;
using Jellyfin.Plugin.JellyOidc.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Branding;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyOidc.Services;

/// <summary>
/// Applies branding (login disclaimer, CSS) and optional web index patch for login.js.
/// </summary>
public sealed class LoginExperienceService : IHostedService
{
    private const string ScriptMarker = "<!-- jelly-oidc-login-js -->";

    private readonly IServerConfigurationManager _configurationManager;
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<LoginExperienceService> _logger;

    public LoginExperienceService(
        IServerConfigurationManager configurationManager,
        IApplicationPaths applicationPaths,
        ILogger<LoginExperienceService> logger)
    {
        _configurationManager = configurationManager;
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            ApplyBranding(_configurationManager, Plugin.Instance.Configuration);
            TryInjectLoginScript(Plugin.Instance.Configuration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply Jelly-OIDC login experience");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Updates branding configuration for SSO affordances.
    /// </summary>
    public static void ApplyBranding(IServerConfigurationManager configurationManager, PluginConfiguration config)
    {
        if (!config.SyncLoginDisclaimer)
        {
            return;
        }

        var branding = configurationManager.GetConfiguration<BrandingOptions>("branding");
        if (!config.Enabled || (!config.ShowLoginButton && !config.OidcOnly))
        {
            if (branding.LoginDisclaimer?.Contains("jelly-oidc", StringComparison.OrdinalIgnoreCase) == true)
            {
                branding.LoginDisclaimer = null;
                configurationManager.SaveConfiguration("branding", branding);
            }

            return;
        }

        var label = LoginUiHelper.GetSignInButtonLabel(config);
        branding.LoginDisclaimer =
            "<!-- jelly-oidc -->"
            + $"<p class=\"jelly-oidc-disclaimer\">{label}</p>"
            + (config.OidcOnly
                ? "<p><em>Password sign-in is hidden. Use SSO or keep a local admin for break-glass access.</em></p>"
                : string.Empty);

        var cssImport = "@import url('/JellyOidc/ui/login.css');";
        if (config.UseJellyfinThemeVars && branding.CustomCss?.Contains(cssImport, StringComparison.Ordinal) != true)
        {
            branding.CustomCss = string.Join('\n', new[] { branding.CustomCss, cssImport }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        configurationManager.SaveConfiguration("branding", branding);
    }

    private void TryInjectLoginScript(PluginConfiguration config)
    {
        if (!config.InjectLoginScript || !config.Enabled)
        {
            return;
        }

        var indexPath = Path.Combine(_applicationPaths.WebPath, "index.html");
        if (!File.Exists(indexPath))
        {
            _logger.LogDebug("Web index not found at {Path}", indexPath);
            return;
        }

        var html = File.ReadAllText(indexPath);
        if (html.Contains(ScriptMarker, StringComparison.Ordinal))
        {
            return;
        }

        const string tag = ScriptMarker + "<script defer src=\"/JellyOidc/ui/login.js\"></script>";
        if (html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
        {
            html = html.Replace("</head>", tag + "</head>", StringComparison.OrdinalIgnoreCase);
            File.WriteAllText(indexPath, html);
            _logger.LogInformation("Injected Jelly-OIDC login script into web index");
        }
    }
}