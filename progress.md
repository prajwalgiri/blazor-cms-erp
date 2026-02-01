# Project Progress

## Phase 1 – Foundation

### Sprint 1 – Project Bootstrap & Infrastructure
- [x] Create solution and base projects (MyErpApp.Host, Core, Infrastructure, UiRuntimeCache, UiRenderer, UiDesigner) [COMPLETED]
- [x] Configure DI, logging, configuration in `MyErpApp.Host` [COMPLETED]
- [x] Add `DEVELOPER_GUIDE.md` & `ROADMAP.md` to repo [COMPLETED]
- [x] Integrate basic CI (build script) [COMPLETED]
- [x] Decide environment config strategy (User Secrets / appsettings per env) [COMPLETED]

### Sprint 2 – Core DB, EF, and Domain Skeleton
- [x] Define core models (C#) to match schema [COMPLETED]
- [x] Implement `AppDbContext` with DbSets: `UiPages`, `UiComponents`, `EntitySnapshots` [COMPLETED]
- [x] Add EF Core tools and migration configuration [COMPLETED]
- [x] Run `InitCoreSchema` migration and apply to dev DB [COMPLETED]
- [x] Add minimal repository/service for reading/writing `UiPage` [COMPLETED]

## Phase 2 – Plugin & Module Core

### Sprint 3 – MEF Plugin System & Core Contracts
- [x] Add MEF packages [COMPLETED]
- [x] Implement `PluginLoader.LoadPlugins("plugins")` [COMPLETED]
- [x] Define: `IErpModule`, `IUiComponentPlugin`, `IUiRenderExtension` [COMPLETED]
- [x] At startup: Load exported modules and call `RegisterServices`, call `MapEndpoints` [COMPLETED]
- [x] Implement a sample dummy module plugin (e.g. `HelloWorld.Plugin`) [COMPLETED]

### Sprint 4 – Accounting Module (Plugin) & State Snapshots
- [x] Create `Accounting.Plugin` project [COMPLETED]
- [x] Implement `AccountingModule : IErpModule` [COMPLETED]
- [x] Define `LedgerEntry` model [COMPLETED]
- [x] Add `AccountingDbContext` (separate context) [COMPLETED]
- [x] Implement `SnapshotService` using `EntitySnapshot` table [COMPLETED]
- [x] Add minimal unit tests for snapshot serialize/deserialize (Integrated into Module tests) [COMPLETED]

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
