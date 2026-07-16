# Web Identity Administration Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver safe, complete Identity account administration in `Homelab.Web`, with seeded access, user lifecycle controls, role/claim management, and immutable audit information.

**Architecture:** ASP.NET Core Identity stays authoritative. All privileged mutations move behind `IIdentityAdministrationService`, which receives an acting administrator and writes an audit entry for both success and failure. Admin UI becomes a dashboard, searchable user catalog, user-detail workspace, role/claim catalog, and audit viewer.

**Tech Stack:** .NET 10, Blazor Server, ASP.NET Core Identity, EF Core/Npgsql, xUnit, Blazor Bootstrap.

## Global Constraints

- Seed `admin@homelab.local` / `ChangeMe!123` with confirmed email, lockout enabled, `Admin` role, and claim `must_change_password=true`.
- The checked-in password is explicitly requested for this template; production configuration must disable the seed path and documentation must prohibit using it in Production.
- Keep all existing Identity self-service features: registration, login, confirmation, password reset/change, external logins, 2FA, recovery codes, profile, personal data, and deletion.
- Admin routes and all administration service mutations require a named policy that requires the `Admin` role.
- Never allow zero active admins, self-disable, self-delete, or removal of an administrator's own final Admin role.
- Audit every privileged success and failure; exclude passwords, tokens, recovery codes, and sensitive claim values.
- Use `ConnectionStrings:WebDatabase` everywhere; Aspire and Compose inject it as `ConnectionStrings__WebDatabase`.

---

## Stage 1 — Seed Policy and Persistence

### Task 1: Typed seed options and explicit Identity policy

**Files:** Create `src/WebServices/Homelab.Web/IdentityAdministration/IdentityAdministrationOptions.cs`; modify `src/WebServices/Homelab.Web/Program.cs` and all `src/WebServices/Homelab.Web/appsettings*.json`; create `src/Homelab.Tests/IdentityAdministrationOptionsTests.cs`.

**Produces:** `IdentityAdministrationOptions` with `DefaultAdminEmail`, `DefaultAdminPassword`, `AdminRoleName = "Admin"`, and `SeedDefaultAdministrator`.

- [ ] Add tests that valid development options bind, while blank email/password/role and invalid email fail at startup.
- [ ] Implement options binding with `ValidateOnStart`; baseline config is `admin@homelab.local`, `ChangeMe!123`, Admin, and seed enabled. Set `SeedDefaultAdministrator=false` in Production.
- [ ] Configure explicit Identity rules: unique email, confirmed-account sign-in, lockout defaults, password policy, cookie settings, and token providers.
- [ ] Run `dotnet test src/Homelab.Tests/Homelab.Tests.csproj -c Release --filter FullyQualifiedName~IdentityAdministrationOptionsTests`; expected PASS.

### Task 2: Add append-only application audit storage

**Files:** Create `src/Common/Homelab.Domain/Entities/Web/IdentityAdministrationAudit.cs` and `src/WebServices/Homelab.Web/Data/IdentityAdministrationAuditEntityTypeConfiguration.cs`; modify `ApplicationDbContext.cs` and its migration snapshot; create one EF migration; create `src/Homelab.Tests/IdentityAdministrationAuditTests.cs`.

**Produces:** `IdentityAdministrationAudit` with numeric ID, `OccurredUtc`, actor/target user IDs, target role ID, `Action`, `Outcome`, redacted `Detail`, and correlation ID.

- [ ] Write a failing persistence test that saves/reloads action, UTC time, outcome, actor, and target; assert the entity has no credential/token fields.
- [ ] Configure `IdentityAdministrationAudits`: required Action (100), Outcome (32), Detail (2000), indexes on timestamp, actor, and target; no cascade deletes from Identity tables.
- [ ] Add the DbSet, generate migration using `ConnectionStrings__WebDatabase`, inspect the generated SQL, and verify no pending model changes.
- [ ] Run the audit test; expected PASS.

### Task 3: Idempotently seed administrator and role

**Files:** Create `IIdentityDatabaseInitializer.cs` and `IdentityDatabaseInitializer.cs` under `IdentityAdministration`; modify `Program.cs`; create `IdentityDatabaseInitializerTests.cs`.

