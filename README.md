# TaskFlow API

Task scheduling & daily productivity tracking REST API built with **ASP.NET Core 8**, **EF Core (PostgreSQL, code-first)**, **ASP.NET Core Identity + JWT with rotating refresh tokens**, and strict **Clean Architecture**.

## Quick start

```bash
# 1. Prerequisites: .NET 8 SDK, PostgreSQL (local install or: docker run -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16)
dotnet restore
dotnet build

# 2. (optional) set your connection string and a real JWT secret (>= 32 chars)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>" --project src/TaskFlow.API
dotnet user-secrets set "Jwt:SecretKey" "<a long random secret, 32+ chars>" --project src/TaskFlow.API

# 3. Create the database (creates the first migration if none exists, then applies it)
./scripts/init-db.sh          # macOS / Linux / Git Bash
./scripts/init-db.ps1         # Windows PowerShell

# 4. Run
dotnet run --project src/TaskFlow.API
# Swagger UI: http://localhost:5080/swagger  (Development)
```

Manual equivalent of step 3:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API --output-dir Persistence/Migrations
dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API
```

Pending migrations are applied automatically on every startup (with retries while the DB boots). Commit the generated `Persistence/Migrations` folder.

**Using Swagger:** call `POST /api/auth/register` (or `login`), copy the `accessToken`, click **Authorize**, paste the token (no `Bearer ` prefix).

## Endpoints

| Area | Method & route | Notes |
|---|---|---|
| Auth | `POST /api/auth/register`, `/login`, `/refresh` | anonymous, rate limited (20/min per IP) |
| Auth | `POST /api/auth/revoke` | logout: revokes a refresh token |
| Projects | `GET/POST /api/projects`, `GET/PUT/DELETE /api/projects/{id}` | deleting keeps tasks (ProjectId set to NULL) |
| Tasks | `GET /api/tasks` | filters: `status, priority, projectId, dueFrom, dueTo`; `sortBy, sortDescending, page, pageSize` |
| Tasks | `POST /api/tasks`, `GET/PUT/DELETE /api/tasks/{id}`, `PATCH /api/tasks/{id}/status` | |
| Time logs | `POST /api/timelogs/start`, `POST /api/timelogs/stop`, `GET /api/timelogs/active` | one running timer per user |
| Time logs | `POST /api/timelogs/manual` | `startTime` + (`endTime` **or** `durationInMinutes`) |
| Time logs | `GET /api/timelogs/{id}`, `DELETE /api/timelogs/{id}`, `GET /api/tasks/{taskId}/timelogs` | |
| Analytics | `GET /api/analytics/daily?date=YYYY-MM-DD` | completed tasks, time tracked, per-project breakdown, completion rate |
| Analytics | `GET /api/analytics/weekly?startDate=YYYY-MM-DD` | 7 days, most productive day, total hours |
| Health | `GET /health` | anonymous |

Errors are always RFC 9457 `ProblemDetails`: 400 validation (with an `errors` map), 401 auth, 404 not found, 409 conflict, 422 business rule, 500 unexpected.

## Business rules worth knowing

- **Ownership:** every query is filtered by the caller's user id (from the JWT `sub` claim). Someone else's resource returns **404**, never 403, so ids don't leak.
- **Timers:** only one running timer per user. This is enforced in the service **and** by a filtered unique index (`EndTime IS NULL`) in PostgreSQL, so two simultaneous requests cannot both win (the loser gets 409).
- **Starting a timer** moves a `Todo` task to `InProgress`. **Completing a task** stops a timer still running on it. Timers cannot start on completed tasks.
- **Manual logs** cannot overlap another log or the running timer, cannot end in the future, and are limited to 24 h each.
- **Dates are UTC.** Analytics days are UTC days. A task counts toward the day of its `CompletedAtUtc`; a time log counts toward the day it **started**.
- **Completion rate** (daily) = tasks *due* that day that are Completed / tasks due that day x 100 (0 when none are due).
- **Refresh tokens** are stored as SHA-256 hashes, rotated on every use, and replaying a revoked token revokes all of that user's sessions.
- **Passwords:** Identity's PBKDF2 hasher, min 8 chars with upper/lower/digit/symbol, lockout after 5 failures for 15 min.

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | Npgsql connection string **or** a `postgresql://user:pass@host:port/db` URL |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SecretKey` | token settings (secret must be >= 32 chars; **replace the dev value in `appsettings.json` and never commit a real one**) |
| `Jwt:AccessTokenMinutes` / `Jwt:RefreshTokenDays` | lifetimes (15 min / 7 days) |
| `PORT` | HTTP port to bind (Railway sets it; default 8080) |
| `SKIP_DB_MIGRATION` | set to `true` to skip startup migrations |
| `Cors:AllowedOrigins` | array of allowed browser origins (empty = none) |

---

# Architecture & Developer Guide

## Layers and the dependency rule

```
API  ──►  Infrastructure  ──►  Application  ──►  Domain
 └────────────────────────────►┘
```

Dependencies only point inward. `Domain` knows nothing about anything else; `Application` knows only `Domain`.

| Project | Responsibility | Contains |
|---|---|---|
| **Domain** | Business model & rules, no packages | Entities (`Project`, `TaskItem`, `TimeLog`, `RefreshToken`), enums, domain exceptions, repository interfaces, `IUnitOfWork`, `TaskQueryFilter`, `PagedResult<T>` |
| **Application** | Use cases | Request/response DTOs, FluentValidation validators, services (`AuthService`, `ProjectService`, `TaskService`, `TimeLogService`, `AnalyticsService`), manual mappers, ports for outside things (`IIdentityService`, `ITokenService`, `ICurrentUserService`, `IDateTimeProvider`) |
| **Infrastructure** | Technical details | `AppDbContext`, EF configurations, repositories, `UnitOfWork`, Identity (`ApplicationUser`, `IdentityService`), `JwtTokenService`, clock |
| **API** | HTTP | Controllers, exception middleware, JWT/Swagger/rate-limit setup, `CurrentUserService` (reads JWT claims), `Program.cs` |

