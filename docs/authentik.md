# Authentik setup for Jelly-OIDC

## Redirect URIs

Register these in your OAuth2/OIDC provider (Authentik application):

- `http://127.0.0.1:8096/JellyOidc/OIDC/Callback`

For production, use your public Jellyfin URL with the same path.

## Recommended scopes

- `openid`
- `profile`
- `email`

Add the **groups** scope mapping in Authentik so the `groups` claim is available for Jellyfin RBAC.

## Groups

Create (or map) these groups in Authentik:

| Group | Jellyfin use |
| --- | --- |
| `jellyfin_users` | Allowed to sign in |
| `jellyfin_admins` | Jellyfin administrator |

In the plugin admin UI, set **Allowed roles** to `jellyfin_users` and **Admin roles** to `jellyfin_admins`.

## Plugin settings (Authority)

Use the Authentik **OpenID Configuration issuer** URL, for example:

`http://127.0.0.1:9000/application/o/jellyfin/`

Copy **Client ID** and **Client secret** from the Authentik provider into the Jelly-OIDC plugin page.

## Break-glass admin

Always keep at least one **local** Jellyfin administrator before enabling SSO admin mapping.