- [ ] Write failing tests for empty-db seeding, a second no-op seed, retained state of an existing account, and disabled seed options.
- [ ] Resolve `UserManager`, `RoleManager`, options, and logger. Create role if missing; find/create configured user; on creation confirm email, enable lockout, add Admin role, and add exact `must_change_password=true` claim. Identity failures throw sanitized errors.
- [ ] Call the initializer after web migration/database creation and before requests are accepted. Never reset existing passwords, claims, account state, or profile data.
- [ ] Run seed tests; expected PASS.

## Stage 2 — Authorization and Service Refactor

### Task 4: Make Identity administration a named authorization policy

**Files:** Create `IdentityAdministrationConstants.cs`; modify `Program.cs`, `Components/Routes.razor`, and `Components/Layout/NavMenu.razor`; create `IdentityAdministrationAuthorizationTests.cs`.

**Produces:** constants `AdminRole`, `AdministrationPolicy`, and `MustChangePasswordClaimType`.

- [ ] Test unauthenticated and non-admin users are forbidden, an Admin passes, and every `/admin` component declares the policy.
- [ ] Register the policy with `RequireAuthenticatedUser` and `RequireRole(AdminRole)`. Replace literal `"Admin"` authorization strings in navigation/components.
- [ ] Add first-login enforcement: a principal with `must_change_password=true` may only access permitted account-management/password-change/logout routes. Clear the claim only after successful password change and refresh sign-in.
- [ ] Run authorization tests; expected PASS.

### Task 5: Replace `IdentityAdminService` with an audited command service

**Files:** Create `IIdentityAdministrationService.cs`, `IdentityAdministrationService.cs`, and `IdentityAdministrationModels.cs` in `IdentityAdministration`; modify `Program.cs`; migrate components; delete `Components/Admin/IdentityAdminService.cs`; create `IdentityAdministrationServiceTests.cs`.

**Produces:** `AdministrationActor`, `AdminOperationResult`, `UserCatalogQuery`, `UserAdministrationDetail`, `IdentityAdministrationAuditQuery`, and explicit commands for every mutation.

- [ ] Write tests for user profile edits, roles, user/role claims, confirm email, lock/unlock, disable/enable, reset access failures, reset authenticator, temporary password, delete user, role CRUD, and audit success/failure entries.
- [ ] Use distinct commands such as `AssignRoleCommand`, `RemoveRoleCommand`, `AddUserClaimCommand`, `ReplaceUserClaimCommand`, `RemoveUserClaimCommand`, role-claim equivalents, `SetAccountStateCommand`, `ResetAuthenticatorCommand`, `SetTemporaryPasswordCommand`, `ConfirmEmailCommand`, and `DeleteUserCommand`. Do not accept arbitrary property names or reflection-based UI input.
- [ ] Validate normalized identifiers, role names, claim type/value limits, duplicates, role/user existence, and typed destructive confirmations. Return structured validation errors.
- [ ] Re-check active Admin state in the mutation transaction. Reject any operation that would remove the final active Admin, self-disable/delete, or self-removal of the final Admin role.
- [ ] Write audit records for authorization, validation, IdentityResult, and success outcomes. Action names use namespaces such as `user.role.assign`; redact claim values where sensitive and never audit passwords/tokens.
- [ ] Run service tests; expected PASS.

## Stage 3 — User-Centric Admin UI

### Task 6: Dashboard and safer user catalog

**Files:** Create `Components/Admin/AdminDashboard.razor`, `Components/Admin/Users/UserList.razor`, and `UserList.razor.css`; replace or migrate `Components/Admin/Users.razor`; modify NavMenu; create `AdminUserListTests.cs`.

- [ ] Test server-side paging, email/username/ID search, role/status filters, non-sensitive list fields, policy protection, and detail navigation.
- [ ] Dashboard shows active users/admins, disabled/locked/unconfirmed counts, recent audit actions, and a warning when only one active admin remains.
- [ ] User list retains the useful existing filters, uses accessible labels/status badges, and has no destructive inline actions. Every row uses a Manage link.
- [ ] Admin navigation shows Dashboard, Users, Roles, Claims, and Audit only under the named policy.
- [ ] Run component/service tests; expected PASS.

