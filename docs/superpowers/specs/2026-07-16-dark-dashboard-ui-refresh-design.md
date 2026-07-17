# Homelab Dark Dashboard UI Refresh Design

## Goal

Refresh the Homelab.Web interface into a cleaner, calmer dark dashboard while preserving all existing Blazor behavior, Identity flows, authorization boundaries, and Bootstrap/Blazor Bootstrap dependencies.

## Design Direction

Use a layered dark blue-slate palette with a restrained warm-gold accent. Gold is reserved for primary actions, active navigation, and important focus states. Cards, dialogs, and forms should read as elevated surfaces without heavy borders or excessive shadows.

The visual hierarchy is:

1. Page background: deepest slate, low contrast.
2. Sidebar/navigation: darker than content with a clear active item.
3. Content surfaces: elevated blue-slate cards and panels.
4. Primary action: warm gold with dark text.
5. Status colors: accessible green, amber, red, and muted blue used only for status meaning.

## Scope

### Global theme

Add CSS custom properties to `src/WebServices/Homelab.Web/wwwroot/app.css` for page/surface colors, text hierarchy, borders, accent colors, status colors, radii, shadows, spacing, and focus rings. Update Bootstrap-compatible selectors for buttons, links, controls, alerts, cards, modals, list groups, accordions, tables, badges, and validation messages.

Use `color-scheme: dark`. Preserve readable contrast for body text and controls. Add `@media (prefers-reduced-motion: reduce)` to remove non-essential transitions and transforms.

### Navigation

Update `Components/Layout/NavMenu.razor` and its CSS to provide:

- consistent icon width and label alignment;
- a visible rounded active state;
- grouped authenticated/admin links;
- a distinct account section and logout action;
- unchanged authorization checks and antiforgery behavior.

Do not expose admin links to non-admin users. Keep the current route names.

### Layout

Update `MainLayout.razor.css` and related layout styles for a more generous content gutter, responsive sidebar behavior, and consistent page-width constraints. Do not change routing, render modes, or authentication state handling.

### Admin users page

Retain the existing user catalog, filters, pagination, user detail modal, role/claim controls, notification form, and safety behavior. Improve only presentation:

- use a compact elevated filter panel;
- improve card spacing, metadata grouping, and status badge hierarchy;
- reduce border weight and use hover elevation sparingly;
- make the modal header and sections easier to scan;
- preserve keyboard focus, button labels, loading indicators, and confirmation semantics;
- keep responsive layouts usable at 430px, 640px, 900px, 1200px, and desktop widths.

### Roles and claims pages

Apply the same surface, typography, form, table, badge, and action-button language to `Roles.razor` and `Claims.razor`. Keep role/claim operations and named-policy authorization unchanged.

## Library Decision

Do not introduce Tailwind, MudBlazor, Radzen, or another UI framework in this refresh. Bootstrap and Blazor Bootstrap are already used throughout the project; adding a competing system would increase bundle size, visual inconsistency, and migration risk. The existing packages are sufficient when paired with a tokenized theme layer.

Do not upgrade packages solely for appearance. Package updates remain part of the separate dependency-maintenance work and must be independently verified.

## Accessibility and Safety

- Preserve semantic headings, labels, form descriptions, and `aria` attributes.
- Keep visible focus indicators with a minimum 3:1 focus-indicator contrast against adjacent colors.
- Do not communicate status by color alone; retain text labels.
- Keep destructive actions visually distinct but not visually dominant.
- Do not move destructive actions into hover-only controls.
- Ensure disabled/loading states remain understandable without relying on animation.
- Test keyboard navigation through the sidebar, filters, cards, modal, forms, and action buttons.

## Validation

The refresh is complete when:

- the Web project and full solution build without new errors;
- existing non-container tests remain passing;
- the app renders correctly at desktop, tablet, and mobile widths;
- admin routes remain policy-protected;
- no admin or account functionality is removed;
- focus, contrast, reduced-motion, and keyboard behavior are manually checked;
- no new UI framework or unnecessary package dependency is introduced.

## Out of Scope

This design does not change authentication, Identity behavior, authorization, data models, API contracts, routes, startup configuration, or admin workflows. It does not add theme switching, light mode, new dashboards, new components, or new business functionality.
