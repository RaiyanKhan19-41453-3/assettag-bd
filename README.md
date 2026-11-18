# AssetTag BD

B2B SaaS for corporate IT & office asset tracking with QR codes. Replaces spreadsheets with a searchable asset register, QR scan checkouts, assignment history and maintenance logs.

## Stack

- **Backend:** ASP.NET Core 8 Web API, EF Core 8, PostgreSQL, Clean Architecture (Domain → Application → Infrastructure → API), MediatR + FluentValidation, BCrypt, JWT, QRCoder.
- **Frontend:** React 18 + TypeScript (Vite 5), TailwindCSS 3, React Router 6, TanStack Query 5, Axios, `html5-qrcode`.
- **DevOps:** multi-stage Dockerfiles, Nginx reverse proxy, Docker Compose.

## Quick start (Docker)

```bash
cp .env.example .env      # then edit the secrets
docker compose up -d --build
```

- Frontend: http://localhost:3000
- API: http://localhost:5080 (Swagger is enabled only when `ASPNETCORE_ENVIRONMENT=Development`)
- PostgreSQL: localhost:5432

The API applies EF migrations and seeds a default tenant with a SuperAdmin on startup. The login is whatever you set as `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` in `.env` (the template uses `admin@example.com` / `change-me-admin-password`). **Change these values before exposing the app.** The demo credentials shown on the local login page only appear when `VITE_SHOW_DEMO_CREDENTIALS=true` is passed to the frontend build.

To onboard a real customer, use **Create a company workspace** on the login screen (`POST /api/auth/signup-company`). That creates a new isolated tenant plus its first SuperAdmin. The SuperAdmin can then invite IT Managers and Employees from the **Team** page; public self-registration is disabled. Emails are globally unique so login remains unambiguous, and every tenant-owned table has a `TenantId` with an EF global query filter — cross-tenant reads return 404.

To browse Swagger with the Docker stack, set `ASPNETCORE_ENVIRONMENT=Development` in `.env` and recreate the API container (`docker compose up -d --force-recreate api`). Set `SEED_SAMPLE_DATA=true` at the same time if you want demo assets.

Useful commands:

```bash
docker compose logs -f api
docker compose down                 # keep the database volume
docker compose down -v              # also delete the database
```

## Local development (without Docker)

```bash
# Database
docker run -d --name assettagbd -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=assettagbd -p 5432:5432 postgres:16-alpine

# API
dotnet restore AssetTagBD.sln
dotnet run --project src/AssetTagBD.API

# Frontend (separate terminal)
cd frontend && npm install && npm run dev
```

Set `VITE_API_URL=http://localhost:5080/api` in `frontend/.env` (see `.env.example`).

## Roles

| Role | Capabilities |
|---|---|
| SuperAdmin | Everything, including deleting assets and creating privileged users |
| ITManager | Create/update assets, assign/return, scan, resolve maintenance |
| Employee | View assets, see **My assets** (items checked out to them), report maintenance issues |

The asset list is server-paginated (12 per page, sortable by name/tag/purchase date/status) so it stays fast as the register grows. Asset status is kept consistent with the assignment record: an asset cannot be marked Assigned without an active assignment, or Available while one exists — use the Assign/Return actions instead. Maintenance cannot be reported on a retired asset.

## Project layout

```
src/
  AssetTagBD.Domain/        Entities, enums, no dependencies
  AssetTagBD.Application/   DTOs, validators, MediatR features, interfaces
  AssetTagBD.Infrastructure/ EF Core, repositories, JWT/BCrypt/QR services, migrations
  AssetTagBD.API/           Controllers, auth, exception middleware, seeding
frontend/                   React SPA (services → hooks → components → pages)
docker-compose.yml          db + api + frontend
```

## Authentication

Access tokens are short-lived JWTs; refresh tokens are **hashed at rest**, expire after 7 days and **rotate on every use** (replaying a used token is rejected and revokes the session). Password reset links are single-use, hashed, expire after one hour and revoke all active sessions when used.

