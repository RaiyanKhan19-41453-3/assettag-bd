#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
set -a; source .env; set +a
export DOMAIN="${DOMAIN:-localhost}"
export EMAIL="${EMAIL:-admin@example.com}"

COMPOSE=(docker compose)
if [[ -n "${COMPOSE_FILE:-}" ]]; then
  COMPOSE+=( -f "$COMPOSE_FILE" )
fi

FILE="${1:?Usage: scripts/restore.sh backups/<file>.dump.gz}"
if [[ ! -f "$FILE" ]]; then
  echo "Backup file not found: $FILE" >&2
  exit 1
fi

if [[ "${ASSUME_YES:-}" != "1" ]]; then
  read -rp "This REPLACES the current database. Type YES to continue: " CONFIRM
  [[ "$CONFIRM" == "YES" ]] || { echo "Aborted."; exit 1; }
fi

# Stop writers, recreate a clean schema (so stale tables can't survive an
# older dump), then restore atomically. Always bring the API back.
"${COMPOSE[@]}" stop api

restart_api() {
  "${COMPOSE[@]}" start api >/dev/null 2>&1 || true
}
trap restart_api EXIT

"${COMPOSE[@]}" exec -T db \
  psql -U "${POSTGRES_USER:-postgres}" -d "${POSTGRES_DB:-assettagbd}" \
  -c 'DROP SCHEMA public CASCADE; CREATE SCHEMA public;' >/dev/null

gunzip -c "$FILE" | "${COMPOSE[@]}" exec -T db \
  pg_restore -U "${POSTGRES_USER:-postgres}" -d "${POSTGRES_DB:-assettagbd}" \
  --single-transaction --exit-on-error --no-owner

trap - EXIT
restart_api

echo "Restore complete from: $FILE"
