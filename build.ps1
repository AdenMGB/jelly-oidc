cd C:\Users\Aden\Documents\jelly-oidc

dotnet build .\Jellyfin.Plugin.JellyOidc\Jellyfin.Plugin.JellyOidc.csproj -c Release

$out = ".\build\plugins\JellyOidc"
New-Item -ItemType Directory -Force -Path $out | Out-Null
Remove-Item "$out\*" -Recurse -Force -ErrorAction SilentlyContinue

Copy-Item ".\Jellyfin.Plugin.JellyOidc\bin\Release\net9.0\Jellyfin.Plugin.JellyOidc.dll" $out
Copy-Item ".\Jellyfin.Plugin.JellyOidc\bin\Release\net9.0\Configuration" $out -Recurse

$nuget = "$env:USERPROFILE\.nuget\packages"
Copy-Item "$nuget\identitymodel.oidcclient\6.0.0\lib\net6.0\IdentityModel.OidcClient.dll" $out
Copy-Item "$nuget\identitymodel\7.0.0\lib\net6.0\IdentityModel.dll" $out

@'
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
'@ | Set-Content -Encoding utf8 "$out\meta.json"

Write-Host "Built folder: $((Resolve-Path $out).Path)"