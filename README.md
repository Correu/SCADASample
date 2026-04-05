## SCADA Sample Project

A small, opinionated sample of a **SCADA-style backend** built with ASP.NET Core and SQL Server.  
This repository is intended as a learning/reference project for:

- **.NET 8 Web APIs**
- **Entity Framework Core with SQL Server**
- **Containerized local database development (Azure SQL Edge / SQL Server in Docker)**

---

## Project Structure

```text
SCADASample/
  README.md
  docker-compose.yml           # Local SQL Server (Azure SQL Edge) container

  SCADASampleAPI/              # ASP.NET Core Web API (backend)
    SCADASampleAPI.csproj
    Program.cs                 # App bootstrap, DI & middleware
    appsettings.json           # Main configuration (logging, connection string)
    appsettings.Development.json

    Controllers/
      AlarmsController.cs      # API endpoint(s) for alarms

    Data/
      ApplicationDbContext.cs  # EF Core DbContext and model configuration

    Models/
      Alarms.cs                # Alarm entity representing SCADA alarms

  SCADASampleApp/              # Angular front‑end client
    package.json               # NPM scripts, dependencies (Angular CLI)
    package-lock.json
    src/                       # Angular source (components, modules, routes, etc.)
    ...                        # Standard Angular CLI project structure
```

### High-Level Architecture

- **API layer** (`SCADASampleAPI`)
  - ASP.NET Core Web API exposing endpoints (for example, an `Alarms` endpoint).
  - Uses **Swagger/OpenAPI** for interactive documentation in development.
- **Data access layer**
  - `ApplicationDbContext` uses **Entity Framework Core** to map C# models to SQL tables.
  - The `Alarms` entity models common alarm fields in a SCADA system (tag, setpoint, severity, message, enabled flag, etc.).
- **Database**
  - SQL Server (via **Azure SQL Edge** image) runs in Docker, configured by `docker-compose.yml`.
  - The API connects via the `DefaultConnection` string in `appsettings.json`.

---

## Technologies Used

- **Languages**
  - C# (via `net8.0` on the backend)
  - TypeScript (Angular front‑end)

- **Backend / Framework**
  - ASP.NET Core 8 Web API (`Microsoft.NET.Sdk.Web`)
  - Swagger/OpenAPI via:
    - `Microsoft.AspNetCore.OpenApi`
    - `Swashbuckle.AspNetCore`

- **Frontend / Framework**
  - Angular 19 (`@angular/core`, `@angular/router`, `@angular/forms`, etc.)
  - Angular CLI 19
  - RxJS, Zone.js

- **Data Access**
  - Entity Framework Core (`DbContext`, `DbSet<T>`)
  - SQL Server / Azure SQL Edge

- **Infrastructure & Tooling**
  - Docker & Docker Compose
  - `docker-compose` v3.8 file format
  - Node.js / NPM (for Angular app)

---

## Prerequisites

- **.NET SDK 8.0+**
  - Verify: `dotnet --version`
- **Node.js 18+ and NPM** (for Angular CLI)
  - Verify: `node --version` and `npm --version`
- **Docker** and **Docker Compose**
  - Verify: `docker --version` and `docker compose version` (or `docker-compose --version`)
- Optional: **SQL client tools** (Azure Data Studio, SQL Server Management Studio, `sqlcmd`, etc.) if you want to inspect the database manually.

---

## Running the Angular Client (`SCADASampleApp`)

The `SCADASampleApp/` project is an Angular 19 application intended to be the **SCADA UI** that sits on top of the `SCADASampleAPI` backend.

You normally want the **API + database** running first (see sections below), and then start the Angular dev server.

### 1. Install dependencies

From the repository root:

```bash
cd SCADASampleApp
npm install
```

This installs Angular, Angular CLI, and all other front‑end dependencies defined in `package.json`.

### 2. Run the Angular dev server

Still inside `SCADASampleApp/`:

```bash
npm start
# or, depending on package.json scripts:
npm run dev
# or:
npm run serve
```

Check the `scripts` section in `SCADASampleApp/package.json` if you’re unsure which command is defined.

By default, Angular dev server runs on a URL similar to:

- `http://localhost:4200`

Open that URL in your browser to load the SCADA sample UI.

### 3. Pointing the client at the API

The Angular application should be configured (e.g., via an `environment.ts` file or similar) with the base URL of the API, such as:

- `https://localhost:7183` or `http://localhost:5183`

If you change the API port or host, update the Angular environment/config files accordingly so HTTP calls from the front‑end reach the correct backend.

---

## Running the Database (Docker)

The repo includes a `docker-compose.yml` that starts a **SQL Server (Azure SQL Edge)** instance configured for local development.

```yaml
services:
  mssql:
    image: mcr.microsoft.com/azure-sql-edge:latest
    container_name: sample-scada-mssql
    environment:
      - SA_PASSWORD=yourStrong(!)Password
      - ACCEPT_EULA=Y
    ports:
      - "1433:1433"
```

### Start the Database

From the repository root (`SCADASample/`), run:

```bash
docker compose up -d
```

