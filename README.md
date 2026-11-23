# jelly-oidc

**Jelly-OIDC** is a Jellyfin plugin that adds OpenID Connect (OIDC) single sign-on, with defaults aimed at [Authentik](https://goauthentik.io/). It supports role-based access, OIDC-only web login, branded admin settings, and Quick Connect for mobile/TV clients.

## Features

- Authorization Code + PKCE OIDC login for the web UI
- Authentik-friendly `groups` role claim parsing (JSON arrays and object keys)
- Admin configuration page in the Jellyfin dashboard
- **OIDC-only** mode (hide password fields / auto-redirect)
- Login disclaimer + optional `login.js` injection aligned with Jellyfin theming
- **Quick Connect** flow for native clients
- Docker Compose dev stack (Jellyfin 10.11 + Authentik)

## Build

```bash
./scripts/build-plugin.sh
```

Output: `build/plugins/JellyOidc/` (copy or mount into Jellyfin `plugins/`).

## Local development stack

**Recommended on Cloud Agents** (host networking — works when Docker bridge traffic is restricted):

```bash
bash scripts/build-plugin.sh
docker compose -f docker/docker-compose.e2e.yml up -d
```

**Full Authentik stack** (requires working container-to-container networking):

```bash
./scripts/dev-up.sh
```

| Service | URL |
| --- | --- |
| Jellyfin | http://127.0.0.1:8096 |
| Dex (e2e OIDC) | http://127.0.0.1:5556/dex |
| Authentik (full stack) | http://127.0.0.1:9000 |

Default Authentik bootstrap (compose): `admin@example.com` / `authentik-admin` (change after first login).

See [docs/authentik.md](docs/authentik.md) and [docs/quick-connect.md](docs/quick-connect.md).

### Blueprint client (dev)

The Authentik blueprint provisions:

- Client ID: `jellyfin`
- Client secret: `jellyfin-dev-secret-change-me`
- Redirect URI: `http://127.0.0.1:8096/JellyOidc/OIDC/Callback`
- Groups: `jellyfin_users`, `jellyfin_admins`

Configure the plugin **Authority** to the issuer shown in Authentik for the Jellyfin application (typically `http://127.0.0.1:9000/application/o/jellyfin/`).

## OIDC routes

| Method | Path |
| --- | --- |
| GET | `/JellyOidc/OIDC/Start` |
| GET | `/JellyOidc/OIDC/Callback` |
| POST | `/JellyOidc/OIDC/Complete` |
| GET | `/JellyOidc/OIDC/QuickConnect` |
| POST | `/JellyOidc/OIDC/QuickConnect/Authorize` |

## License

MIT (add license file as needed).