### Task 7: Per-user administration workspace

**Files:** Create `Components/Admin/Users/UserDetail.razor`, `UserDetail.razor.css`, and `UserDetailModels.cs`; create `AdminUserDetailTests.cs`.

- [ ] Test normal user, Admin user, current actor, sole active Admin, and not-found states. Guards must disable dangerous UI actions and provide why; service remains final enforcement.
- [ ] Summary/profile section shows ID, username, email and confirmations, phone, lockout/access failures, 2FA, direct claims, effective roles, and recent audits. Edits present field-level Identity errors.
- [ ] Roles section adds/removes roles. Claims section distinguishes direct user claims from role-derived access.
- [ ] Account-state section offers lock duration, unlock, disable/enable, reset failures, confirm email, clear phone, 2FA reset, and temporary password. Temporary-password creation always adds `must_change_password=true` and never echoes the new password after submit.
- [ ] Delete/disable and Admin-role removal require typed confirmation of target email or role. Explain consequences before confirmation.
- [ ] Run detail tests; expected PASS.

## Stage 4 — Roles, Claims, and Audit Catalogs

### Task 8: Refactor role administration

**Files:** Create `Components/Admin/Roles/RoleList.razor` and `RoleDetail.razor`; migrate `Components/Admin/Roles.razor`; create `AdminRoleTests.cs`.

- [ ] Test role creation/rename/delete, member count, claim count, and blocked Admin deletion/rename when it risks active-admin safety.
- [ ] Role list shows membership/claim counts and audit history. Detail shows members, claims, rename, and destructive delete action.
- [ ] Deletion requires typed role-name confirmation and shows affected memberships/claims. Protect Admin through the service guard.
- [ ] Run role tests; expected PASS.

### Task 9: Claims catalog and audit viewer

**Files:** Create `Components/Admin/Claims/ClaimCatalog.razor` and `Components/Admin/Audit/AuditList.razor`; migrate `Components/Admin/Claims.razor`; create `AdminClaimAndAuditTests.cs`.

- [ ] Test paged/filterable claim and audit queries. Filters include actor, target, action, outcome, and date range. Assert sensitive values are not displayed or exported.
- [ ] Claims catalog supports roles and users but sends user-level changes to user detail where possible. Validate type/value length, duplicate claims, and subject existence.
- [ ] Audit is read-only and paged server-side. Display timestamp, actor, target, action, outcome, and redacted detail. Do not add export until retention and data-protection policies are defined.
- [ ] Run claims/audit tests; expected PASS.

## Stage 5 — Account Integration and Release Safety

### Task 10: Preserve self-service account experience

**Files:** Modify `Components/Account/Pages/Manage/ChangePassword.razor`, `ManageNavMenu.razor`, and `Components/Routes.razor`; create `FirstLoginPasswordChangeTests.cs`.

- [ ] Test seeded-admin routing to password change, successful removal of `must_change_password`, and retention on failed change.
- [ ] Remove the exact claim only after a successful `UserManager.ChangePasswordAsync`, then refresh the authentication cookie.
- [ ] Verify existing routes for registration, confirmation, password reset, external login, 2FA, recovery codes, email/phone, personal data, and logout remain available.
- [ ] Run first-login tests; expected PASS.

### Task 11: Concurrency, security, and operational evidence

**Files:** Create `IdentityAdministrationConcurrencyTests.cs` and `IdentityAdministrationSecurityTests.cs`; create `docs/identity-administration.md`; update `docs/configuration.md` and `README.md`.

- [ ] Test concurrent attempts to remove/disable the final Admin. Use a transaction/concurrency strategy so one command is rejected and at least one active Admin always remains.
- [ ] Test unauthorized callers, self-destructive requests, audit records for rejected actions, and credential/token exclusion from audit details.
- [ ] Document default credentials, mandatory first change, Production seeding disabled by default, secure bootstrap override, break-glass recovery, audit retention/backup, role/claim conventions, and default-account cleanup.
- [ ] Run `dotnet build src/Homelab.sln -c Release --no-restore` and `dotnet test src/Homelab.Tests/Homelab.Tests.csproj -c Release --no-build --filter "FullyQualifiedName!~WebTests"`; expected build success and all non-container tests passing. Run Aspire integration tests where Docker is available.