If your Docker installation uses the older CLI name, use:

```bash
docker-compose up -d
```

This will:

- Start a container named `**sample-scada-mssql**`
- Expose SQL Server on `**localhost:1433**`

> **Security note:** The `SA_PASSWORD` value in `docker-compose.yml` and `appsettings.json` is intended **only for local development**. Change it for any non-local environment.

### Stop the Database

```bash
docker compose down
```

or

```bash
docker-compose down
```

---

## API Configuration

The main API configuration is in `SCADASampleAPI/appsettings.json`.

Key section:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=SCADASampleDB;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;"
}
```

- **Server**: `localhost,1433` (matches the port published by Docker)
- **Database**: `SCADASampleDB`
- **User Id / Password**: matches the SA credentials configured in `docker-compose.yml`
- **TrustServerCertificate**: `True` to simplify local TLS handling

If you change the password or port in `docker-compose.yml`, update this connection string accordingly.

### EF migrations failed part-way (“already an object named …” or “multiple cascade paths”)

If `dotnet run` stops during `MigrateAsync()` with **There is already an object named** `Tags`, `Alarms`, `AspNetUsers`, etc., the database is usually **half migrated**: some `CREATE TABLE` statements committed, but `__EFMigrationsHistory` was never updated, so the next run tries to create the same objects again.

If you see **Introducing FOREIGN KEY constraint … may cause cycles or multiple cascade paths**, that was a SQL Server rule: `ScadaAlarms` had two cascade paths from `Pipelines`. The project fixes this by using **Restrict** on `ScadaAlarms` → `ScadaTags` (you still need a **clean database** or reset script if a failed run left the DB half-built).

**Fix (pick one):**

1. **Reset the Docker volume** (simplest; wipes all SQL data):

   ```bash
   docker compose down -v
   docker compose up -d
   ```

2. **Run the cleanup script** [`SCADASampleAPI/Data/Scripts/ResetDevDatabase.sql`](SCADASampleAPI/Data/Scripts/ResetDevDatabase.sql) against `SCADASampleDB`, then start the API again.

SCADA tables are intentionally named **`ScadaTags`** and **`ScadaAlarms`** so they do not clash with unrelated tables named `Tags` / `Alarms` that may already exist in the database.

### Where are users stored?

ASP.NET Core Identity does not use a table named `Users`. Accounts are in **`AspNetUsers`** (plus **`AspNetRoles`**, **`AspNetUserRoles`**, etc.).

---

## Running the Web API

### 1. Restore and build

From the repository root:

```bash
cd SCADASampleAPI
dotnet restore
dotnet build
```

### 2. Ensure the database container is running

In a separate terminal from the repo root:

```bash
docker compose up -d
```

Confirm the container is running:

```bash
docker ps
```

You should see `sample-scada-mssql` in the list.

### 3. Run the API

From the `SCADASampleAPI/` directory:

```bash
dotnet run
```

By default, ASP.NET Core will host the API on ports similar to:

- HTTP: `http://localhost:5183` (or another dynamically assigned port)
- HTTPS: `https://localhost:7183` (or similar)

The exact ports are defined in `Properties/launchSettings.json` or environment variables.

### 4. Explore the API (Swagger / OpenAPI)

In **Development** mode, Swagger is enabled by `Program.cs`:

- Navigate to the Swagger UI in your browser, e.g.:
  - `https://localhost:7183/swagger`
  - or `http://localhost:5183/swagger`

From there you can:

- Inspect the available endpoints (e.g., weather forecast sample, alarms when implemented)
- Execute requests directly in the browser

---

## Domain Model: Alarms

The `Alarms` entity (`Models/Alarms.cs`) represents a simplified SCADA alarm configuration:

- **AlarmId**: Primary key
- **TagId**: Related tag/point identifier
- **AlarmType**: Type of alarm (e.g., high, low, rate-of-change)
- **SetPoint**: Threshold or setpoint value
- **Severity**: Integer severity level
- **Message**: Human-readable alarm description
- **IsEnabled**: Whether the alarm is active

The `ApplicationDbContext` exposes:

- `DbSet<Alarms> Alarms` – enabling CRUD operations via EF Core and LINQ.

---

## Typical Development Workflow

1. **Start the database**
  - `docker compose up -d`
2. **Run the API**
  - `cd SCADASampleAPI`
  - `dotnet run`
3. **Use Swagger UI** to test endpoints and inspect the model
4. **Iterate on models and controllers**
  - Add/modify entities in `Models/`
  - Update mappings in `ApplicationDbContext`
  - Add/extend controllers in `Controllers/`

---

## Notes & Future Enhancements

- Additional SCADA-style entities (tags, trends, events, historian, etc.) can be added under `Models/` and surfaced via new controllers.
- Migrations (`dotnet ef migrations`) can be introduced to version and evolve the database schema as the domain model grows.
- Frontend visualization (e.g., React or Blazor dashboard) could be added as a separate project alongside `SCADASampleAPI` in this solution.

This README will evolve as the sample project expands to cover more SCADA scenarios.