# Creates the first EF Core migration (if none exists) and applies it to the configured database.
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    dotnet tool install --global dotnet-ef
}

$existing = Get-ChildItem "src/TaskFlow.Infrastructure/Persistence/Migrations" -Filter "*_*.cs" -ErrorAction SilentlyContinue
if (-not $existing) {
    dotnet ef migrations add InitialCreate `
        --project src/TaskFlow.Infrastructure `
        --startup-project src/TaskFlow.API `
        --output-dir Persistence/Migrations
}

dotnet ef database update `
    --project src/TaskFlow.Infrastructure `
    --startup-project src/TaskFlow.API
