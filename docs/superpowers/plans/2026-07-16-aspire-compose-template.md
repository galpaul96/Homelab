# Aspire and Docker Compose Template Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a reusable .NET template with Aspire local orchestration and a secure, scalable Docker Compose deployment where only the Blazor web application is reachable externally.

**Architecture:** Aspire models web, API, Redis, two PostgreSQL databases, and MongoDB for development. Compose uses Caddy as the only host-published edge service, balancing web replicas; API and all stateful services remain on internal Docker networks. A one-shot migrator owns schema deployment.

**Tech Stack:** .NET LTS, Aspire, Blazor Server, ASP.NET Core Identity, EF Core/Npgsql, MongoDB.Driver, Redis, Docker Compose v2, Caddy, xUnit.

## Global Constraints

- Use one coherent .NET/Aspire/EF Core/Microsoft.Extensions version train, managed centrally.
- Retain PostgreSQL, MongoDB, and Redis. PostgreSQL has independently owned `web` and `api` databases.
- Only `edge` may have Compose `ports:`. `web`, `api`, Redis, PostgreSQL, MongoDB, and `migrator` must not publish host ports.
- `application` and `data` networks must be `internal: true`; `edge` is the only non-internal network.
- Use exactly `Development`, `Test`, `Acceptance`, and `Production` for `DOTNET_ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT`.
- Do not commit passwords, connection strings with passwords, tokens, certificates, or populated `.env` files. Rotate the credentials currently in tracked launch profiles.
- Application startup must never run `Database.Migrate()`. Only the migrator changes schemas.
- API has no Caddy route, no external Aspire endpoint, and no host port.

---

## Stage 1 — Secure and Standardize the Repository

**Independent deliverable:** The source tree has no tracked secrets and every dependency is selected from one supported version train.

### Task 1: Remove tracked credentials and define secret handling

**Files:**
- Modify: `src/WebServices/Homelab.Web/Properties/launchSettings.json`
- Modify: `src/ApiServices/Homelab.Api/Properties/launchSettings.json`
- Modify: `.gitignore`
- Create: `.env.example`
- Create: `docs/configuration.md`

**Interfaces:** Later work receives database values only as `ConnectionStrings__WebDatabase`, `ConnectionStrings__ApiDatabase`, Mongo settings, and secret files/user secrets.

- [ ] Remove the database environment variables from the two tracked launch profiles. Preserve URLs and profile names only. Rotate the previously committed database users immediately; Git history removal alone is insufficient.
- [ ] Add these `.gitignore` entries and create a names-only `.env.example`:

```gitignore
.env
.env.*
!.env.example
secrets/
**/appsettings.Local.json
```

```dotenv
WEB_DB_PASSWORD=replace-me
API_DB_PASSWORD=replace-me
MONGO_ROOT_PASSWORD=replace-me
POSTGRES_MIGRATOR_PASSWORD=replace-me
```

- [ ] Document configuration precedence: appsettings baseline, environment-specific appsettings, environment variables, then user secrets (local) or Compose secrets/external secret manager (deployment). Include safe user-secrets commands with password placeholders.
- [ ] Verify: `rg -n -i '(password\\s*=|pwd\\s*=|api[_-]?key|secret)' -g '!bin' -g '!obj' .` finds only intentional placeholders/docs.
- [ ] Commit: `git add .gitignore .env.example docs/configuration.md src/WebServices/Homelab.Web/Properties/launchSettings.json src/ApiServices/Homelab.Api/Properties/launchSettings.json; git commit -m "security: remove tracked development credentials"`.

### Task 2: Pin SDK and centralize packages

**Files:**
- Create: `global.json`
- Create: `Directory.Packages.props`
- Modify: all `src/**/*.csproj`

