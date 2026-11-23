#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
"$ROOT/scripts/build-plugin.sh"
cd "$ROOT/docker"
if [ "${JELLY_OIDC_STACK:-e2e}" = "authentik" ]; then
  sudo docker compose pull
  sudo docker compose up -d
else
  sudo docker compose -f docker-compose.e2e.yml pull
  sudo docker compose -f docker-compose.e2e.yml up -d
fi
echo "Waiting for Jellyfin..."
for i in $(seq 1 60); do
  if curl -fsS http://127.0.0.1:8096/health >/dev/null 2>&1; then
    echo "Jellyfin is up"
    break
  fi
  sleep 2
done
echo "Authentik UI: http://127.0.0.1:9000/if/flow/initial-setup/ (first run) or http://127.0.0.1:9000"
echo "Jellyfin: http://127.0.0.1:8096"