Interfaces are **owned by the inner layer that needs them** (repositories in Domain; identity/token/current-user ports in Application) and **implemented by the outer layer**. That is what keeps EF Core, Identity and ASP.NET out of the business logic.

## How a request flows (example: `POST /api/timelogs/start`)

1. **Pipeline:** `ExceptionHandlingMiddleware` wraps everything, then authentication validates the JWT and builds `HttpContext.User`.
2. **Controller** (`TimeLogsController.Start`) binds the JSON body to `StartTimerRequest` and calls `ITimeLogService`. No logic here.
3. **Service** (`TimeLogService`):
   - validates the DTO with FluentValidation (`RequestValidationException` -> 400),
   - gets the caller id from `ICurrentUserService` (implemented in the API from the `sub` claim),
   - loads the task through `IUnitOfWork.Tasks` (scoped by user id, otherwise `NotFoundException` -> 404),
   - checks business rules (no running timer -> otherwise `BusinessRuleException` -> 422),
   - calls domain behaviour (`TimeLog.StartNew`, `TaskItem.ChangeStatus`),
   - commits once with `IUnitOfWork.SaveChangesAsync`.
4. **Infrastructure:** repositories translate calls to EF Core/LINQ; `AppDbContext` stamps `UpdatedAtUtc`; `UnitOfWork` turns a unique-index violation into `ConflictException` -> 409.
5. **Response:** the entity is mapped to `TimeLogResponse` and returned as JSON (enums as strings). Any exception along the way is converted to `ProblemDetails` by the middleware.

```
HTTP -> Middleware -> Auth -> Controller -> Service -> (Validator, Domain rules)
                                              |
                                      IUnitOfWork / Repositories -> EF Core -> PostgreSQL
```

## Adding a new feature without breaking existing code: Recurring Tasks

The change is additive: extend inward-to-outward and touch existing files only where unavoidable.

1. **Domain**
   - Add `Enums/RecurrenceFrequency.cs` (`Daily, Weekly, Monthly`).
   - Add `Entities/RecurrenceRule.cs` (`TaskItemId`, `Frequency`, `Interval`, `EndsOnUtc`, `LastGeneratedUtc`) with a method such as `GetNextOccurrence(DateTime after)`.
   - Add `Interfaces/IRecurrenceRepository.cs` and expose it as `IRecurrenceRepository Recurrences { get; }` on `IUnitOfWork`.
2. **Application**
   - Add `DTOs/Recurrence/*` (`SetRecurrenceRequest`, `RecurrenceResponse`), a `SetRecurrenceRequestValidator`, an `IRecurrenceService` + `RecurrenceService`, and one line in `AddApplication()`.
   - Generating the next occurrence is a method on the service that creates a normal `TaskItem` through the existing `ITaskRepository`, so filtering, analytics and timers work on generated tasks with no changes.
3. **Infrastructure**
   - Add `RecurrenceRuleConfiguration` (picked up automatically by `ApplyConfigurationsFromAssembly`), a `DbSet`, `RecurrenceRepository`, register it in DI, and wire it into `UnitOfWork`'s constructor.
   - Run `dotnet ef migrations add AddRecurringTasks ...` (a new migration; existing ones are never edited).
   - For automatic generation, add a `BackgroundService` here that calls `IRecurrenceService` on a schedule.
4. **API**
   - Add `RecurrenceController` (new routes such as `PUT /api/tasks/{id}/recurrence`). Existing controllers and DTOs stay untouched.

Why nothing breaks: existing services depend on small interfaces that only gain members, new tables have no foreign keys *into* existing behaviour, and every query is still user-scoped through the same repository pattern.

## Design notes & trade-offs

- **No FK from domain tables to `AspNetUsers`:** keeps the Domain free of Identity and avoids multiple-cascade-path problems. Ownership is enforced in every repository query. If you want hard referential integrity, add the FKs in Infrastructure configurations (use `NoAction` for `TimeLogs`).
- **Manual mapping** instead of AutoMapper: explicit, compile-time checked, no licence concerns.
- **Expired/revoked refresh tokens are not purged automatically.** Add a periodic cleanup job for production.
- **Roles:** the token carries `role` claims and the JWT validation maps them, so `[Authorize(Roles = "...")]` works once you create roles; no roles are seeded.
- **Tests:** the architecture makes the Application layer testable with fakes for `IUnitOfWork`, `ICurrentUserService` and `IDateTimeProvider`. `Program` is exposed (`public partial class`) for `WebApplicationFactory` integration tests.


---

# Deploying to Railway

Files: `Dockerfile`, `.dockerignore`, `railway.json` (Docker build, `/health` health check).

1. Push this folder to a GitHub repository.
2. Railway -> **New Project** -> **Deploy from GitHub repo** (or `railway init` + `railway up`).
3. In the project add **Database -> PostgreSQL**.
4. On the API service open **Variables** and add:

| Variable | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | `${{Postgres.DATABASE_URL}}` (use your Postgres service's exact name) |
| `Jwt__SecretKey` | a random string of 32+ characters |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

5. **Settings -> Networking -> Generate Domain**. Swagger is at `https://<your-domain>/swagger` (the root `/` redirects there).

Notes: the app binds to Railway's `PORT`; migrations run automatically at startup; if no migration is committed, the Docker build generates `InitialCreate` so the first deploy still works, but commit a real migration (`./scripts/init-db.sh` locally) before changing the model.
