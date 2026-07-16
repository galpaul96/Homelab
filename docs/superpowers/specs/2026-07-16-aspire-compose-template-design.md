# Aspire and Docker Compose Template Design

## Purpose

Turn the current solution into a reusable .NET web/API template that has a simple .NET Aspire local-development experience and a secure, scalable Docker Compose deployment topology. The Blazor web application is the only application workload reachable from outside the deployment; the API, PostgreSQL, MongoDB, and Redis communicate only on internal networks.

## Existing-State Assessment

The solution contains a sensible layer split:

- `Homelab.Web` is a Blazor Server application with ASP.NET Core Identity.
- `Homelab.Api` is a controller-based API.
- `Homelab.Web.Gateway` and `Homelab.Web.Services` separate web UI concerns from API calls.
- `Homelab.Api.Services`, `Homelab.Api.Ef`, and `Homelab.Api.MongoDb` separate application services and persistence concerns.
- `Homelab.Domain` holds cross-boundary contracts/entities.
- `Homelab.ServiceDefaults` centralizes service discovery, resilience, health checks, and OpenTelemetry.
- `Homelab.AppHost` already starts Redis, API, and web, and marks only the web project with `WithExternalHttpEndpoints()`.

The baseline needs the following corrections before it can be a reliable template:

1. AppHost SDK and package versions are inconsistent (`Aspire.AppHost.Sdk` 9.5.0 versus hosting packages 13.3.5); service projects target .NET 8 while some shared dependencies are version 10.x. The template must select one supported .NET/Aspire release train and manage it centrally.
2. The AppHost does not model PostgreSQL or MongoDB, so local development relies on undeclared external configuration.
3. `Homelab.Web.Gateway` configures an unused named client pointing to `https://www.example.com`; its client implementation creates a different, unconfigured client and accepts arbitrary base URLs. This bypasses service discovery, makes environment behavior ambiguous, and risks outbound calls to unintended hosts.
4. PostgreSQL connection strings are read directly from `WebDatabase` and `ApiDatabase` environment variables, while `Homelab.Web/appsettings.json` retains an unused SQL Server connection string. Configuration has no clear Test, Acceptance, or Production contract.
5. Both web and API run EF migrations when their processes start. This creates concurrency and privilege problems when replicas scale and makes a failed database rollout indistinguishable from an application start failure.
6. The current Dockerfiles expose both HTTP and HTTPS container ports, but there is no Compose topology, network isolation, health-gated dependency ordering, or secrets strategy.
7. Health endpoints are mapped only in Development. Compose needs a private readiness endpoint for orchestration, while externally visible health information should remain minimal and protected by the edge.
8. The web project has direct persistence ownership for Identity and the API has a separate PostgreSQL persistence context. This is acceptable as two bounded databases, but must be explicit rather than sharing tables or a migration owner.

## Evaluated Deployment Approaches

### A. Directly publish the web application

Publish the Blazor web container's port and keep API/data ports internal. This is the smallest Compose file but does not provide TLS termination, host routing, rate limits, or a convenient horizontal load-balancing point.

### B. Edge reverse proxy/load balancer (recommended)

Publish only Caddy or Traefik on ports 80/443. It terminates TLS and load-balances requests over one or more `web` replicas. The API, Redis, PostgreSQL, and MongoDB have no `ports:` mappings and live on internal Docker networks. The web application calls the API by the internal DNS name `api`; Caddy has no route to it.

This is the recommended template baseline because it meets the exposure requirement, makes web scaling a deployment setting, and keeps certificates/HTTP policy out of application containers.

### C. External or orchestrator-provided load balancer

Keep the same container topology but have a platform ingress/load balancer terminate traffic (for example Kubernetes Ingress, a cloud load balancer, or a homelab ingress). This is an excellent future target but should remain a documented extension, not a Compose dependency. The application must not depend on Caddy-specific behavior other than standard forwarded headers.

## Approved Target Architecture