## Implementation Review Checklist

- Every admin action has an actor, validation, authorization, and audit outcome.
- Last-admin and self-lockout safeguards exist in the service, tests, and UI.
- Seed behavior is idempotent and does not mutate existing accounts.
- The requested default password is confined to configuration and disabled in Production.
- No passwords, tokens, or recovery codes can enter audit storage, logs, UI success messages, or exceptions.
- Role, claim, lifecycle, and self-service account flows have automated tests.

---

## Agent Execution Playbook (Mandatory Decision Detail)

This section is deliberately prescriptive. Treat it as the source of truth when a UI convenience, an Identity API default, or a proposed shortcut conflicts with safety, auditability, or the invariants below.

### 1. Repository Map and Boundaries

| Area | Current path | Responsibility after refactor | Do not place here |
| --- | --- | --- | --- |
| Web composition root | `src/WebServices/Homelab.Web/Program.cs` | DI, Identity policy, options validation, initializer invocation, middleware order | User/role mutation business logic |
| Identity data model | `src/WebServices/Homelab.Web/Data/ApplicationDbContext.cs` | Identity tables, audit DbSet, EF configuration discovery | Controller/UI validation |
| Identity user | `src/WebServices/Homelab.Web/Data/ApplicationUser.cs` | User-specific persisted fields only | Roles, permissions, or audit queries |
| Existing legacy service | `src/WebServices/Homelab.Web/Components/Admin/IdentityAdminService.cs` | Read before extracting behaviour; delete only after all consumers use the replacement | New features or new callers |
| New administration boundary | `src/WebServices/Homelab.Web/IdentityAdministration/` | Commands, queries, policy constants, validation, seeding, auditing | Razor rendering and CSS |
| Razor UI | `src/WebServices/Homelab.Web/Components/Admin/` | Rendering, form state, confirmation dialogs, calling typed service APIs | Direct `UserManager`, `RoleManager`, `DbContext`, or raw SQL access |
| Self-service Identity UI | `src/WebServices/Homelab.Web/Components/Account/Pages/Manage/` | Current user's password, MFA, profile and recovery flow | Administrator actions on other users |
| Tests | `src/Homelab.Tests/` | Fast unit/component/service tests, no Docker prerequisite | Production seeding or real shared databases |

The administrator UI must only call `IIdentityAdministrationService`. It must not inject `UserManager<ApplicationUser>`, `RoleManager<IdentityRole>`, or `ApplicationDbContext` directly. This prevents a future page from bypassing audit and final-admin protection.

### 2. Non-Negotiable Security Invariants

Implement each invariant in the service before implementing its corresponding button. UI disabling is informative only; an HTTP/Blazor event or future caller can bypass it.

1. An active administrator means a user that exists, is not disabled, is not currently locked out, and is in `Admin`.
2. The system must have at least one active administrator after every committed operation.
3. An administrator cannot delete, disable, lock, or remove their own final `Admin` membership. Prefer rejecting all self-disable and self-delete attempts, even when other admins exist.
4. A user must not be able to change their own privileges through an administration endpoint. Use self-service account pages for profile/password/MFA actions.
5. Only the named `IdentityAdministrationPolicy` authorizes the admin area and service entry points. Do not rely solely on `[Authorize(Roles = "Admin")]` strings.
6. Passwords, password hashes, reset tokens, authenticator keys, recovery codes, access tokens, and full sensitive claim values must never be persisted in audit rows, logs, exception messages, toast messages, or URLs.
7. A failed privileged request is still auditable. Audit the intended action and failure category, not untrusted raw input.
8. Seeding is idempotent and additive. It never changes an existing user's password, roles, claims, lockout state, profile, MFA configuration, or confirmation state.
9. Production defaults to no checked-in account creation. A deliberate, documented break-glass bootstrap procedure is the only production exception.
10. A database migration and seed process must complete before the application reports readiness. Do not expose a ready instance while its Identity schema is absent.

### 3. Exact Configuration Contract