- [ ] Run `dotnet --list-sdks`; choose a supported LTS SDK present in CI and developers' machines. Select matching Aspire packages. Do not retain the current mix of Aspire 9.x SDK declaration / 13.x packages or EF 8.x / Microsoft.Extensions 10.x runtime packages.
- [ ] Add `global.json` with the selected exact SDK and `rollForward: latestPatch`.
- [ ] Enable central package management and move every project-level `Version=` into `Directory.Packages.props`. Use exactly one version each for Aspire, EF Core, Npgsql, OpenTelemetry, and Microsoft.Extensions families.
- [ ] Verify: `dotnet restore src/Homelab.sln` then `dotnet build src/Homelab.sln --no-restore -c Release` both exit `0` without downgrade/conflict warnings.
- [ ] Commit: `git add global.json Directory.Packages.props src; git commit -m "build: centralize template dependency versions"`.

## Stage 2 — Define Application Configuration and Internal API Contract

**Independent deliverable:** All applications use typed/validated configuration and web cannot target arbitrary external API hosts.

### Task 3: Introduce Test, Acceptance, and Production settings

**Files:**
- Create: `src/WebServices/Homelab.Web/appsettings.Test.json`
- Create: `src/WebServices/Homelab.Web/appsettings.Acceptance.json`
- Create: `src/WebServices/Homelab.Web/appsettings.Production.json`
- Create: `src/ApiServices/Homelab.Api/appsettings.Test.json`
- Create: `src/ApiServices/Homelab.Api/appsettings.Acceptance.json`
- Create: `src/ApiServices/Homelab.Api/appsettings.Production.json`
- Create: `src/WebServices/Homelab.Web.Gateway/ApiClientOptions.cs`
- Modify: `src/WebServices/Homelab.Web/Program.cs`
- Modify: `src/ApiServices/Homelab.Api.Ef/Configuration.cs`
- Test: `src/Homelab.Tests/ConfigurationTests.cs`

**Interfaces:** `ApiClientOptions.SectionName == "ApiClient"`, `ApiClientOptions.BaseAddress` is a required URI; connection string keys are `WebDatabase` and `ApiDatabase`.

- [ ] First add `ConfigurationTests` using in-memory configuration: valid `ApiClient:BaseAddress` and connection strings pass `ValidateOnStart`; an absent connection string and malformed URI throw on startup.
- [ ] Add this options type:

```csharp
namespace Homelab.Web.Gateway;
public sealed class ApiClientOptions
{
    public const string SectionName = "ApiClient";
    public required Uri BaseAddress { get; init; }
}
```

- [ ] Replace direct `Environment.GetEnvironmentVariable("WebDatabase")` / `("ApiDatabase")` access with `GetConnectionString("WebDatabase")` and `GetConnectionString("ApiDatabase")`. Remove the unused web SQL Server `DefaultConnection` and SQL Server EF packages when no code refers to them.
- [ ] Add non-secret behavior per environment: Test has deterministic low-noise logging; Acceptance is production-like with protected diagnostics; Production disables detailed errors and Swagger. No setting file contains a password.
- [ ] Verify: `dotnet test src/Homelab.Tests/Homelab.Tests.csproj --filter FullyQualifiedName~ConfigurationTests`; commit `feat: add validated deployment configuration`.

### Task 4: Correct the gateway and service-discovery boundary

**Files:**
- Modify: `src/WebServices/Homelab.Web.Gateway/ExternalApis/IGatewayClient.cs`
- Modify: `src/WebServices/Homelab.Web.Gateway/ExternalApis/GatewayClient.cs`
- Modify: `src/WebServices/Homelab.Web.Gateway/Configurations.cs`
- Modify: `src/WebServices/Homelab.Web.Services/ClientService.cs`
- Modify: `src/WebServices/Homelab.Web.Services/ProductService.cs`
- Modify: `src/WebServices/Homelab.Web.Services/Configurations.cs`
- Test: `src/Homelab.Tests/GatewayClientTests.cs`

