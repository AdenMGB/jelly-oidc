using Jellyfin.Plugin.JellyOidc.Configuration;
using Jellyfin.Plugin.JellyOidc.Helpers;
using Xunit;

namespace Jellyfin.Plugin.JellyOidc.Tests;

public class LoginUiHelperTests
{
    [Fact]
    public void Uses_display_name_when_button_label_empty()
    {
        var config = new PluginConfiguration { DisplayName = "Authentik", ButtonLabel = string.Empty };
        Assert.Equal("Sign in with Authentik", LoginUiHelper.GetSignInButtonLabel(config));
    }

    [Fact]
    public void Supports_provider_placeholder()
    {
        var config = new PluginConfiguration { DisplayName = "Dex", ButtonLabel = "Continue with {provider}" };
        Assert.Equal("Continue with Dex", LoginUiHelper.GetSignInButtonLabel(config));
    }
}