```text
Internet
    |
    |  Published ports: 80/443 only
    v
Edge proxy/load balancer (Caddy by default; Traefik-compatible labels documented)
    |
    +-- web replicas (Blazor Server) -----------------------------+
          | internal HTTP service discovery / Docker DNS          |
          v                                                       |
        API replicas ----------------------------------------------+
          |                 |                    |
          v                 v                    v
     PostgreSQL: web     PostgreSQL: api       MongoDB / Redis
     (Identity DB)       (API DB)              (internal only)
```

### Network Boundaries

- `edge` is a normal Docker network containing only the edge proxy and `web`.
- `application` is an `internal: true` Docker network containing `web`, `api`, and Redis.
- `data` is an `internal: true` Docker network containing `web`, `api`, PostgreSQL instances, MongoDB, and Redis as required.
- The edge proxy is the only service with `ports:` entries. No API, cache, or database container publishes a host port.
- Docker service names are the only DNS names used for internal traffic. APIs do not accept external routes through the proxy.

### Scaling Contract

- Compose supports `docker compose up --scale web=N --scale api=M`; alternatively, replicas may be set through an orchestrator, not hard-coded under `container_name`.
- Caddy performs load balancing across `web` service DNS records. The edge must pass standard `X-Forwarded-*` headers; web config enables and restricts forwarded headers to the known proxy network.
- Blazor Server connections are long-lived. Redis provides the required distributed state/cache/backplane capability so a reconnect can reach another web replica. The plan must verify the exact SignalR scale-out requirement and configure it explicitly rather than assuming output caching alone is sufficient.
- API endpoints must remain stateless. Database migrations are a one-shot deploy job, never an API/web startup side effect.
- PostgreSQL and MongoDB single-node containers are development/small-deployment defaults. High availability, replication, backups, monitoring, and restore testing are separately documented production operations; simply adding application replicas does not make data stores highly available.

## Local Development With Aspire

`Homelab.AppHost` is the canonical local launcher. It will declare Redis, both PostgreSQL databases, and MongoDB as Aspire resources, using containers for a repeatable developer environment. It will then inject generated connection strings/references into the web and API projects and use `WaitFor` readiness dependencies.

Only `web` uses `WithExternalHttpEndpoints()`. The API has an internal endpoint for AppHost-to-service discovery and developer dashboard health status but is not externally bound. The AppHost models resource names as stable template contracts: `web`, `api`, `cache`, `web-db`, `api-db`, and `mongo`.

The web gateway uses a typed `HttpClient` whose base address is the service-discovery URI `https+http://api` (or the single equivalent selected by the supported Aspire version). UI services consume an interface that exposes domain-specific API operations, not generic `(baseUrl, route)` methods. This keeps API routes and resilience policy centrally configured and prevents arbitrary upstream calls.

## Docker Compose Deployment

Compose is the canonical non-development deployment artifact. It contains:

- An `edge` Caddy service configured from a checked-in Caddyfile and its own persistent certificate/config volumes.
- A `web` service built from the existing multi-stage web Dockerfile, attached to `edge` and `application` networks.
- An `api` service built from the API Dockerfile, attached only to `application` and `data` networks.
- `redis`, `postgres-web`, `postgres-api`, and `mongo` services on internal networks with named persistent volumes.
- A one-shot `migrator` service/profile that runs web and API migrations after database health checks and before application rollout. It receives only the database credentials it needs.
- Service health checks, `restart: unless-stopped` (or the deployment policy selected by the operator), resource limits, non-root application users, and read-only file systems where compatible.

Compose has no compose-level replica setting that should be coupled to source control. The operator starts additional replicas with `docker compose up -d --scale web=2 --scale api=2`; the document will include a default one-replica invocation and a scaled invocation.

## Environment and Secret Configuration

Every deployable project uses normal .NET configuration precedence: baseline `appsettings.json`, an optional environment-specific `appsettings.{Environment}.json`, environment variables, and a secret provider. Checked-in files contain only non-secret defaults.

The supported deployment environments are exact `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT` values: `Test`, `Acceptance`, and `Production`. `Development` remains for Aspire local development.