`POST /api/auth/forgot-password` always returns the same message so account existence is never disclosed. Password-reset emails are sent through `SmtpEmailSender` when `Email:Smtp:Host` is configured (`SMTP_HOST` + `SMTP_USER` + `secrets/smtp_password.txt` in the production stack). Without SMTP, a logging fallback is used **only** in dev/demo (`AUTH_EXPOSE_RESET_TOKEN=true`); in production it fails loudly rather than silently dropping reset emails. Requests are throttled to 5 per email per hour and issuing a new link invalidates earlier ones.

## Excel import & QR labels

IT Managers can bulk-import assets from **Assets → Import Excel**. The first worksheet must have a header row; recognized columns (case-insensitive, spaces optional) are:

| Column | Aliases | Required |
|---|---|---|
| Name | Asset Name, Asset | yes |
| Purchase Date | Purchase | yes |
| Category | Type | no (defaults to Other) |
| Asset Tag | Tag | no (auto-generated when blank) |
| Serial Number | Serial | no |
| Warranty Expiry Date | Warranty, Warranty Expiry | no |

Invalid rows are skipped with a row-numbered error list; valid rows are inserted in one transaction and QR codes are generated for all of them. After importing, **Print QR labels** opens an A4 sheet (3 × 8 labels) ready for a standard label printer. A single label can also be printed from the asset detail page.

## Tests

Integration tests run against a real PostgreSQL 16 container (Testcontainers), so Docker must be running:

```bash
dotnet test AssetTagBD.sln
```

They cover tenant isolation, company sign-up, invite-only registration, the assign/return workflow, maintenance resolution, QR generation and the validation pipeline. CI (`.github/workflows/ci.yml`) runs the backend build + tests, the frontend typecheck/build, and a full Docker image build on every push and PR.

## Production deployment

`docker-compose.prod.yml` adds automatic TLS via Caddy (Let's Encrypt) and mounts secrets as files instead of environment variables.

```bash
mkdir -p secrets
printf 'a-long-random-jwt-secret-at-least-32-chars' > secrets/jwt_secret.txt
# Username must match POSTGRES_USER and password must match db_password.txt.
printf 'Host=db;Port=5432;Database=assettagbd;Username=postgres;Password=a-strong-database-password' > secrets/db_connection.txt
printf 'a-strong-database-password' > secrets/db_password.txt
printf 'a-strong-initial-admin-password' > secrets/admin_password.txt

export DOMAIN=assets.example.com
export EMAIL=you@example.com
docker compose -f docker-compose.prod.yml up -d --build
```

The API reads secrets through `SecretReader` (`JWT_SECRET_FILE`, `ConnectionStrings__DefaultConnection_FILE`, `Seed__AdminPassword_FILE`), so images don't need to change between dev and prod. Caddy proxies only `/api/*` to the API and everything else to the SPA; no application port is published publicly. Health probes and `/metrics` are intentionally **not** routed through Caddy — scrape them directly on the internal Docker network (`api:8080`).

### Health, logs and metrics

- `/health/live` — process liveness (no dependencies)
- `/health/ready` — database connectivity
- `/metrics` — Prometheus metrics from `prometheus-net`
- Logs are structured JSON on stdout (Serilog). Use `docker compose logs api | jq .` locally.

Example Prometheus scrape config and alert rules are in `monitoring/`.

### Backups

```bash
scripts/backup.sh                       # creates backups/assettagbd_<timestamp>.dump.gz
ASSUME_YES=1 scripts/restore.sh backups/assettagbd_....dump.gz
```

Backups retain 14 days. **Test restores regularly** — a backup that has never been restored is only a hypothesis.

## Configuration

All runtime settings can be overridden with environment variables (the Docker Compose file uses `ConnectionStrings__DefaultConnection`, `Jwt__*`, `Seed__*`, `Cors__AllowedOrigins__0`). See `.env.example` for the full list.
