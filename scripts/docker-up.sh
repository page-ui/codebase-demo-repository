#!/usr/bin/env bash
set -euo pipefail

TIMEOUT_SECONDS="${1:-900}"

if ! [[ "$TIMEOUT_SECONDS" =~ ^[0-9]+$ ]]; then
  echo "Usage: $0 [timeout_seconds]" >&2
  exit 2
fi

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname -- "$SCRIPT_DIR")"
cd "$ROOT_DIR"

docker compose up --build -d

DEADLINE=$((SECONDS + TIMEOUT_SECONDS))

while (( SECONDS < DEADLINE )); do
  mapfile -t SERVICES < <(docker compose config --services)
  PENDING=()

  for SERVICE in "${SERVICES[@]}"; do
    mapfile -t CONTAINER_IDS < <(docker compose ps -q "$SERVICE")

    if (( ${#CONTAINER_IDS[@]} == 0 )); then
      PENDING+=("${SERVICE}:not-created")
      continue
    fi

    for CONTAINER_ID in "${CONTAINER_IDS[@]}"; do
      [[ -z "$CONTAINER_ID" ]] && continue

      STATE="$(docker inspect --format '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}no-health{{end}}|{{.State.ExitCode}}' "$CONTAINER_ID")"
      IFS='|' read -r STATUS HEALTH EXIT_CODE <<< "$STATE"

      if [[ "$STATUS" == "exited" && "$EXIT_CODE" == "0" ]]; then
        continue
      fi

      if [[ "$STATUS" != "running" ]]; then
        PENDING+=("${SERVICE}:${STATUS}")
        continue
      fi

      if [[ "$HEALTH" != "no-health" && "$HEALTH" != "healthy" ]]; then
        PENDING+=("${SERVICE}:${HEALTH}")
      fi
    done
  done

  if (( ${#PENDING[@]} == 0 )); then
    docker compose ps
    echo "All Page UI services are up and healthy."
    exit 0
  fi

  printf 'Waiting for services: %s\n' "$(IFS=', '; echo "${PENDING[*]}")"
  sleep 5
done

docker compose ps
echo "Timed out after ${TIMEOUT_SECONDS} seconds waiting for Page UI services to become healthy." >&2
exit 1
