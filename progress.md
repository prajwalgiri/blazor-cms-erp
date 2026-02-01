# Project Progress

## Phase 1 – Foundation

### Sprint 1 – Project Bootstrap & Infrastructure
- [x] Create solution and base projects (MyErpApp.Host, Core, Infrastructure, UiRuntimeCache, UiRenderer, UiDesigner) [COMPLETED]
- [x] Configure DI, logging, configuration in `MyErpApp.Host` [COMPLETED]
- [x] Add `DEVELOPER_GUIDE.md` & `ROADMAP.md` to repo [COMPLETED]
- [ ] Integrate basic CI (build + tests on each commit) [PENDING]
- [x] Decide environment config strategy (User Secrets / appsettings per env) [COMPLETED]

### Sprint 2 – Core DB, EF, and Domain Skeleton
- [ ] Define core models (C#) to match schema [PENDING]
- [ ] Implement `AppDbContext` with DbSets: `UiPages`, `UiComponents`, `EntitySnapshots` [PENDING]
- [ ] Add EF Core tools and migration configuration [PENDING]
- [ ] Run `InitCoreSchema` migration and apply to dev DB [PENDING]
- [ ] Add minimal repository/service for reading/writing `UiPage` [PENDING]

## Phase 2 – Plugin & Module Core

### Sprint 3 – MEF Plugin System & Core Contracts
- [ ] Add MEF packages [PENDING]
- [ ] Implement `PluginLoader.LoadPlugins("plugins")` [PENDING]
- [ ] Define: `IErpModule`, `IUiComponentPlugin`, `IUiRenderExtension` [PENDING]
- [ ] At startup: Load exported modules and call `RegisterServices`, call `MapEndpoints` [PENDING]
- [ ] Implement a sample dummy module plugin (e.g. `HelloWorld.Plugin`) [PENDING]

### Sprint 4 – Accounting Module (Plugin) & State Snapshots
- [ ] Create `Accounting.Plugin` project [PENDING]
- [ ] Implement `AccountingModule : IErpModule` [PENDING]
- [ ] Define `LedgerEntry` model [PENDING]
- [ ] Add `AccountingDbContext` (if plugin DB separated) or reuse AppDbContext [PENDING]
- [ ] Implement `SnapshotService` using `EntitySnapshot` table [PENDING]
- [ ] Add minimal unit tests for snapshot serialize/deserialize [PENDING]

## Phase 3 – UI Runtime Engine

### Sprint 5 – UI Persistence & Runtime Cache
- [ ] Implement `UiRenderCacheService` in `MyErpApp.UiRuntimeCache` [PENDING]
- [ ] Register `UiRenderCacheService` as `Singleton` in Host [PENDING]
- [ ] Implement `UiCachePreloader` hosted service [PENDING]
- [ ] On startup: Load all `UiPages` + their `UiComponents`, Generate HTML string per page and cache it [PENDING]
- [ ] Add integration test: Insert sample page in DB, Start app & ensure it’s present in cache [PENDING]

### Sprint 6 – UI Rendering API & Preload
- [ ] Implement `UiRenderController` in `MyErpApp.UiRenderer` [PENDING]
- [ ] Inject `IUiRenderCacheService` and serve pages from cache [PENDING]
- [ ] Implement basic HTML wrapper if needed [PENDING]
- [ ] Log cache hits/misses for monitoring [PENDING]
- [ ] Add tests for render endpoints [PENDING]

## Phase 4 – UI Designer IDE

### Sprint 7 – UI Designer Shell & Component Palette
- [ ] Setup Blazor Server project and host integration [PENDING]
- [ ] Add Tailwind CDN to `index.html` / `_Host.cshtml` [PENDING]
- [ ] Inject MEF container or service that exposes component plugins into Blazor [PENDING]
- [ ] Build Toolbox that enumerates plugins and lists their `DisplayName` [PENDING]
- [ ] Mock selection of components and show selection event logs [PENDING]

### Sprint 8 – Canvas, Sidebar Config & Save Flow
- [ ] Implement component model on client [PENDING]
- [ ] Implement “Add component” flow [PENDING]
- [ ] Implement sidebar binding [PENDING]
- [ ] Implement save call [PENDING]

### Sprint 9 – Load/Edit Existing Pages & Live Preview
- [ ] Implement API to retrieve a page with components by name [PENDING]
- [ ] In Designer: Load selected page, Map components to internal model for editing [PENDING]
- [ ] Implement preview component [PENDING]
- [ ] Add “Save & Preview” button [PENDING]

## Phase 5 – Advanced Extensibility

### Sprint 10 – Additional UI Components & Renderer Plugins
- [ ] Design config structures for each new component type (JSON) [PENDING]
- [ ] Implement plugin classes [PENDING]
- [ ] Test plugin discovery via Designer and runtime [PENDING]
- [ ] Implement `IUiRenderExtension` [PENDING]
- [ ] Update runtime engine to invoke all registered `IUiRenderExtension` plugins in order [PENDING]

### Sprint 11 – Plugin Management & Observability
- [ ] On plugin discovery, record plugin info into `RegisteredPlugins` [PENDING]
- [ ] Expose read-only endpoint listing all plugins [PENDING]
- [ ] Add logging around plugin load and UI render [PENDING]
- [ ] Add basic metrics and dashboards [PENDING]

## Phase 6 – Hardening & Release

### Sprint 12 – Performance, Security & Launch Prep
- [ ] Load test UI rendering to validate throughput [PENDING]
- [ ] Profile memory for large number of pages/components [PENDING]
- [ ] Introduce optional eviction or config flags for cache strategy [PENDING]
- [ ] Integrate authentication (e.g. JWT, cookie-based) [PENDING]
- [ ] Add role-based authorization for designer and admin APIs [PENDING]
- [ ] Clean up logs, add error handling, and finalize documentation [PENDING]
