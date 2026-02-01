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

### Sprint 3.1 – Plugin Error Handling & Resilience
- [ ] Create `PluginLoadResult` model [PENDING]
- [ ] Implement try-catch wrapper in `PluginLoader` [PENDING]
- [ ] Add `IPluginHealthMonitor` interface and implementation [PENDING]
- [ ] Create global exception middleware [PENDING]
- [ ] Add plugin status endpoint: `GET /admin/plugins/status` [PENDING]

### Sprint 3.2 – Configuration Management & DI Scoping
- [ ] Extend `IErpModule` interface for configuration validation [PENDING]
- [ ] Create `appsettings.Plugins.json` template [PENDING]
- [ ] Implement `PluginConfigurationValidator` [PENDING]
- [ ] Add DI conflict detection [PENDING]
- [ ] Document configuration strategy in `CONFIGURATION_GUIDE.md` [PENDING]

### Sprint 4 – Accounting Module (Plugin) & State Snapshots
- [x] Create `Accounting.Plugin` project [COMPLETED]
- [x] Implement `AccountingModule : IErpModule` [COMPLETED]
- [x] Define `LedgerEntry` model [COMPLETED]
- [x] Add `AccountingDbContext` (separate context) [COMPLETED]
- [x] Implement `SnapshotService` using `EntitySnapshot` table [COMPLETED]
- [x] Add minimal unit tests for snapshot serialize/deserialize (Integrated into Module tests) [COMPLETED]

### Sprint 4.1 – Database Migration Coordination
- [ ] Extend `IErpModule` with migration methods [PENDING]
- [ ] Create `PluginMigrations` history table [PENDING]
- [ ] Implement `MigrationCoordinator` (dependency graph, execution order) [PENDING]
- [ ] Add migration CLI commands (`migrate:plugins`, `rollback:plugin`) [PENDING]

## Phase 3 – UI Runtime Engine

### Sprint 5 – UI Persistence & Runtime Cache
- [ ] Implement `UiRenderCacheService` in `MyErpApp.UiRuntimeCache` [PENDING]
- [ ] Register `UiRenderCacheService` as `Singleton` in Host [PENDING]
- [ ] Implement `UiCachePreloader` hosted service [PENDING]
- [ ] On startup: Load all `UiPages` + their `UiComponents`, Generate HTML string per page and cache it [PENDING]
- [ ] Add integration test: Insert sample page in DB, Start app & ensure it's present in cache [PENDING]

### Sprint 5.1 – Cache Invalidation & Warming Strategy
- [ ] Extend `IUiRenderCacheService` with invalidation methods [PENDING]
- [ ] Implement `LruCacheStrategy` [PENDING]
- [ ] Add `RedisCacheAdapter` (optional/distributed) [PENDING]
- [ ] Create cache invalidation triggers (on save/manual) [PENDING]
- [ ] Add cache statistics and dashboard [PENDING]

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
- [ ] Implement "Add component" flow [PENDING]
- [ ] Implement sidebar binding [PENDING]
- [ ] Implement save call [PENDING]

### Sprint 8.1 – UI Versioning & State Management
- [ ] Extend `UiPages` table with status/version info [PENDING]
- [ ] Create `UiPageVersions` table [PENDING]
- [ ] Implement undo/redo in Blazor [PENDING]
- [ ] Add publish workflow [PENDING]
- [ ] Implement conflict detection for concurrent edits [PENDING]

### Sprint 9 – Load/Edit Existing Pages & Live Preview
- [ ] Implement API to retrieve a page with components by name [PENDING]
- [ ] In Designer: Load selected page, Map components to internal model for editing [PENDING]
- [ ] Implement preview component [PENDING]
- [ ] Add "Save & Preview" button [PENDING]

## Phase 5 – Security & Observability

### Sprint 10 – Security & Authorization Framework
- [ ] Implement authentication (JWT/Cookie) [PENDING]
- [ ] Implement Role-Based Access Control (RBAC) [PENDING]
- [ ] Add API Key management system [PENDING]
- [ ] Implement rate limiting middleware [PENDING]

### Sprint 10.1 – Plugin Sandboxing & Isolation
- [ ] Implement database access control per plugin [PENDING]
- [ ] Add resource monitoring (Memory/CPU per plugin) [PENDING]
- [ ] Implement plugin action audit trail [PENDING]
- [ ] Document security policies in `SECURITY_GUIDE.md` [PENDING]

### Sprint 11 – Observability, Logging & Health Checks
- [ ] Implement structured logging with correlation IDs [PENDING]
- [ ] Create health check framework (aggregate + per-plugin) [PENDING]
- [ ] Add metrics collection and OpenTelemetry integration [PENDING]
- [ ] Create metrics dashboard [PENDING]

### Sprint 11.1 – Plugin Versioning & Compatibility
- [ ] Create `PluginMetadataAttribute` [PENDING]
- [ ] Implement semantic versioning and compatibility checks [PENDING]
- [ ] Create `PluginDependencyResolver` [PENDING]
- [ ] Add plugin metadata and version history tables [PENDING]

## Phase 6 – Advanced Extensibility

### Sprint 12 – Additional UI Components & Renderer Plugins
- [ ] Design config structures for each new component type (JSON) [PENDING]
- [ ] Implement plugin classes for Button, Label, Select, Checkbox [PENDING]
- [ ] Test plugin discovery via Designer and runtime [PENDING]
- [ ] Implement `IUiRenderExtension` (Layout wrapper/analytics) [PENDING]

### Sprint 12.1 – API Versioning & Background Jobs
- [ ] Implement API versioning strategy (v1/v2) [PENDING]
- [ ] Create background job infrastructure (Hangfire/Quartz) [PENDING]
- [ ] Add scheduled task support per module [PENDING]
- [ ] Implement monitoring for long-running operations [PENDING]

### Sprint 13 – Testing Infrastructure & Harness
- [ ] Create `PluginTestHarness` utility [PENDING]
- [ ] Implement integration test base classes [PENDING]
- [ ] Add UI Designer test framework (simulation) [PENDING]
- [ ] Create test data generators [PENDING]

## Phase 7 – Hardening & Release

### Sprint 14 – Performance, Security, and Launch Prep
- [ ] Load test UI rendering to validate throughput [PENDING]
- [ ] Profile memory and optimize database queries [PENDING]
- [ ] Conduct security audit (SQLi, XSS, CSRF) [PENDING]
- [ ] Create production deployment checklist [PENDING]

### Sprint 15 – File Storage & Localization
- [ ] Create file storage abstraction and adapters (Local/Azure/S3) [PENDING]
- [ ] Implement file upload handling in UI Designer [PENDING]
- [ ] Add localization framework and multi-language support [PENDING]
- [ ] Document file storage strategy [PENDING]
