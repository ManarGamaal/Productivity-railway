# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project files first so "dotnet restore" is cached until dependencies change.
COPY TaskFlow.sln ./
COPY src/TaskFlow.Domain/TaskFlow.Domain.csproj                   src/TaskFlow.Domain/
COPY src/TaskFlow.Application/TaskFlow.Application.csproj         src/TaskFlow.Application/
COPY src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj   src/TaskFlow.Infrastructure/
COPY src/TaskFlow.API/TaskFlow.API.csproj                         src/TaskFlow.API/
RUN dotnet restore TaskFlow.sln

COPY . .

# Safety net: if no EF Core migration has been committed yet, generate the initial one here
# so the first deployment still creates the schema. Commit a real migration for production use.
RUN if ! ls src/TaskFlow.Infrastructure/Persistence/Migrations/*_*.cs >/dev/null 2>&1; then \
      dotnet tool install --global dotnet-ef --version "8.*" && \
      SKIP_DB_MIGRATION=true /root/.dotnet/tools/dotnet-ef migrations add InitialCreate \
        --project src/TaskFlow.Infrastructure \
        --startup-project src/TaskFlow.API \
        --output-dir Persistence/Migrations; \
    fi

# Builds Domain, Application and Infrastructure through the API project references.
RUN dotnet publish src/TaskFlow.API/TaskFlow.API.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

# Railway injects PORT at runtime; the app binds to it (default 8080).
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "TaskFlow.API.dll"]
