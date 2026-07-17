# Dark Dashboard UI Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refresh Homelab.Web into a cleaner dark dashboard while preserving routes, Identity behavior, authorization, accessibility, and existing Bootstrap dependencies.

**Architecture:** Use a tokenized CSS theme in `wwwroot/app.css`, then make focused layout/navigation/admin component style changes. No new UI framework or business logic is introduced.

**Tech Stack:** .NET 10, Blazor Server, Bootstrap, Blazor Bootstrap, CSS custom properties.

## Global Constraints

- Keep the dark dashboard direction with blue-slate surfaces and restrained warm-gold accents.
- Do not introduce Tailwind, MudBlazor, Radzen, or another UI framework.
- Preserve all routes, render modes, authorization attributes, antiforgery behavior, and Identity workflows.
- Do not move destructive actions into hover-only controls or weaken confirmations.
- Preserve semantic labels, visible focus indicators, status text, reduced-motion support, and responsive layouts.

---

### Task 1: Add the shared design-token theme

**Files:** Modify `src/WebServices/Homelab.Web/wwwroot/app.css`.

- [ ] Add `:root` variables for `--hl-bg`, `--hl-surface`, `--hl-surface-raised`, `--hl-border`, `--hl-text`, `--hl-text-muted`, `--hl-accent`, status colors, radii, shadows, and focus ring.
- [ ] Replace hard-coded theme selectors with variables while retaining Bootstrap-compatible class names.
- [ ] Style body, links, headings, cards, alerts, forms, buttons, badges, modal surfaces, list groups, accordions, tables, and validation messages.
- [ ] Add visible `:focus-visible` styles and `@media (prefers-reduced-motion: reduce)` rules.
- [ ] Keep the existing Blazor error boundary readable and visually distinct.
- [ ] Verify no selector hides focus outlines or relies on color alone for status.

### Task 2: Refresh navigation and layout

**Files:** Modify `src/WebServices/Homelab.Web/Components/Layout/NavMenu.razor`, `NavMenu.razor.css`, and `MainLayout.razor.css`.

- [ ] Preserve the existing `AuthorizeView Roles="Admin"`, authenticated account links, logout form, and antiforgery token.
- [ ] Add visual grouping for primary, administration, and account links without changing route values.
- [ ] Add a rounded active `NavLink` state, consistent icon column width, improved hover/focus states, and a distinct logout button treatment.
- [ ] Use CSS variables from Task 1; do not add inline color literals.
- [ ] Ensure the collapsed mobile navigation remains keyboard accessible and closes using the existing checkbox behavior.
- [ ] Constrain main content width and increase responsive gutters without changing component render mode.

### Task 3: Refresh admin users presentation

**Files:** Modify `src/WebServices/Homelab.Web/Components/Admin/Users.razor`.

- [ ] Preserve all existing data loading, filters, pagination, role/claim controls, notification controls, policy attribute, and destructive operation handlers.
- [ ] Replace local hard-coded colors with shared variables and reduce border/shadow intensity.
- [ ] Improve filter panel hierarchy, button grouping, user-card spacing, status badge contrast, and modal section spacing.
- [ ] Retain accessible labels, `aria` attributes, loading indicators, keyboard-operable cards, and modal close behavior.
- [ ] Validate layouts at 430px, 640px, 900px, 1200px, and desktop widths using the existing responsive media queries.

### Task 4: Refresh roles and claims pages

**Files:** Modify `src/WebServices/Homelab.Web/Components/Admin/Roles.razor` and `Claims.razor`.

- [ ] Preserve their named authorization policy and all existing service calls.
- [ ] Apply the shared page header, surface, table/form, badge, and action-button styling.
- [ ] Keep destructive actions visually distinct and confirmation behavior unchanged.
- [ ] Ensure long role names and claim values wrap or truncate without horizontal page overflow.

### Task 5: Verify the visual refresh

**Files:** No new production files. Review the files from Tasks 1–4.

- [ ] Run `dotnet build src/Homelab.sln -c Release --no-restore`; expected 0 errors.
- [ ] Run `dotnet test src/Homelab.Tests/Homelab.Tests.csproj -c Release --no-build --filter "FullyQualifiedName!~WebTests"`; expected all existing tests pass.
- [ ] Inspect all changed CSS for hard-coded colors that should be tokens, focus suppression, invalid media syntax, and selectors that affect account forms unexpectedly.
- [ ] Manually verify keyboard focus, admin link visibility, modal operation, mobile navigation, and reduced-motion behavior when a browser is available.