| Environment | Intent | Configuration policy |
| --- | --- | --- |
| Test | Automated/integration verification | Ephemeral or isolated database names, deterministic feature flags, lower resource limits, test-only credentials supplied by CI secrets. |
| Acceptance | Human validation/staging | Production-like topology and TLS, isolated persistent data, diagnostics enabled at safe levels, acceptance-only secrets. |
| Production | Live workload | TLS enforced at edge, restrictive logging and error handling, only production secrets, backups/retention and operational alerting enabled. |

Configuration sections use explicit names: `ConnectionStrings:WebDatabase`, `ConnectionStrings:ApiDatabase`, `MongoDb`, `Redis`, `ApiClient`, `ReverseProxy`, and `OpenTelemetry`. Environment variables follow .NET's double-underscore mapping, such as `ConnectionStrings__ApiDatabase`; Compose secrets or mounted secret files supply values rather than `.env` files committed with credentials.

`appsettings.Test.json`, `appsettings.Acceptance.json`, and `appsettings.Production.json` are added only where a service has non-secret behavioral differences. AppHost settings are development-focused and do not become a production deployment mechanism.

## Persistence and Migration Ownership

- The web project owns the Identity PostgreSQL schema and migrations only.
- The API persistence project owns its PostgreSQL schema and migrations only.
- MongoDB uses explicit collection/index initialization in the migrator or a separate idempotent bootstrap operation.
- Application containers use minimally privileged database users at runtime; the migrator receives a schema-change user.
- No production connection string or credential is checked into an appsettings file, Compose file, Dockerfile, or repository `.env` file.

## Observability, Security, and Health

- Keep `Homelab.ServiceDefaults` as the shared OpenTelemetry/resilience base, but align its dependencies with the selected target framework/Aspire version and document OTLP environment variables.
- Map separate liveness and readiness checks in every environment for internal Docker health checks. Do not expose diagnostic detail publicly; the edge may expose a narrow health route only if required by an external load balancer.
- Enable forwarded headers only from the edge proxy, HSTS in non-development environments, secure cookies, and production error handling. Swagger stays disabled by default outside Development and, if needed in Acceptance, is routed/authenticated intentionally rather than accidentally public.
- Add request limits, security headers, and Caddy rate limiting where appropriate for the intended template audience.

## Project-Structure Improvements

1. Retain the existing domain/application/infrastructure separation, but correct the typo-named `ConfiugreServices`/`ConfiugreGateway` extension methods to `ConfigureServices`/`ConfigureGateway`.
2. Replace the generic gateway client with a typed API client and options object in `Homelab.Web.Gateway`; register it in the web composition root.
3. Move database startup migration code out of `Program.cs` and `Api.Ef.Configuration` into the dedicated migrator project/command.
4. Introduce central package management (`Directory.Packages.props`) and a repository `global.json`; align .NET, EF Core, Npgsql, Aspire, Microsoft.Extensions, and OpenTelemetry package families.
5. Add a repository-root Compose directory and environment-template files, keeping container orchestration separate from application source folders.
6. Expand Aspire integration tests to assert that `web` is externally reachable, API/data resources are not externally bound, and the web-to-API call succeeds over service discovery.

## Acceptance Criteria

1. `dotnet run --project src/Homelab.AppHost` starts all declared dependencies locally, exposes web only, and supports a successful web-to-API request.
2. `docker compose --env-file <environment file> up -d` starts the edge, web, API, data stores, and health checks without exposing API, Redis, PostgreSQL, MongoDB, or migration ports on the host.
3. The sole host-published ports belong to the edge proxy; external requests can reach the web application but not `/api`, API Swagger, or service health diagnostics unless explicitly configured.
4. Test, Acceptance, and Production configuration templates validate without secrets committed to source control.
5. Database migrations run exactly once through the migration operation and web/API replicas can be restarted or scaled without schema changes.
6. At least two web replicas serve requests through the edge while Redis-backed Blazor scale-out behavior is verified.
