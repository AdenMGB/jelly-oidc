using System.Net.Http.Json;
using Jellyfin.Plugin.JellyOidc.Helpers;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JellyOidc.Api;

/// <summary>
/// Admin helpers for OIDC configuration.
/// </summary>
[ApiController]
[Route("JellyOidc/Config")]
[Authorize(Policy = "RequiresElevation")]
public class ConfigController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ConfigController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("TestDiscovery")]
    public async Task<IActionResult> TestDiscovery()
    {
        var config = Plugin.Instance.Configuration;
        if (string.IsNullOrWhiteSpace(config.Authority))
        {
            return BadRequest(new { ok = false, message = "Authority is empty" });
        }

        var authority = config.Authority.TrimEnd('/');
        var discoveryUrl = authority.Contains("/.well-known/", StringComparison.Ordinal)
            ? authority
            : $"{authority}/.well-known/openid-configuration";

        using var client = _httpClientFactory.CreateClient();
        try
        {
            var doc = await client.GetFromJsonAsync<Dictionary<string, object>>(discoveryUrl).ConfigureAwait(false);
            return Ok(new
            {
                ok = true,
                discoveryUrl,
                issuer = doc != null && doc.TryGetValue("issuer", out var issuer) ? issuer : null,
                redirectUri = RequestUrlHelper.GetBaseUrl(Request, config.RedirectBaseOverride) + "/JellyOidc/OIDC/Callback",
                quickConnectUrl = RequestUrlHelper.GetBaseUrl(Request, config.RedirectBaseOverride) + "/JellyOidc/OIDC/QuickConnect"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { ok = false, message = ex.Message });
        }
    }

    [HttpPost("ApplyLoginExperience")]
    public IActionResult ApplyLoginExperience()
    {
        Services.LoginExperienceService.ApplyBranding(
            HttpContext.RequestServices.GetRequiredService<IServerConfigurationManager>(),
            Plugin.Instance.Configuration);
        return Ok(new { ok = true });
    }
}