# ERP-CMS Platform – Product Roadmap & Sprint Plan  
**File:** `ROADMAP.md`  
**Owner:** Project Owner / Product Lead  
**Audience:** Tech Leads, Senior Developers, Architects

---

## 0. Overview

This roadmap describes the **end-to-end delivery plan** for the ERP-CMS platform:

- Modular monolith core
- MEF-based plugin system (ERP + UI components)
- Low-code UI Designer (drag-and-drop IDE)
- UI runtime engine (HTML + Tailwind, memory-first)
- State versioning & audit (snapshots)
- Extensible ERP modules (starting with Accounting)

Work is broken into **sprints**, each with clear **objectives, milestones, and concrete tasks**.

Assume:
- Standard sprint duration (e.g. 1–2 weeks)
- Core team of senior .NET developers + 1–2 front-end devs

---

## 1. Release Phases & Sprints Overview

| Phase | Sprint | Name |
|-------|--------|------|
| Phase 1 – Foundation | Sprint 1 | Project Bootstrap & Infrastructure |
|                       | Sprint 2 | Core DB, EF, and Domain Skeleton |
| Phase 2 – Plugin & Module Core | Sprint 3 | MEF Plugin System & Contracts |
|                               | Sprint 4 | Accounting Module (Plugin) & State Snapshots |
| Phase 3 – UI Runtime Engine | Sprint 5 | UI Persistence & Runtime Cache |
|                             | Sprint 6 | UI Rendering API & Preload |
| Phase 4 – UI Designer IDE | Sprint 7 | UI Designer Shell & Component Palette |
|                           | Sprint 8 | Canvas, Sidebar Config & Save Flow |
|                           | Sprint 9 | Load/Edit Existing Pages & Preview |
| Phase 5 – Advanced Extensibility | Sprint 10 | Additional UI Components & Renderer Plugins |
|                                  | Sprint 11 | Plugin Management & Observability |
| Phase 6 – Hardening & Release | Sprint 12 | Performance, Security, and Launch Prep |

---

## 2. Sprint-by-Sprint Plan

---

### 🧱 Sprint 1 – Project Bootstrap & Infrastructure

**Goal:** Establish solution structure, core projects, and basic CI.

#### Objectives
- Create initial solution structure for modular monolith + plugins.
- Set up build pipeline and coding standards.
- Prepare environment configs.

#### Deliverables
- `.sln` with projects:
  - `MyErpApp.Host`
  - `MyErpApp.Core`
  - `MyErpApp.Infrastructure`
  - `MyErpApp.UiRuntimeCache`
  - `MyErpApp.UiRenderer`
  - `MyErpApp.UiDesigner`
  - `Plugins/` folder (for plugin projects later)
- Coding guidelines & naming conventions in `DEVELOPER_GUIDE.md`.
- Basic CI pipeline (build, test).

#### Tasks
- [ ] Create solution and base projects.
- [ ] Configure DI, logging, configuration in `MyErpApp.Host`.
- [ ] Add `DEVELOPER_GUIDE.md` & `ROADMAP.md` to repo.
- [ ] Integrate basic CI (build + tests on each commit).
- [ ] Decide environment config strategy (User Secrets / appsettings per env).

#### Milestone
- Team can build & run the host with a health check endpoint.

---

### 🗂️ Sprint 2 – Core DB, EF, and Domain Skeleton

**Goal:** Implement core domain and database integration.

#### Objectives
- Define `AppDbContext` for core entities.
- Implement DB schema for:
  - `UiPages`
  - `UiComponents`
  - `EntitySnapshots`
  - (optional) `RegisteredPlugins`
- Prepare EF Core migrations.

#### Deliverables
- `AppDbContext` in `MyErpApp.Infrastructure`.
- Initial DB migration applied to dev DB.
- Entity classes:
  - `UiPage`
  - `UiComponent`
  - `EntitySnapshot`.

