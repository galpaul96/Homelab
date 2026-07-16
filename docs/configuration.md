# Configuration contract

The applications use standard .NET configuration precedence: `appsettings.json`, `appsettings.{Environment}.json`, environment variables, then user secrets or deployment secret providers.

Supported environment names are `Development`, `Test`, `Acceptance`, and `Production`.

Local database values belong in user secrets, never in tracked launch profiles:

```powershell
dotnet user-secrets set "ConnectionStrings:WebDatabase" "Host=localhost;Database=homelab_web_dev;Username=homelab_web;Password=<local-secret>" --project src/WebServices/Homelab.Web
dotnet user-secrets set "ConnectionStrings:ApiDatabase" "Host=localhost;Database=homelab_api_dev;Username=homelab_api;Password=<local-secret>" --project src/ApiServices/Homelab.Api
```

Compose maps `ConnectionStrings__WebDatabase` and `ConnectionStrings__ApiDatabase` from deployment secrets. The checked-in `.env.example` contains names only; operators provide real values through an ignored `.env`, Compose secrets, or an external secret manager.

## Identity administration bootstrap

Development and isolated Test environments seed `admin@homelab.local` with the checked-in template password `ChangeMe!123`, assign the `Admin` role, and mark the account with `must_change_password=true`. Change the password immediately after the first successful sign-in at `/Account/Manage/ChangePassword`.

`appsettings.Production.json` sets `IdentityAdministration:SeedDefaultAdministrator` to `false` and intentionally contains no production password. Do not enable the checked-in seed account in Acceptance or Production. For a new deployment, use a one-time operator-controlled bootstrap secret or an existing break-glass account, then remove/rotate that secret.

The Web startup initializer applies EF migrations, creates the audit table, and performs idempotent seeding before the application starts serving requests. It never changes an existing configured user's password, roles, claims, or profile. Privileged mutations from the Admin pages are recorded in `IdentityAdministrationAudits`; passwords, tokens, recovery codes, authenticator keys, and sensitive claim values are redacted.