**Interfaces:** Later callers use `GetAsync(string route, CancellationToken)`, `PostAsync(string route, object?, CancellationToken)`, `PutAsync`, and `DeleteAsync`; no method accepts a base URL.

- [ ] Add a failing `GatewayClientTests` case with a stub `HttpMessageHandler`. It must prove `Licensing/clients` resolves against the configured base URI and that callers cannot replace its host.
- [ ] Replace the interface with route-only methods, configure one named/typed client with `ApiClientOptions.BaseAddress`, and delete the `https://www.example.com` placeholder, `BuildUri`, and the `CreateClient(nameof(GetAsync))` naming mismatch.
- [ ] Configure Aspire local base address as `https+http://api`; configure Compose as `http://api:8080`. Retain `AddServiceDefaults()` resilience/service discovery.
- [ ] Rename `ConfiugreServices` and `ConfiugreGateway` to `ConfigureServices` / `ConfigureGateway`; update every reference. Pass cancellation tokens from web services.
- [ ] Verify targeted tests pass; commit `refactor: use internal typed API client`.

## Stage 3 — Aspire Local Development

**Independent deliverable:** One AppHost command starts every local resource and provides only the web application externally.

### Task 5: Model all dependencies in AppHost

**Files:**
- Modify: `src/Homelab.AppHost/AppHost.cs`
- Modify: `src/Homelab.AppHost/Homelab.AppHost.csproj`
- Modify: `src/WebServices/Homelab.Web/Program.cs`
- Modify: `src/ApiServices/Homelab.Api/Program.cs`
- Create: `src/Homelab.Tests/TopologyTests.cs`

**Interfaces:** Aspire resource names are `web`, `api`, `cache`, `web-db`, `api-db`, `mongo`. Web references API/cache/web-db; API references api-db/mongo.

- [ ] Add a failing integration test based on `WebTests.cs`. Start `Projects.Homelab_AppHost`, wait for web/API health, assert the web root is reachable and inspect the resource model to ensure API has no external HTTP endpoint.
- [ ] Add PostgreSQL and MongoDB Aspire hosting packages matching the selected central version. In `AppHost.cs`, declare Redis, a PostgreSQL server with `web-db` and `api-db`, and MongoDB; reference and `WaitFor` them from API/web. Only web receives `WithExternalHttpEndpoints()`.
- [ ] Use this intended graph (adjust API names only for the selected Aspire release):

```csharp
var cache = builder.AddRedis("cache");
var postgres = builder.AddPostgres("postgres");
var webDb = postgres.AddDatabase("web-db");
var apiDb = postgres.AddDatabase("api-db");
var mongo = builder.AddMongoDB("mongo");
var api = builder.AddProject<Projects.Homelab_Api>("api")
    .WithReference(apiDb).WithReference(mongo).WaitFor(apiDb).WaitFor(mongo);
builder.AddProject<Projects.Homelab_Web>("web")
    .WithExternalHttpEndpoints().WithReference(webDb).WithReference(cache)
    .WithReference(api).WaitFor(webDb).WaitFor(cache).WaitFor(api);
```

- [ ] Verify with `dotnet test src/Homelab.Tests/Homelab.Tests.csproj --filter FullyQualifiedName~TopologyTests` and then `dotnet run --project src/Homelab.AppHost`. Dashboard shows all resources; browser can access web, not API. Commit `feat: model local dependencies in Aspire`.

## Stage 4 — Migration, Health, and Stateless Startup

**Independent deliverable:** Scaling a web/API process cannot initiate a schema change and Compose has private readiness signals.

### Task 6: Move migrations to a one-shot migrator

**Files:**
- Create: `src/Homelab.Migrator/Homelab.Migrator.csproj`
- Create: `src/Homelab.Migrator/Program.cs`
- Modify: `src/Homelab.sln`
- Modify: `src/WebServices/Homelab.Web/Program.cs`
- Modify: `src/ApiServices/Homelab.Api.Ef/Configuration.cs`
- Test: `src/Homelab.Tests/MigrationStartupTests.cs`