Use this section name and keys in every Web configuration file. Do not introduce `DefaultConnection`, `IdentityConnection`, raw `ApiDatabase`, or a second bootstrap key name.

```json
"IdentityAdministration": {
  "DefaultAdminEmail": "admin@homelab.local",
  "DefaultAdminPassword": "ChangeMe!123",
  "AdminRoleName": "Admin",
  "SeedDefaultAdministrator": true,
  "AuditRetentionDays": 365
}
```

| Environment | `SeedDefaultAdministrator` | Checked-in password permitted | Required deployment action |
| --- | ---: | ---: | --- |
| Development | `true` | Yes, by explicit template requirement | Change password immediately after first login |
| Test | `true` only for isolated disposable database | Yes | Create a new database per test run or clean it before test |
| Acceptance | `false` by default | No | Supply a one-time bootstrap account through an approved secret or use an existing admin |
| Production | `false` | No | Use documented break-glass bootstrap; store secret outside source control |

Binding rule: `ConnectionStrings:WebDatabase` is read with `GetConnectionString("WebDatabase")`; environment injection uses `ConnectionStrings__WebDatabase`. Never read a connection string from a custom environment variable in the identity code.

Validation rule: fail application startup when seeding is enabled and the email is invalid, role is blank, password is blank, or the password fails the configured Identity password policy. Do not silently disable seeding or substitute another password.

### 4. Identity Defaults to Configure Explicitly

Do not assume the template defaults remain stable across framework upgrades. Set and test the following in `Program.cs`:

```csharp
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
});
```

Keep the current scaffolded Identity endpoints and token providers. Do not replace ASP.NET Core Identity with custom password storage, custom password hashing, or a home-grown authorization system. The checked-in seed password is intentionally an exception to normal secret hygiene; it must satisfy the configured policy exactly.

### 5. Required Types and Stable Service Shape

Use immutable request/response models. A lower-level agent must not expose `ApplicationUser`, `IdentityRole`, `IdentityResult`, or EF entities to Razor components.

```csharp
public sealed record AdministrationActor(string UserId, string Email);

public sealed record AdminOperationResult(
    bool Succeeded,
    string? ErrorCode,
    IReadOnlyList<string> Errors,
    Guid CorrelationId);

public sealed record SetAccountStateCommand(
    string TargetUserId,
    bool IsEnabled,
    DateTimeOffset? LockoutEndUtc,
    string ConfirmationText);

public sealed record AssignRoleCommand(string TargetUserId, string RoleName);
public sealed record RemoveRoleCommand(string TargetUserId, string RoleName, string ConfirmationText);
public sealed record AddUserClaimCommand(string TargetUserId, string Type, string Value);
public sealed record RemoveUserClaimCommand(string TargetUserId, string Type, string Value, string ConfirmationText);
public sealed record SetTemporaryPasswordCommand(string TargetUserId, string Password, string ConfirmationText);
public sealed record DeleteUserCommand(string TargetUserId, string ConfirmationText);
```

The service interface should accept `AdministrationActor` on every mutation and `CancellationToken` on every method. Queries should return view models with only fields permitted for that page. The current actor must be obtained from the authenticated principal in the component or a small adapter, never supplied by a hidden form field.

Use stable error codes for tests and UI: `not_found`, `forbidden`, `validation_failed`, `self_protection`, `last_active_admin`, `confirmation_mismatch`, `concurrency_conflict`, and `identity_failed`. UI displays a friendly localized message; audit stores the code and redacted context.

### 6. Mutation Algorithm (Use for Every Privileged Operation)

For each command, follow this exact ordering:

1. Generate or obtain a `CorrelationId` from the request/activity.
2. Resolve and validate the acting user from `AdministrationActor`; reject if it does not match the authenticated principal.
3. Authorize against `IdentityAdministrationPolicy` before loading sensitive target data.
4. Validate command syntax and confirmation text. Limit role names to 256 characters; claim type to 256; claim value to 1024; never accept control characters in a role name.
5. Load the target and all state needed for invariants from the database.
6. Start a database transaction at `Serializable` isolation for changes that can alter active-admin count: deletion, enable/disable, lock/unlock, role removal, role deletion, and role rename.
7. Re-read the active-admin count inside that transaction immediately before mutation. A simple count performed before starting the transaction is not safe.
8. Execute the specific `UserManager`/`RoleManager` operation and handle every `IdentityResult` error.
9. Add an audit entry in the same transaction for both success and expected rejection. For unexpected exceptions, log a sanitized error and persist a failure audit in a new safe transaction if the original transaction is rolled back.
10. Commit. Only then return success and update the UI.

