#!/usr/bin/env bash
# Crea las categorías SERVIDORES y CLOUD a través de la API (POST /Category), tal como pide el
# enunciado: no se insertan directamente en la base de datos.
#
# Uso:
#   ./scripts/seed.sh
#   API_URL=http://localhost:5152 ADMIN_PASSWORD='...' ./scripts/seed.sh
#
# Variables:
#   API_URL         URL base de la API          (por defecto http://localhost:8080, la de docker compose)
#   ADMIN_USER      usuario para el login       (por defecto admin)
#   ADMIN_PASSWORD  contraseña del usuario      (obligatoria; la misma que SeedAdmin__Password)
#   BULK_COUNT      si se define, además encola una carga masiva de N productos (p. ej. 100000)
set -euo pipefail

API_URL="${API_URL:-http://localhost:8080}"
ADMIN_USER="${ADMIN_USER:-admin}"
: "${ADMIN_PASSWORD:?Define ADMIN_PASSWORD con la contraseña del usuario administrador}"

echo "==> Login como '${ADMIN_USER}' en ${API_URL}"
login_response=$(curl -sS --fail-with-body -X POST "${API_URL}/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${ADMIN_USER}\",\"password\":\"${ADMIN_PASSWORD}\"}")

# Extrae el token sin depender de jq.
TOKEN=$(printf '%s' "$login_response" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [ -z "$TOKEN" ]; then
  echo "No se pudo obtener el token. Respuesta: $login_response" >&2
  exit 1
fi
echo "    token obtenido"

create_category() {
  local name="$1" photo="$2" status
  status=$(curl -sS -o /tmp/seed_category.json -w "%{http_code}" -X POST "${API_URL}/Category" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"${name}\",\"photoUrl\":\"${photo}\"}")

  case "$status" in
    201) echo "==> Categoría ${name} creada: $(cat /tmp/seed_category.json)" ;;
    409) echo "==> Categoría ${name} ya existía (409), se omite" ;;
    *)   echo "Error creando ${name} (HTTP ${status}): $(cat /tmp/seed_category.json)" >&2; exit 1 ;;
  esac
}

create_category "SERVIDORES" "https://picsum.photos/seed/servidores/640/480"
create_category "CLOUD" "https://picsum.photos/seed/cloud/640/480"

if [ -n "${BULK_COUNT:-}" ]; then
  echo "==> Encolando carga masiva de ${BULK_COUNT} productos"
  bulk_response=$(curl -sS --fail-with-body -X POST "${API_URL}/Product/bulk" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{\"count\":${BULK_COUNT}}")
  echo "    ${bulk_response}"
  JOB_ID=$(printf '%s' "$bulk_response" | sed -n 's/.*"jobId":"\([^"]*\)".*/\1/p')

  while true; do
    status_response=$(curl -sS --fail-with-body "${API_URL}/Product/bulk/${JOB_ID}" -H "Authorization: Bearer ${TOKEN}")
    status=$(printf '%s' "$status_response" | sed -n 's/.*"status":"\([^"]*\)".*/\1/p')
    processed=$(printf '%s' "$status_response" | sed -n 's/.*"processedRecords":\([0-9]*\).*/\1/p')
    echo "    estado=${status} procesados=${processed}/${BULK_COUNT}"
    case "$status" in
      Completed) break ;;
      Failed) echo "    ${status_response}" >&2; exit 1 ;;
    esac
    sleep 1
  done
fi

echo "==> Listo"
