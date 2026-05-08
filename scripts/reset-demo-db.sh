#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

CONTAINER_NAME="${EQUIPRENTAL_DB_CONTAINER:-equiprental-db}"
SA_PASSWORD="${EQUIPRENTAL_DB_PASSWORD:-Admin123456!}"
WAIT_SECONDS="${EQUIPRENTAL_DB_WAIT_SECONDS:-60}"

cd "${ROOT_DIR}"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

require_command docker
require_command dotnet

if ! docker ps -a --format '{{.Names}}' | grep -Fxq "${CONTAINER_NAME}"; then
  cat >&2 <<EOF
SQL Server container '${CONTAINER_NAME}' was not found.

Create it first:
docker run \\
  -e "ACCEPT_EULA=Y" \\
  -e "SA_PASSWORD=${SA_PASSWORD}" \\
  -p 1433:1433 \\
  --name ${CONTAINER_NAME} \\
  -d mcr.microsoft.com/mssql/server:2022-latest
EOF
  exit 1
fi

if ! docker ps --format '{{.Names}}' | grep -Fxq "${CONTAINER_NAME}"; then
  echo "Starting SQL Server container '${CONTAINER_NAME}'..."
  docker start "${CONTAINER_NAME}" >/dev/null
fi

find_sqlcmd() {
  docker exec "${CONTAINER_NAME}" sh -lc '
    if command -v sqlcmd >/dev/null 2>&1; then
      command -v sqlcmd
    elif [ -x /opt/mssql-tools18/bin/sqlcmd ]; then
      echo /opt/mssql-tools18/bin/sqlcmd
    elif [ -x /opt/mssql-tools/bin/sqlcmd ]; then
      echo /opt/mssql-tools/bin/sqlcmd
    fi
  ' | tr -d '\r'
}

SQLCMD="$(find_sqlcmd)"
if [ -z "${SQLCMD}" ]; then
  echo "Could not find sqlcmd inside '${CONTAINER_NAME}'." >&2
  exit 1
fi

echo "Waiting for SQL Server to accept connections..."
deadline=$((SECONDS + WAIT_SECONDS))
until docker exec "${CONTAINER_NAME}" "${SQLCMD}" -C -S localhost -U sa -P "${SA_PASSWORD}" -Q "SELECT 1" >/dev/null 2>&1; do
  if [ "${SECONDS}" -ge "${deadline}" ]; then
    echo "Timed out waiting for SQL Server after ${WAIT_SECONDS}s." >&2
    exit 1
  fi
  sleep 2
done

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Development}"

echo "Dropping EquipmentRentalDb..."
dotnet ef database drop --force

echo "Applying migrations..."
dotnet ef database update

echo "Seeding demo data..."
dotnet run -- --seed-only

cat <<'EOF'

Demo database has been reset.

Default accounts:
  admin@equiprental.com / Admin@123456
  demo.deviceadmin@equiprental.com / Demo@123456
  demo.dispatcher@equiprental.com / Demo@123456
  demo.projectlead@equiprental.com / Demo@123456
  demo.safetyofficer@equiprental.com / Demo@123456
  demo.auditor@equiprental.com / Demo@123456

Uploads/ was not cleaned. Start the app with:
  dotnet run
EOF
