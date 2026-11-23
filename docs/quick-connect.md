# Quick Connect (mobile / TV)

1. Enable **Quick Connect** in Jellyfin: *Dashboard → General → Quick Connect*.
2. On the native app, open **Quick Connect** and note the 6-digit code.
3. In a browser, open: `http://<jellyfin>/JellyOidc/OIDC/QuickConnect`
4. Sign in with Authentik, then enter the code.
5. The native app should complete pairing automatically.

Direct in-app OIDC without Quick Connect is not supported by Jellyfin client APIs in v1.