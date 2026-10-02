# ClaimBase

Multi-tenant SaaS for universities to calculate lecturer claims from sessions the lecturer logs, with versioned rates and an on-demand PDF. Biometric imports appear on the report as a presence mark. They do not change what is paid.

Product rules live in `.cursor/skills/`. Slice 1 adds sign-in. Courses and claims are later slices.

## Layout

```
ClaimBase.sln
Backend/src/
  ClaimBase.Api/              ASP.NET Core Web API
  ClaimBase.Application/      Use cases (MediatR, FluentValidation)
  ClaimBase.Domain/           Entities and claim rules
  ClaimBase.Infrastructure/   EF Core, JWT, Hangfire, QuestPDF, Excel
  ClaimBase.Shared/           Public DTOs
Backend/tests/
Portal/                       Angular standalone + Tailwind (project name: portal)
MobileApp/                    .NET MAUI for lecturers (Android target)
docker-compose.yml            PostgreSQL 16 on localhost:5434
```

## Local development

### 1. Database

```bash
docker compose up -d
```

Postgres listens on `localhost:5434`, database `claimbase_db`, user `claimbase`. The Development password is `claimbase_dev` and lives only in `appsettings.Development.json`.

### 2. API

```bash
dotnet run --project Backend/src/ClaimBase.Api/ClaimBase.Api.csproj
```

API: http://localhost:5190

Health: `GET /health`

On first Development startup the API applies the identity migration and seeds one university. Sign in at http://localhost:4201/login with one of these addresses. The shared password is `Seed:Password` in `appsettings.Development.json` and is not copied here.

| Role | Email |
|---|---|
| Tenant admin | `tenantadmin@claimbase.test` |
| Admin | `admin@claimbase.test` |
| Head of department | `hod@claimbase.test` |
| Finance | `finance@claimbase.test` |
| Lecturer | `lecturer@claimbase.test` |

A lecturer can sign in to the API and is turned away by the portal with "Use the ClaimBase mobile app". Portal routes are `/login` and an empty `/app` shell.

### 3. Portal

```bash
cd Portal
npm start
```

UI: http://localhost:4201

The production Portal build calls same-origin `/api`. `environment.ts` points at `http://localhost:5190` for `ng serve`.

### 4. Mobile app

```bash
dotnet build MobileApp/ClaimBase.MobileApp.csproj -f net9.0-android
```

The project targets Android because that workload is installed. Lecturer sign-in is not in this slice.

## Configuration

| Setting | Development default |
|---|---|
| Postgres | `localhost:5434`, db `claimbase_db`, user `claimbase` |
| JWT issuer / audience | `ClaimBase.Api` / `ClaimBase.Portal` |
| JWT key | Development key in `appsettings.Development.json` only |
| Portal origin | `http://localhost:4201` |
| Forwarded client IP | Loopback proxies only, plus any `ForwardedHeaders__KnownProxies` or `ForwardedHeaders__KnownNetworks` |

`appsettings.json` ships empty connection string and JWT values. Production refuses to start on the Development password, `Include Error Detail`, a JWT key shorter than 64 characters, or the Development JWT key.

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Host=...;Database=claimbase_db;Username=...;Password=..."
export JWT__KEY="<at least 64 random characters>"
export CORS__ORIGINS="https://portal.example"
export ForwardedHeaders__KnownProxies__0="<reverse proxy IP>"
```

`ForwardedHeaders__KnownNetworks__0` is a CIDR block when the proxy is a range. Loopback stays trusted either way. A client that is not a known proxy cannot replace its address with `X-Forwarded-For`.

Do not set `Include Error Detail=true` on the Production connection string. Apply EF migrations as a deploy step. The API does not migrate itself in Production.

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update --project Backend/src/ClaimBase.Infrastructure/ClaimBase.Infrastructure.csproj --startup-project Backend/src/ClaimBase.Api/ClaimBase.Api.csproj
```

## Tests

```bash
dotnet test ClaimBase.sln
cd Portal && npm run build
```

## Product rules

- A semester must be open before a lecturer can log a session.
- Lecturers enter course code, start, and end. There is no timetable import.
- Teaching pay is one flat rate per session: lecturer position on that date × course qualification.
- Transport pay is one tenant rate per distinct calendar day with at least one claimed session.
- Rates are effective-dated and copied onto claim lines. Later edits do not rewrite an approved claim.
- Part-time and full-time lecturers are both paid.
- A biometric punch on the session date prints a checkmark. A missing punch leaves the amount unchanged.

## Skills

| Skill | Use |
|-------|-----|
| [claimbase](.cursor/skills/claimbase/SKILL.md) | Domain, architecture, delivery slices |
| [claimbase-ui-ux](.cursor/skills/claimbase-ui-ux/SKILL.md) | Portal screens and interaction |
| [claimbase-design-system](.cursor/skills/claimbase-design-system/SKILL.md) | Visual tokens |

Build one slice at a time and confirm before the next. Slice order is in [phases.md](.cursor/skills/claimbase/phases.md).
