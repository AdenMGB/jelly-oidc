using System.Collections.Generic;
using Jellyfin.Plugin.JellyOidc.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.JellyOidc;

/// <summary>
/// Jelly-OIDC authentication plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Stable plugin identifier.
    /// </summary>
    public const string PluginId = "6524d89d-6e89-4e98-b7df-fc3d1f824f18";

    private static Plugin? _instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        _instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Jelly-OIDC";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse(PluginId);

    /// <summary>
    /// Gets the running plugin singleton.
    /// </summary>
    public static Plugin Instance => _instance ?? throw new InvalidOperationException("Plugin not initialized");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = string.Concat(GetType().Namespace, ".Configuration.configPage.html")
        };
    }
}