- [ ] Add a test proving the web/API build/start path does not execute `Database.Migrate()`. Add a disposable-PostgreSQL integration test that runs the migrator once and sees both expected schemas.
- [ ] Create a console migrator that reads `ConnectionStrings:WebDatabase` and `ConnectionStrings:ApiDatabase`, performs `Database.MigrateAsync`, then runs idempotent Mongo collection/index bootstrap. Support explicit `--web`, `--api`, and `--mongo` switches and exit nonzero for a missing requested configuration key.
- [ ] Delete migration and `Stopwatch` code from web/API startup. Runtime database users must be less privileged than migrator users.
- [ ] Verify migration tests and `dotnet run --project src/Homelab.Migrator -- --help`; commit `feat: run schema migrations as a deploy operation`.

### Task 7: Make health checks deployment-ready but private

**Files:**
- Modify: `src/Common/Homelab.ServiceDefaults/Extensions.cs`
- Modify: `src/WebServices/Homelab.Web/Program.cs`
- Modify: `src/ApiServices/Homelab.Api/Program.cs`
- Test: `src/Homelab.Tests/HealthEndpointTests.cs`

- [ ] Add health tests: `/alive` returns healthy for the process itself; `/health` includes registered Redis/PostgreSQL/Mongo dependencies. No response includes exception details.
- [ ] Map both endpoints in every environment, use tags so `/alive` checks only `live`, and put dependency checks in readiness. Caddy must not route them publicly unless an external load balancer explicitly requires a minimal route.
- [ ] Verify tests; commit `feat: add deployment health checks`.

## Stage 5 — Container Images and Compose Deployment

**Independent deliverable:** A secure Compose deployment has exactly one public container and reproducible persistent state.

### Task 8: Harden the application images

**Files:**
- Create: `.dockerignore`
- Modify: `src/WebServices/Homelab.Web/Dockerfile`
- Modify: `src/ApiServices/Homelab.Api/Dockerfile`

- [ ] Exclude `.git`, `**/bin`, `**/obj`, `.env*`, `secrets`, and test results in `.dockerignore`.
- [ ] Retain multi-stage builds, use the existing non-root `APP_UID`, set `ASPNETCORE_URLS=http://+:8080`, and expose only `8080`. Remove container HTTPS exposure because Caddy terminates TLS. Confirm required temporary paths work with a read-only root filesystem.
- [ ] Verify both `docker build -f src/WebServices/Homelab.Web/Dockerfile -t homelab-web:local src` and API equivalent. Commit `build: harden application container images`.

### Task 9: Create the Caddy/Compose network topology

**Files:**
- Create: `deploy/compose/docker-compose.yml`
- Create: `deploy/compose/Caddyfile`
- Create: `deploy/compose/.env.test.example`
- Create: `deploy/compose/.env.acceptance.example`
- Create: `deploy/compose/.env.production.example`
- Create: `deploy/compose/secrets/README.md`
- Create: `deploy/compose/scripts/verify-isolation.ps1`

**Interfaces:** Compose services are `edge`, `web`, `api`, `redis`, `postgres-web`, `postgres-api`, `mongo`, `migrator`. Only `edge` has `ports: ["80:80", "443:443"]`.

- [ ] First write `verify-isolation.ps1` to load `docker compose config --format json`; throw if a service other than `edge` has a published port, or if `application`/`data` are not internal.
- [ ] Define networks/volumes:

```yaml
networks:
  edge: {}
  application: { internal: true }
  data: { internal: true }
volumes:
  caddy_data: {}
  caddy_config: {}
  postgres_web_data: {}
  postgres_api_data: {}
  mongo_data: {}
  redis_data: {}
```

- [ ] Attach edge to `edge`; web to `edge` + `application`; API to `application` + `data`; stateful services only to private consumer networks. Add named volumes, health checks, and no `container_name` values, allowing `--scale`.
- [ ] Use this whole Caddy route surface; never add API routing:

```caddyfile
{$SITE_ADDRESS} {
    reverse_proxy web:8080
}
```

- [ ] Add a `migrator` service with `profiles: ["migrate"]`, no ports, and command `--web --api --mongo`. Document the rollout:

```powershell
docker compose --env-file .env.acceptance up --build --abort-on-container-exit migrator
docker compose --env-file .env.acceptance up -d --scale web=2 --scale api=2
```

- [ ] Verify resolved configuration and port isolation; commit `feat: add private Docker Compose deployment topology`.

## Stage 6 — Scale, Edge Security, and Operational Proof

**Independent deliverable:** The template provides evidence that it can scale safely through its edge without expanding the public attack surface.

### Task 10: Configure proxy-aware security and Blazor scale-out

**Files:**
- Modify: `src/WebServices/Homelab.Web/Program.cs`
- Modify: `src/WebServices/Homelab.Web/appsettings.Acceptance.json`
- Modify: `src/WebServices/Homelab.Web/appsettings.Production.json`
- Modify: `deploy/compose/docker-compose.yml`
- Test: `src/Homelab.Tests/ForwardedHeadersTests.cs`
- Test: `src/Homelab.Tests/ScaleOutTests.cs`

- [ ] Test known-proxy `X-Forwarded-*` behavior and reject/ignore untrusted forwarded headers. Apply forwarded-header processing before HTTPS redirection, enable secure cookies/HSTS outside Development, and do not enable detailed errors in Acceptance/Production.
- [ ] Verify and explicitly configure the Redis capability required for Blazor Server/SignalR scale-out. `AddRedisOutputCache("cache")` alone is not proof that long-lived circuit reconnects will behave correctly.
- [ ] Run two web replicas through Caddy, establish an authenticated Blazor session, restart one replica, and verify reconnect/authentication behavior. API remains stateless and can scale independently.
- [ ] Commit `feat: support proxy-aware scalable web hosting`.

### Task 11: Add release gates and runbooks

**Files:**
- Create: `docs/deployment.md`
- Create or Modify: `README.md`
- Modify: `src/Homelab.Tests/WebTests.cs`

- [ ] Extend `WebTests` beyond root HTML: call a web feature that causes a real web-to-API request, proving service discovery/internal DNS is working.
- [ ] Document Aspire local run, Test Compose, Acceptance TLS deployment, Production secrets/backups, scaling, and rollback. Rollback restores prior web/API image tags; it never auto-rolls-back a migration.
- [ ] Run the release gate:

```powershell
dotnet test src/Homelab.sln -c Release
docker compose --env-file deploy/compose/.env.test.example -f deploy/compose/docker-compose.yml config
powershell -ExecutionPolicy Bypass -File deploy/compose/scripts/verify-isolation.ps1 -ComposeFile deploy/compose/docker-compose.yml -EnvironmentFile deploy/compose/.env.test.example
```

Expected: tests pass; Compose resolves; only edge is public; API is not routed at the edge. Commit `docs: add deployment and isolation verification runbooks`.

## Stage Gates

| Stage | Work can stop here? | Required evidence |
| --- | --- | --- |
| 1: Safety/baseline | Yes | No tracked credentials; restore/build clean. |
| 2: Config/client | Yes | Options and gateway tests pass. |
| 3: Aspire | Yes | Integration test proves only web is externally exposed. |
| 4: Migration/health | Yes | Apps no longer migrate at startup; health tests pass. |
| 5: Compose | Yes | Resolved Compose exposes only edge and internal networks. |
| 6: Scale/release | Yes | Two-replica evidence and full release gate pass. |

High availability for PostgreSQL and MongoDB is intentionally a separate operational decision. Before production scale-out, choose managed databases or implement and test replication, backup, and restore procedures; application replicas alone do not make stateful services highly available.
