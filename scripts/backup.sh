#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
set -a; source .env; set +a
export DOMAIN="${DOMAIN:-localhost}"
export EMAIL="${EMAIL:-admin@example.com}"

# Use COMPOSE_FILE=docker-compose.prod.yml to back up the production stack.
COMPOSE=(docker compose)
if [[ -n "${COMPOSE_FILE:-}" ]]; then
  COMPOSE+=( -f "$COMPOSE_FILE" )
fi

mkdir -p backups
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
FILE="backups/assettagbd_${TIMESTAMP}.dump.gz"

"${COMPOSE[@]}" exec -T db \
  pg_dump -U "${POSTGRES_USER:-postgres}" -d "${POSTGRES_DB:-assettagbd}" -Fc \
  | gzip > "$FILE"

# Keep two weeks of nightly backups.
find backups -type f -name '*.dump.gz' -mtime +14 -delete

echo "Backup created: $FILE"
