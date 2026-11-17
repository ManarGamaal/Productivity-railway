#!/usr/bin/env bash
# Creates the first EF Core migration (if none exists) and applies it to the configured database.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet tool restore >/dev/null 2>&1 || true
command -v dotnet-ef >/dev/null 2>&1 || dotnet tool install --global dotnet-ef

if ! ls src/TaskFlow.Infrastructure/Persistence/Migrations/*_*.cs >/dev/null 2>&1; then
  dotnet ef migrations add InitialCreate \
    --project src/TaskFlow.Infrastructure \
    --startup-project src/TaskFlow.API \
    --output-dir Persistence/Migrations
fi

dotnet ef database update \
  --project src/TaskFlow.Infrastructure \
  --startup-project src/TaskFlow.API