Do not use a generic `UpdateUser(fieldName, value)` method, reflection, model-binding directly to Identity entities, or client-provided actor IDs. Do not implement bulk role assignment/deletion in the first version; it complicates final-admin guarantees and audit semantics.

### 7. Audit Schema and Redaction Rules

Use the following minimum entity fields. Prefer `Guid` correlation IDs and UTC (`DateTimeOffset`) timestamps.

```csharp
public sealed class IdentityAdministrationAudit
{
    public long Id { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
    public Guid CorrelationId { get; set; }
    public required string Action { get; set; }
    public required string Outcome { get; set; }
    public string? ErrorCode { get; set; }
    public string? ActorUserId { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetRoleId { get; set; }
    public string? Detail { get; set; }
}
```

Accepted action values are fixed constants, not UI strings: `user.create`, `user.profile.update`, `user.email.confirm`, `user.lock`, `user.unlock`, `user.disable`, `user.enable`, `user.access_failures.reset`, `user.authenticator.reset`, `user.password.temporary_set`, `user.delete`, `user.role.assign`, `user.role.remove`, `user.claim.add`, `user.claim.replace`, `user.claim.remove`, `role.create`, `role.rename`, `role.delete`, `role.claim.add`, and `role.claim.remove`.

`Outcome` is exactly `succeeded`, `rejected`, or `failed`. `Detail` contains bounded, structured, redacted metadata such as `role=Editor`, `reason=last_active_admin`, or `lockout_end_utc=...`; it must not contain request dumps, stack traces, email bodies, raw claim values, or credentials. For claim types such as `access_token`, `refresh_token`, `password`, `secret`, `key`, `recovery`, and `authenticator`, audit only the claim type and literal `value=redacted`.

Entity configuration must use maximum lengths, non-null action/outcome/timestamp/correlation ID, `DeleteBehavior.Restrict`/`NoAction` for Identity references, and indexes on `(OccurredUtc)`, `(ActorUserId, OccurredUtc)`, `(TargetUserId, OccurredUtc)`, and `(CorrelationId)`. There must be no UI or service method that updates or deletes audit rows.

### 8. Seed and Startup Sequence

Implement a single scoped `IIdentityDatabaseInitializer.InitializeAsync(CancellationToken)` and call it from an explicit startup scope in `Program.cs`:

```csharp
await using var scope = app.Services.CreateAsyncScope();
var initializer = scope.ServiceProvider.GetRequiredService<IIdentityDatabaseInitializer>();
await initializer.InitializeAsync(CancellationToken.None);
```

Within the initializer, call `Database.MigrateAsync` once, then seed only when options enable it. Look up the role and user by normalized Identity APIs; create missing role; create missing configured user; then set confirmed email, add `Admin`, and add `must_change_password=true` only for the newly created user. Treat `UserManager`/`RoleManager` errors as startup failures with errors sanitized for logs. Do not call `EnsureCreated`; migrations are the only schema path.

For multi-replica deployments, do not allow every Web replica to migrate simultaneously. Stage the Compose/production rollout with exactly one migration/initialization job, or use a PostgreSQL advisory lock around migration and seeding. The final deployment plan must document which method is selected before enabling multiple web replicas.

### 9. UI Behaviour Contract

