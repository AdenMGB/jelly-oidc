using Jellyfin.Plugin.JellyOidc.Helpers;
using Xunit;

namespace Jellyfin.Plugin.JellyOidc.Tests;

public class ClaimParserTests
{
    [Theory]
    [InlineData("[\"jellyfin_users\",\"jellyfin_admins\"]", 2)]
    [InlineData("{\"jellyfin_users\":{},\"jellyfin_admins\":{}}", 2)]
    [InlineData("jellyfin_users,jellyfin_admins", 2)]
  [InlineData("jellyfin_users", 1)]
    public void ParseRoleValue_ParsesCommonFormats(string raw, int expectedCount)
    {
        var roles = ClaimParser.ParseRoleValue(raw);
        Assert.Equal(expectedCount, roles.Count);
    }
}