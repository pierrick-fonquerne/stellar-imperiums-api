# Stellar Imperiums — API

Backend API for Stellar Imperiums, a space colony management browser game.

## Tech Stack

- **.NET 10** (ASP.NET Core Web API)
- **PostgreSQL** (relational database)
- **MongoDB** (activity logs)
- **Entity Framework Core** (ORM)
- **JWT Authentication**

## Architecture

Clean Architecture with 4 layers:

```
StellarImperiums.Api/            → Controllers, middleware, configuration
StellarImperiums.Application/    → Services, DTOs, interfaces
StellarImperiums.Domain/         → Entities, enums, value objects
StellarImperiums.Infrastructure/ → EF Core, MongoDB, repositories, email
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/) 16+
- [MongoDB](https://www.mongodb.com/) 8.0+
- Or [Docker](https://www.docker.com/) for local development

### Run locally

```bash
dotnet restore
dotnet build
dotnet run --project StellarImperiums.Api
```

### Run with Docker Compose

```bash
docker compose up -d
```

## Project

- **Frontend**: [stellar-imperiums-frontend](https://github.com/pierrick-fonquerne/stellar-imperiums-frontend)
- **Project Board**: [GitHub Project](https://github.com/users/pierrick-fonquerne/projects/7)