* Dashboard is read-only: counts, recent redacted audit events, and a prominent single-admin warning. It has no mutation controls.
* User list is server-side paged (default 25, maximum 100), with explicit search, role, enabled, locked, and confirmed filters. Do not load every user into a Blazor circuit.
* User detail is the only location for user mutations. Each destructive action has a dedicated form/model and confirmation field; do not reuse one ambiguous “Save” button.
* Disable/delete/user-role removal must require the exact target email or role name. A current actor, disabled button, or sole-admin warning never replaces service validation.
* A temporary password can be shown once in the submit response only if the operator supplied it; never generate/store/display a password in audit or later page state. Prefer an operator-entered temporary password that conforms to policy.
* Direct claims and inherited role claims must be visually distinct. Removing a direct claim must not imply removal of a role-derived claim.
* Role delete screen must show member and claim counts before confirmation. It cannot silently cascade-delete user roles or claims.
* Audit screen is read-only, server-paged, date-bounded (default last 30 days), and contains no export function in this scope.

### 10. Test Strategy and Required Evidence

Create a dedicated test database strategy before adding behaviour tests. Never run seeding or migrations against the developer's `WebDatabase` from unit tests. For service tests use either an isolated PostgreSQL database configured by the test host or EF SQLite only if every used Identity/query/concurrency behaviour is compatible; use PostgreSQL for the active-admin concurrency tests.

Minimum named tests (write them first):

```text
IdentityDatabaseInitializerTests.Seed_creates_configured_admin_once
IdentityDatabaseInitializerTests.Seed_does_not_mutate_existing_configured_user
IdentityAdministrationServiceTests.Remove_final_active_admin_role_is_rejected_and_audited
IdentityAdministrationServiceTests.Disable_self_is_rejected_and_audited
IdentityAdministrationServiceTests.Set_temporary_password_sets_must_change_password_without_auditing_password
IdentityAdministrationServiceTests.Sensitive_claim_value_is_redacted_in_audit
IdentityAdministrationConcurrencyTests.Two_final_admin_removals_leave_one_active_admin
FirstLoginPasswordChangeTests.Successful_change_removes_must_change_password_and_refreshes_sign_in
FirstLoginPasswordChangeTests.Failed_change_keeps_must_change_password
```

For every mutation test assert all four outcomes: returned result/error code, persisted Identity state, persisted audit row, and absence of a prohibited sensitive value. For every authorization test include anonymous, authenticated non-admin, and Admin callers. For concurrency tests run two commands against separate scopes/connections and assert exactly one succeeds when both would otherwise remove the final active admin.

Required verification commands, in order:

```powershell
dotnet restore src/Homelab.sln
dotnet build src/Homelab.sln -c Release --no-restore
dotnet test src/Homelab.Tests/Homelab.Tests.csproj -c Release --no-build --filter "FullyQualifiedName!~WebTests"
dotnet ef migrations has-pending-model-changes --project src/WebServices/Homelab.Web/Homelab.Web.csproj --startup-project src/WebServices/Homelab.Web/Homelab.Web.csproj
docker compose --env-file deploy/compose/.env.test -f deploy/compose/docker-compose.yml config
```

The last two must produce, respectively, “No changes have been made to the model since the last migration” and a valid resolved Compose document. Run Aspire integration tests only when Docker is healthy; record Docker unavailability as an environmental limitation rather than weakening test assertions.

### 11. Stage Gates and Commit Boundaries

Do not begin the next stage until the gate below passes. Keep each commit independently buildable and avoid mixing code-format-only churn with behaviour changes.

| Gate | Required proof | Suggested commit |
| --- | --- | --- |
| Stage 1 complete | Options validation, migration, and idempotent seed tests pass; seed is false in Production config | `feat(web): seed audited identity administrator` |
| Stage 2 complete | Named policy and all command service mutation/safety/audit tests pass | `feat(web): centralize audited identity administration` |
| Stage 3 complete | User catalog/detail UI tests pass; no UI bypasses the service | `feat(web): add safe user administration workspace` |
| Stage 4 complete | Role/claim/audit pages pass tests; audit is read-only | `feat(web): add role claim and audit administration` |
| Stage 5 complete | Full non-container test suite/build/EF/Compose checks pass; operational documentation reviewed | `docs(web): document identity administration operations` |

### 12. Explicit Non-Goals for This Refactor

Do not add external identity providers, passkeys, organization/tenant scoping, bulk account import, email delivery infrastructure, audit export, soft deletion, custom permissions tables, or a second authorization framework. These are valid future enhancements but introduce security and lifecycle decisions that are not required to deliver complete ASP.NET Core Identity user/role/claim administration safely.
