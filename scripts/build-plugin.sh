#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="${HOME}/.dotnet:${PATH}"
OUT="$ROOT/build/plugins/JellyOidc"
dotnet build "$ROOT/Jellyfin.Plugin.JellyOidc/Jellyfin.Plugin.JellyOidc.csproj" -c Release
mkdir -p "$OUT"
rm -rf "${OUT:?}/"*
cp "$ROOT/Jellyfin.Plugin.JellyOidc/bin/Release/net9.0/Jellyfin.Plugin.JellyOidc.dll" "$OUT/"
cp -r "$ROOT/Jellyfin.Plugin.JellyOidc/bin/Release/net9.0/Configuration" "$OUT/"
cp "$HOME/.nuget/packages/identitymodel.oidcclient/6.0.0/lib/net6.0/IdentityModel.OidcClient.dll" "$OUT/"
cp "$HOME/.nuget/packages/identitymodel/7.0.0/lib/net6.0/IdentityModel.dll" "$OUT/"
cat > "$OUT/meta.json" <<'EOF'
{
  "guid": "6524d89d-6e89-4e98-b7df-fc3d1f824f18",
  "name": "Jelly-OIDC",
  "description": "OpenID Connect SSO for Jellyfin",
  "overview": "Authentik-focused OIDC authentication with Quick Connect",
  "owner": "AdenMGB",
  "category": "Authentication",
  "version": "0.1.0.0",
  "targetAbi": "10.11.0.0",
  "timestamp": "2026-03-23T00:00:00.0000000Z",
  "autoUpdate": false
}
EOF
echo "Published to $OUT"