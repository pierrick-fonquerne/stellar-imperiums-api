# Stellar Imperiums — API

Backend API for Stellar Imperiums, a space colony management browser game.

## Tech Stack

- **.NET 10** (ASP.NET Core Web API)
- **PostgreSQL 16** (relational store) + **MongoDB 8.0** (activity logs)
- **Entity Framework Core 10** + **Npgsql**
- **Wolverine 6** (mediator + messaging, source-generated handlers)
- **JWT Authentication** (planned)

## Architecture

Clean Architecture with 4 layers:

```
StellarImperiums.Api            → Controllers, middleware, DI, hosting
StellarImperiums.Application    → Use cases, Wolverine handlers, DTOs
StellarImperiums.Domain         → Entities (rich domain), value objects
StellarImperiums.Infrastructure → EF Core DbContext, repositories, MongoDB, email
```

Dependency direction is enforced by project references:
`Api → Application + Infrastructure → Application → Domain`.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) for local databases

### 1. Start the databases (from the shared repo)

```bash
cd ../stellar-imperiums-shared/infra
cp .env.example .env
# Set POSTGRES_PASSWORD and MONGO_ROOT_PASSWORD in .env
docker compose up -d
```

### 2. Configure the PostgreSQL connection string (user-secrets)

User secrets keep credentials out of the repository. Run this once per developer machine:

```bash
cd src/StellarImperiums.Api
dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=stellar_imperiums;Username=postgres;Password=<your POSTGRES_PASSWORD>"
```

For production, set the `ConnectionStrings__Postgres` environment variable instead.

### 3. Build and run

```bash
dotnet restore
dotnet build
dotnet run --project src/StellarImperiums.Api --launch-profile http
```

The API listens on `http://localhost:5087` (configurable in `Properties/launchSettings.json`).

### 4. Verify the health endpoint

```bash
curl http://localhost:5087/health
```

Expected response:

```json
{
  "status": "Healthy",
  "durationMs": 232.5,
  "checks": [
    { "name": "postgres", "status": "Healthy", "durationMs": 230.4, "description": null }
  ]
}
```

## Wolverine — code generation

Wolverine generates handler/middleware code at runtime in development (`TypeLoadMode.Dynamic`).
For production builds, pre-generate code and switch to `TypeLoadMode.Static` to remove the
Roslyn runtime dependency and reduce startup time:

```bash
dotnet run --project src/StellarImperiums.Api -- codegen write
```

## Project

- **Frontend**: [stellar-imperiums-frontend](https://github.com/pierrick-fonquerne/stellar-imperiums-frontend)
- **Shared (infra, SQL, docs)**: [stellar-imperiums-shared](https://github.com/pierrick-fonquerne/stellar-imperiums-shared)
- **Project Board**: [GitHub Project](https://github.com/users/pierrick-fonquerne/projects/7)
