using Jellyfin.Plugin.JellyOidc.Auth;
using Jellyfin.Plugin.JellyOidc.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.JellyOidc;

/// <summary>
/// Registers plugin services in DI.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<OidcStateStore>();
        serviceCollection.AddSingleton<UserProvisioner>();
        serviceCollection.AddHostedService<LoginExperienceService>();
    }
}