#### Tasks
- [ ] Define core models (C#) to match schema.
- [ ] Implement `AppDbContext` with DbSets:
  - `UiPages`, `UiComponents`, `EntitySnapshots`.
- [ ] Add EF Core tools and migration configuration.
- [ ] Run `InitCoreSchema` migration and apply to dev DB.
- [ ] Add minimal repository/service for reading/writing `UiPage`.

#### Milestone
- Core DB and EF infrastructure in place; app can CRUD basic `UiPages`.

---

### 🔌 Sprint 3 – MEF Plugin System & Core Contracts

**Goal:** Introduce plugin loading via MEF and core contracts for modules & UI components.

#### Objectives
- Implement MEF plugin loader.
- Define plugin contracts:
  - `IErpModule`
  - `IUiComponentPlugin`
  - `IUiRenderExtension`
- Wire plugins into host startup.

#### Deliverables
- `PluginLoader` utility in `MyErpApp.Core` or `MyErpApp.Infrastructure`.
- Contracts defined in `MyErpApp.Core`.
- Host loads all plugins from `/plugins` folder at startup.

#### Tasks
- [ ] Add MEF packages.
- [ ] Implement `PluginLoader.LoadPlugins("plugins")`.
- [ ] Define:
  - [ ] `IErpModule`
  - [ ] `IUiComponentPlugin`
  - [ ] `IUiRenderExtension`
- [ ] At startup:
  - Load exported modules and call `RegisterServices`.
  - After `app` build, call `MapEndpoints`.
- [ ] Implement a sample **dummy module plugin** (e.g. `HelloWorld.Plugin`) to validate flow.

#### Milestone
- On dropping a plugin DLL into `/plugins`, host discovers and wires endpoints without code change.

---

### 📒 Sprint 4 – Accounting Module (Plugin) & State Snapshots

**Goal:** Build first real ERP plugin (Accounting) and integrate snapshot versioning.

#### Objectives
- Create `Accounting.Plugin` project and implement `IErpModule`.
- Implement `LedgerEntry` entity and minimal API endpoints.
- Implement snapshot service using `EntitySnapshot`.

#### Deliverables
- `Accounting.Plugin` with:
  - `LedgerEntry` domain model (in plugin or shared).
  - Basic endpoints:
    - `GET /accounting/ledger`
    - `POST /accounting/ledger`
- `SnapshotService` capable of:
  - Creating snapshot from entity.
  - Restoring from snapshot JSON.

#### Tasks
- [ ] Create `Accounting.Plugin` project.
- [ ] Implement `AccountingModule : IErpModule`.
- [ ] Define `LedgerEntry` model.
- [ ] Add `AccountingDbContext` (if plugin DB separated) or reuse AppDbContext.
- [ ] Implement `SnapshotService` using `EntitySnapshot` table.
- [ ] Add minimal unit tests for snapshot serialize/deserialize.

#### Milestone
- Accounting module can create ledger entries and snapshot state for rollback.

---

### 🧠 Sprint 5 – UI Persistence & Runtime Cache

**Goal:** Persist UI pages & components and introduce memory-first cache.

#### Objectives
- Implement UI persistence for `UiPage` & `UiComponent`.
- Implement `UiRenderCacheService` as Singleton.
- Implement background preload service to load all pages into cache.

#### Deliverables
- `UiRenderCacheService` with:
  - `Refresh(IEnumerable<UiPage>)`
  - `GetHtml(pageName)`
  - `GetConfig(pageName)`
- `UiCachePreloader : IHostedService` that preloads pages at startup.

#### Tasks
- [ ] Implement `UiRenderCacheService` in `MyErpApp.UiRuntimeCache`.
- [ ] Register `UiRenderCacheService` as `Singleton` in Host.
- [ ] Implement `UiCachePreloader` hosted service.
- [ ] On startup:
  - Load all `UiPages` + their `UiComponents`.
  - Generate HTML string per page and cache it.
- [ ] Add integration test:
  - Insert sample page in DB.
  - Start app & ensure it’s present in cache.

#### Milestone
- All pages are preloaded into memory at startup and retrievable by page name.

---

### 🌐 Sprint 6 – UI Rendering API & Preload

**Goal:** Expose HTTP endpoints to render UI pages directly from cache.

#### Objectives
- Add `UI Render` controller/endpoint.
- Ensure no DB access in main render path.
- Add error handling for missing pages.

#### Deliverables
- Endpoint:
  - `GET /ui/render/{pageName}` → returns HTML (ContentResult).
- Logging for render events and cache misses.

#### Tasks
- [ ] Implement `UiRenderController` in `MyErpApp.UiRenderer`.
- [ ] Inject `IUiRenderCacheService` and serve pages from cache.
- [ ] Implement basic HTML wrapper if needed (e.g. `<html><head>…Tailwind…</head><body>{html}</body></html>`).
- [ ] Log cache hits/misses for monitoring.
- [ ] Add tests for render endpoints.

#### Milestone
- UI pages can be rendered directly from memory with single HTTP call.

---

### 🎨 Sprint 7 – UI Designer Shell & Component Palette

**Goal:** Bootstrap UI Designer app and basic component palette (no full drag & drop yet).

#### Objectives
- Create Blazor Server app for `MyErpApp.UiDesigner`.
- Integrate Tailwind CSS via CDN.
- Implement basic layout:
  - Left: toolbox (components from plugins).
  - Center: static canvas placeholder.
  - Right: empty sidebar.

#### Deliverables
- `Designer.razor` page with three-column layout.
- MEF-driven component list from `IUiComponentPlugin`.

#### Tasks
- [ ] Setup Blazor Server project and host integration.
- [ ] Add Tailwind CDN to `index.html` / `_Host.cshtml`.
- [ ] Inject MEF container or service that exposes component plugins into Blazor.
- [ ] Build Toolbox that enumerates plugins and lists their `DisplayName`.
- [ ] Mock selection of components and show selection event logs (no persistence yet).

#### Milestone
- UI Designer shows real plugin-based components in the toolbox.

---

### 🧲 Sprint 8 – Canvas, Sidebar Config & Save Flow

**Goal:** Implement core design canvas, component configuration editing, and save page to DB.

#### Objectives
- Implement canvas list that reflects components placed by user.
- Implement sidebar for editing basic config (e.g. placeholder, label, text).
- Save UI layout as `UiPage` + `UiComponents` to DB.
- Update runtime cache on save.

#### Deliverables
- `Toolbox.razor`, `Canvas.razor`, `SidebarConfig.razor` functional.
- `POST /api/uipages/save` backend endpoint.
- Cache refresh logic on save.

#### Tasks
- [ ] Implement component model on client:
  - `Id`, `Type`, `TailwindHtml`, `ConfigJson`.
- [ ] Implement “Add component” flow:
  - Pick type from toolbox.
  - Use plugin `DefaultConfig` to initialize.
  - Use plugin `RenderHtml(config)` to render initial HTML.
- [ ] Implement sidebar binding:
  - Load config JSON into editing model.
  - Update config and regenerate HTML via plugin.
- [ ] Implement save call:
  - Build `UiPage` object.
  - POST to API to persist.
  - Update `UiRenderCacheService` with new HTML.

#### Milestone
- Users can design a page and persist it; renderer can render updated page without restart.

---

### 🧩 Sprint 9 – Load/Edit Existing Pages & Live Preview

**Goal:** Support editing existing pages and live preview inside the Designer.

#### Objectives
- Implement page selector in UI Designer (list saved pages).
- Load page from DB into canvas.
- Implement embedded preview area rendering full page.

#### Deliverables
- Page dropdown or explorer in Designer.
- API:
  - `GET /api/uipages/{name}` → returns `UiPage` + components.
- Live preview panel that:
  - Renders final HTML using same plugins used at runtime.

#### Tasks
- [ ] Implement API to retrieve a page with components by name.
- [ ] In Designer:
  - Load selected page.
  - Map components to internal model for editing.
- [ ] Implement preview component:
  - Either:
    - Reuse `RenderHtml(config)` for each component, OR
    - Call runtime render endpoint and embed inside an iframe.
- [ ] Add “Save & Preview” button.

#### Milestone
- Full edit cycle: select page → edit → save → preview updated UI.

---

### 🧱 Sprint 10 – Additional UI Components & Renderer Plugins

**Goal:** Expand UI capabilities and introduce render extensions.

#### Objectives
- Add more UI Component Plugins:
  - `Button`
  - `Label`
  - `Select`
  - `Checkbox`
- Implement at least one `IUiRenderExtension` (e.g. injecting tracking or wrapper layout).

#### Deliverables
- `Ui.Button.Plugin`, `Ui.Label.Plugin`, etc.
- `HtmlWrapperRendererPlugin : IUiRenderExtension`.

#### Tasks
- [ ] Design config structures for each new component type (JSON).
- [ ] Implement plugin classes.
- [ ] Test plugin discovery via Designer and runtime.
- [ ] Implement `IUiRenderExtension` that:
  - Wraps page HTML with standardized layout or analytics instrumentation.
- [ ] Update runtime engine to invoke all registered `IUiRenderExtension` plugins in order.

#### Milestone
- System supports richer UIs and pluggable render extensions.

---

### 🛰️ Sprint 11 – Plugin Management & Observability

**Goal:** Improve control and visibility over plugins, performance, and errors.

#### Objectives
- Introduce `RegisteredPlugins` table or in-memory registry.
- Add endpoint or admin UI showing loaded plugins and versions.
- Add basic monitoring/metrics around:
  - Cache load time
  - Render latency
  - Plugin load failures

#### Deliverables
- `/admin/plugins` API (or simple UI).
- Logging of:
  - Plugin load success/fail.
  - Snapshot operations.
  - Render performance.

#### Tasks
- [ ] On plugin discovery, record plugin info into `RegisteredPlugins`.
- [ ] Expose read-only endpoint listing all plugins.
- [ ] Add logging around plugin load and UI render.
- [ ] Add basic metrics and dashboards (or hooks for APM).

#### Milestone
- Operators can view plugin state and basic performance metrics.

---

### 🛡️ Sprint 12 – Performance, Security & Launch Prep

**Goal:** Optimize performance, tighten security, and prepare for first stable release.

#### Objectives
- Optimize memory usage in `UiRenderCacheService`.
- Add caching strategies (e.g. LRU, max size).
- Secure endpoints (auth/authz).
- Conduct security and load tests.

#### Deliverables
- Performance test report & adjustments.
- Basic authorization around:
  - UI Designer
  - Admin endpoints
  - Sensitive ERP endpoints.
- Hardened deployment profile.

#### Tasks
- [ ] Load test UI rendering to validate throughput.
- [ ] Profile memory for large number of pages/components.
- [ ] Introduce optional eviction or config flags for cache strategy.
- [ ] Integrate authentication (e.g. JWT, cookie-based) as appropriate.
- [ ] Add role-based authorization for designer and admin APIs.
- [ ] Clean up logs, add error handling, and finalize documentation.

#### Milestone
- System is stable, performant, and ready for pilot deployment.

---

## 3. Milestone Summary

| Milestone | Achieved By | Outcome |
|----------|-------------|---------|
| Core Infrastructure Ready | Sprint 2 | Host + DB + EF ready |
| Plugin System Live | Sprint 3 | MEF plugins loaded at startup |
| First ERP Module | Sprint 4 | Accounting + snapshots online |
| UI Runtime Engine | Sprint 6 | Memory-first page rendering |
| Full Designer MVP | Sprint 9 | Design, save, load, preview pages |
| Extended Components & Plugins | Sprint 10–11 | Rich UI + render extensions |
| Release Candidate | Sprint 12 | Performance & security hardened |

---

## 4. Notes for Product & Tech Leads

- Sprints are **flexible**; scope can be adjusted, but **dependencies** should stay intact.
- Ensure **DEVELOPER_GUIDE.md** and this `ROADMAP.md` stay updated alongside actual implementation.
- For larger teams, some sprints can run partially in parallel (e.g., module work vs. UI Designer work), but the plugin contracts and core infrastructure must remain stable to avoid rework.

---

_End of `ROADMAP.md`_
