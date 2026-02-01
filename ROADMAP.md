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
- **Security, observability, and resilience features**

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
|                               | Sprint 3.1 | **Plugin Error Handling & Resilience** |
|                               | Sprint 3.2 | **Configuration Management & DI Scoping** |
|                               | Sprint 4 | Accounting Module (Plugin) & State Snapshots |
|                               | Sprint 4.1 | **Database Migration Coordination** |
| Phase 3 – UI Runtime Engine | Sprint 5 | UI Persistence & Runtime Cache |
|                             | Sprint 5.1 | **Cache Invalidation & Warming Strategy** |
|                             | Sprint 6 | UI Rendering API & Preload |
| Phase 4 – UI Designer IDE | Sprint 7 | UI Designer Shell & Component Palette |
|                           | Sprint 8 | Canvas, Sidebar Config & Save Flow |
|                           | Sprint 8.1 | **UI Versioning & State Management** |
|                           | Sprint 9 | Load/Edit Existing Pages & Preview |
| Phase 5 – Security & Observability | Sprint 10 | **Security & Authorization Framework** |
|                                    | Sprint 10.1 | **Plugin Sandboxing & Isolation** |
|                                    | Sprint 11 | **Observability, Logging & Health Checks** |
|                                    | Sprint 11.1 | **Plugin Versioning & Compatibility** |
| Phase 6 – Advanced Extensibility | Sprint 12 | Additional UI Components & Renderer Plugins |
|                                  | Sprint 12.1 | **API Versioning & Background Jobs** |
|                                  | Sprint 13 | **Testing Infrastructure & Harness** |
| Phase 7 – Hardening & Release | Sprint 14 | Performance, Security, and Launch Prep |
|                               | Sprint 15 | **File Storage & Localization** |

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
- [x] Create solution and base projects.
- [x] Configure DI, logging, configuration in `MyErpApp.Host`.
- [x] Add `DEVELOPER_GUIDE.md` & `ROADMAP.md` to repo.
- [x] Integrate basic CI (build + tests on each commit).
- [x] Decide environment config strategy (User Secrets / appsettings per env).

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
- [x] Define core models (C#) to match schema.
- [x] Implement `AppDbContext` with DbSets:
  - `UiPages`, `UiComponents`, `EntitySnapshots`.
- [x] Add EF Core tools and migration configuration.
- [x] Run `InitCoreSchema` migration and apply to dev DB.
- [x] Add minimal repository/service for reading/writing `UiPage`.

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

### 🛡️ Sprint 3.1 – Plugin Error Handling & Resilience

**Goal:** Add robust error handling for plugin loading and execution failures.

#### Objectives
- Implement graceful plugin load failure handling.
- Add circuit breaker pattern for unstable plugins.
- Create global exception handling middleware.
- Implement plugin health monitoring.

#### Deliverables
- `PluginLoadResult` class with success/failure tracking.
- `PluginHealthMonitor` service.
- Exception handling middleware for plugin endpoints.
- Logging for plugin failures.

#### Tasks
- [ ] Create `PluginLoadResult` model:
  - `Success`, `PluginName`, `Error`, `Status` (Loaded/Failed/Disabled)
- [ ] Implement try-catch wrapper in `PluginLoader`:
  - Log failures with stack traces.
  - Continue loading other plugins on failure.
- [ ] Add `IPluginHealthMonitor` interface and implementation:
  - Track plugin load time.
  - Monitor plugin endpoint failures.
  - Disable plugins after threshold failures.
- [ ] Create global exception middleware:
  - Catch plugin exceptions without crashing host.
  - Return proper HTTP status codes.
  - Log errors with plugin context.
- [ ] Add plugin status endpoint: `GET /admin/plugins/status`.

#### Milestone
- System remains stable even when individual plugins fail to load or crash.

---

### ⚙️ Sprint 3.2 – Configuration Management & DI Scoping

**Goal:** Implement proper configuration strategy and dependency injection isolation.

#### Objectives
- Define plugin-specific configuration sections.
- Implement service lifetime management per plugin.
- Prevent DI service conflicts between plugins.
- Add configuration validation.

#### Deliverables
- `IPluginConfiguration` interface.
- Configuration schema per plugin.
- Service registration conflict detection.
- Configuration validation on startup.

#### Tasks
- [ ] Extend `IErpModule` interface:
  ```csharp
  string GetConfigurationSection(); // Returns "Plugins:PluginName"
  void ValidateConfiguration(IConfiguration config);
  ServiceLifetime GetServiceLifetime(Type serviceType);
  bool AllowServiceOverride { get; }
  ```
- [ ] Create `appsettings.Plugins.json` template:
  ```json
  {
    "Plugins": {
      "Accounting": {
        "ConnectionString": "...",
        "Features": { "EnableSnapshots": true }
      }
    }
  }
  ```
- [ ] Implement `PluginConfigurationValidator`:
  - Check required config keys exist.
  - Validate connection strings.
  - Throw startup exception if invalid.
- [ ] Add DI conflict detection:
  - Warn if multiple plugins register same service.
  - Respect `AllowServiceOverride` flag.
- [ ] Document configuration strategy in `CONFIGURATION_GUIDE.md`.

#### Milestone
- Each plugin has isolated configuration and proper service registration.

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

### 🗄️ Sprint 4.1 – Database Migration Coordination

**Goal:** Implement coordinated migration strategy for multiple plugins.

#### Objectives
- Define migration execution order.
- Handle migration dependencies between plugins.
- Implement rollback strategy.
- Add pre-migration validation.

#### Deliverables
- `IMigrationCoordinator` service.
- Migration dependency graph.
- Rollback scripts per plugin.
- Migration status tracking.

#### Deliverables
- Extended `IErpModule` with migration methods.
- Migration coordinator service.
- Migration history table.

#### Tasks
- [ ] Extend `IErpModule`:
  ```csharp
  int MigrationPriority { get; } // Lower runs first
  IEnumerable<string> DependsOnModules { get; }
  Task<bool> CanMigrate(DbConnection connection);
  Task ApplyMigrations(DbContext context);
  Task RollbackMigrations(DbContext context);
  ```
- [ ] Create `PluginMigrations` table:
  ```sql
  CREATE TABLE PluginMigrations (
      PluginName NVARCHAR(255),
      MigrationName NVARCHAR(255),
      AppliedAt DATETIME2,
      Success BIT
  )
  ```
- [ ] Implement `MigrationCoordinator`:
  - Build dependency graph.
  - Validate no circular dependencies.
  - Execute migrations in order.
  - Track success/failure.
- [ ] Add migration CLI command: `dotnet run -- migrate:plugins`.
- [ ] Add rollback CLI command: `dotnet run -- rollback:plugin <name>`.

#### Milestone
- Multiple plugins can safely apply migrations without conflicts.

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
  - Start app & ensure it's present in cache.

#### Milestone
- All pages are preloaded into memory at startup and retrievable by page name.

---

### 🔄 Sprint 5.1 – Cache Invalidation & Warming Strategy

**Goal:** Implement intelligent cache management with invalidation and warming.

#### Objectives
- Add cache invalidation triggers.
- Implement cache eviction policies (LRU, TTL).
- Add distributed cache support (Redis).
- Implement cache warming strategy.

#### Deliverables
- Extended `IUiRenderCacheService` with invalidation methods.
- LRU cache implementation.
- Redis cache adapter.
- Cache statistics dashboard.

#### Tasks
- [ ] Extend `IUiRenderCacheService`:
  ```csharp
  void Invalidate(string pageName);
  void InvalidateAll();
  Task WarmCache(IEnumerable<string> pageNames);
  CacheStatistics GetStatistics();
  ```
- [ ] Implement `LruCacheStrategy`:
  - Max cache size configuration.
  - Automatic eviction of least-used pages.
- [ ] Add `RedisCacheAdapter : IUiRenderCacheService`:
  - Distributed cache for multi-instance deployments.
  - TTL configuration per page.
- [ ] Create cache invalidation triggers:
  - On `UiPage` save/update/delete.
  - Manual invalidation endpoint.
  - Scheduled full cache refresh.
- [ ] Add cache statistics:
  - Hit/miss ratio.
  - Average load time.
  - Memory usage.
- [ ] Create endpoint: `GET /admin/cache/stats`.

#### Milestone
- Cache can be managed dynamically without app restart.

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
- [ ] Implement "Add component" flow:
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

### 📝 Sprint 8.1 – UI Versioning & State Management

**Goal:** Add version control and state management to UI Designer.

#### Objectives
- Implement undo/redo functionality.
- Add page versioning (draft vs published).
- Support collaborative editing.
- Implement conflict resolution.

#### Deliverables
- Undo/redo stack in UI Designer.
- Page status workflow (Draft → Published → Archived).
- Version history per page.
- Conflict detection for concurrent edits.

#### Tasks
- [ ] Extend `UiPages` table:
  ```sql
  ALTER TABLE UiPages ADD
      Status NVARCHAR(50) DEFAULT 'Draft',
      Version INT DEFAULT 1,
      PublishedAt DATETIME2,
      PublishedBy NVARCHAR(255),
      ModifiedAt DATETIME2,
      ModifiedBy NVARCHAR(255)
  ```
- [ ] Create `UiPageVersions` table:
  ```sql
  CREATE TABLE UiPageVersions (
      Id UNIQUEIDENTIFIER PRIMARY KEY,
      PageId UNIQUEIDENTIFIER,
      Version INT,
      ComponentsSnapshot NVARCHAR(MAX),
      CreatedAt DATETIME2,
      CreatedBy NVARCHAR(255)
  )
  ```
- [ ] Implement undo/redo in Blazor:
  - Command pattern for UI actions.
  - Stack-based history.
- [ ] Add publish workflow:
  - `POST /api/uipages/{id}/publish`
  - Create version snapshot.
  - Update cache with published version.
- [ ] Implement conflict detection:
  - Track last modified timestamp.
  - Warn on concurrent edit.
  - Offer merge or override options.

#### Milestone
- UI Designer supports professional workflow with versioning and collaboration.

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
- [ ] Add "Save & Preview" button.

#### Milestone
- Full edit cycle: select page → edit → save → preview updated UI.

---

### 🔐 Sprint 10 – Security & Authorization Framework

**Goal:** Implement comprehensive security and authorization system.

#### Objectives
- Add authentication strategy.
- Implement role-based authorization.
- Add API key management.
- Implement rate limiting per module.

#### Deliverables
- Authentication middleware (JWT/Cookie).
- Role-based access control (RBAC).
- API key management system.
- Rate limiting middleware.

#### Tasks
- [ ] Extend `IErpModule`:
  ```csharp
  IEnumerable<string> RequiredPermissions { get; }
  bool RequireAuthentication { get; }
  IEnumerable<string> RequiredRoles { get; }
  ```
- [ ] Implement authentication:
  - JWT bearer token support.
  - Cookie-based auth for UI Designer.
  - User identity management.
- [ ] Create `PluginPermissions` table:
  ```sql
  CREATE TABLE PluginPermissions (
      PluginName NVARCHAR(255),
      Role NVARCHAR(100),
      Permission NVARCHAR(255)
  )
  ```
- [ ] Implement authorization middleware:
  - Check user roles against plugin requirements.
  - Return 403 for unauthorized access.
- [ ] Add API key management:
  - Generate keys per user/application.
  - Key rotation support.
  - Usage tracking.
- [ ] Implement rate limiting:
  - Per-user limits.
  - Per-plugin endpoint limits.
  - Configurable throttle policies.
- [ ] Add security endpoints:
  - `POST /auth/login`
  - `POST /auth/refresh`
  - `GET /auth/permissions`

#### Milestone
- All endpoints are secured with proper authentication and authorization.

---

### 🔒 Sprint 10.1 – Plugin Sandboxing & Isolation

**Goal:** Implement security boundaries between plugins.

#### Objectives
- Prevent plugins from accessing other plugin's data.
- Implement resource quotas per plugin.
- Add plugin permission policies.
- Implement audit logging for plugin actions.

#### Deliverables
- `PluginSecurityPolicy` system.
- Resource quota enforcement.
- Plugin action audit trail.
- Sandboxing documentation.

#### Tasks
- [ ] Create `PluginSecurityPolicy` model:
  ```csharp
  public class PluginSecurityPolicy
  {
      public string PluginName { get; set; }
      public List<string> AllowedEndpoints { get; set; }
      public List<string> AllowedDbTables { get; set; }
      public List<string> AllowedExternalDomains { get; set; }
      public int MaxMemoryMb { get; set; }
      public int MaxCpuPercent { get; set; }
  }
  ```
- [ ] Implement database access control:
  - Validate plugin can only access its own tables.
  - Shared tables require explicit permission.
- [ ] Add resource monitoring:
  - Track memory usage per plugin.
  - Track CPU usage per plugin.
  - Disable plugin if exceeds quota.
- [ ] Implement audit logging:
  - Log all plugin DB operations.
  - Log all plugin API calls.
  - Log all plugin file access.
- [ ] Create `PluginAuditLog` table:
  ```sql
  CREATE TABLE PluginAuditLog (
      Id UNIQUEIDENTIFIER PRIMARY KEY,
      PluginName NVARCHAR(255),
      Action NVARCHAR(255),
      Resource NVARCHAR(500),
      UserId NVARCHAR(255),
      Timestamp DATETIME2,
      Success BIT
  )
  ```
- [ ] Document security policies in `SECURITY_GUIDE.md`.

#### Milestone
- Plugins operate in isolated security contexts with auditing.

---

### 📊 Sprint 11 – Observability, Logging & Health Checks

**Goal:** Implement comprehensive observability and monitoring.

#### Objectives
- Add structured logging per plugin.
- Implement health checks per plugin.
- Add performance metrics.
- Integrate distributed tracing.

#### Deliverables
- Structured logging with correlation IDs.
- Health check endpoints per plugin.
- Metrics dashboard.
- APM integration ready.

#### Tasks
- [ ] Extend `IErpModule`:
  ```csharp
  IHealthCheck GetHealthCheck();
  void ConfigureMetrics(IMetricsBuilder metrics);
  void ConfigureLogging(ILoggingBuilder logging);
  ```
- [ ] Implement structured logging:
  - Use `ILogger<T>` with plugin name prefix.
  - Add correlation IDs to all logs.
  - Include plugin context in log scope.
- [ ] Create health check framework:
  - Each plugin reports health status.
  - Aggregate health endpoint: `GET /health`.
  - Individual plugin health: `GET /health/{pluginName}`.
- [ ] Add metrics collection:
  - Plugin load time.
  - Endpoint response time.
  - Cache hit/miss ratio.
  - Database query duration.
  - Memory usage per plugin.
- [ ] Integrate OpenTelemetry:
  - Distributed tracing support.
  - Export to Jaeger/Zipkin.
  - Trace plugin interactions.
- [ ] Create metrics dashboard:
  - Real-time plugin status.
  - Performance graphs.
  - Error rate tracking.
- [ ] Add endpoints:
  - `GET /metrics`
  - `GET /admin/monitoring/dashboard`

#### Milestone
- Full observability into plugin behavior and system performance.

---

### 📦 Sprint 11.1 – Plugin Versioning & Compatibility

**Goal:** Implement robust plugin versioning and compatibility management.

#### Objectives
- Define version compatibility rules.
- Implement breaking change detection.
- Add plugin dependency graph.
- Create compatibility testing framework.

#### Deliverables
- Plugin metadata attributes.
- Version compatibility matrix.
- Dependency resolver.
- Compatibility test suite.

#### Tasks
- [ ] Create `PluginMetadataAttribute`:
  ```csharp
  [AttributeUsage(AttributeTargets.Class)]
  public class PluginMetadataAttribute : Attribute
  {
      public string Name { get; set; }
      public string Version { get; set; }
      public string MinimumCoreVersion { get; set; }
      public string[] Dependencies { get; set; }
      public string[] BreakingChangesFrom { get; set; }
  }
  ```
- [ ] Implement semantic versioning:
  - Parse version strings (Major.Minor.Patch).
  - Compare versions for compatibility.
  - Detect breaking changes (major version bump).
- [ ] Create `PluginDependencyResolver`:
  - Build dependency graph.
  - Detect circular dependencies.
  - Validate all dependencies present.
  - Check version compatibility.
- [ ] Add plugin metadata table:
  ```sql
  CREATE TABLE PluginMetadata (
      PluginName NVARCHAR(255) PRIMARY KEY,
      Version NVARCHAR(50),
      MinimumCoreVersion NVARCHAR(50),
      Dependencies NVARCHAR(MAX), -- JSON array
      LoadedAt DATETIME2,
      Status NVARCHAR(50)
  )
  ```
- [ ] Implement compatibility testing:
  - Test plugin loads with different core versions.
  - Test plugin interactions across versions.
  - Generate compatibility report.
- [ ] Add version management endpoints:
  - `GET /admin/plugins/compatibility`
  - `GET /admin/plugins/{name}/dependencies`
- [ ] Document versioning strategy in `PLUGIN_MANIFEST.md`.

#### Milestone
- Plugins have clear version contracts and dependency management.

---

### 🧱 Sprint 12 – Additional UI Components & Renderer Plugins

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

### 🔧 Sprint 12.1 – API Versioning & Background Jobs

**Goal:** Add API versioning support and background job infrastructure.

#### Objectives
- Implement API versioning strategy.
- Add background job registration for plugins.
- Support scheduled tasks per module.
- Implement long-running operation support.

#### Deliverables
- API versioning middleware.
- Background job framework.
- Scheduled task system.
- Job monitoring dashboard.

#### Tasks
- [ ] Extend `IErpModule`:
  ```csharp
  string ApiVersion { get; } // "v1", "v2"
  void MapEndpoints(IEndpointRouteBuilder app, string version);
  void RegisterBackgroundJobs(IServiceCollection services);
  ```
- [ ] Implement API versioning:
  - URL-based versioning: `/api/v1/...`
  - Header-based versioning: `Api-Version: 2.0`
  - Support multiple versions simultaneously.
  - Deprecation warnings for old versions.
- [ ] Create background job infrastructure:
  - Use Hangfire or Quartz.NET.
  - Plugin-specific job queues.
  - Job retry policies.
  - Job cancellation support.
- [ ] Add scheduled task support:
  - Cron-based scheduling.
  - Plugin-defined schedules.
  - Task execution monitoring.
- [ ] Implement long-running operations:
  - Background task with progress tracking.
  - Cancellation token support.
  - Result storage.
- [ ] Add job monitoring:
  - `GET /admin/jobs/status`
  - `GET /admin/jobs/{id}/progress`
  - Job history and logs.
- [ ] Document API versioning strategy.

#### Milestone
- Plugins can expose versioned APIs and schedule background work.

---

### 🧪 Sprint 13 – Testing Infrastructure & Harness

**Goal:** Build comprehensive testing framework for plugins.

#### Objectives
- Create plugin test harness.
- Add integration test utilities.
- Implement UI Designer test framework.
- Create test data generators.

#### Deliverables
- `PluginTestHarness` utility class.
- Integration test base classes.
- UI test helpers.
- Test documentation.

#### Tasks
- [ ] Create `PluginTestHarness`:
  ```csharp
  public class PluginTestHarness
  {
      public static IServiceProvider CreateTestHost(IErpModule module);
      public static HttpClient CreateTestClient(IErpModule module);
      public static TestDatabase CreateTestDatabase();
      public static void CleanupTestData();
  }
  ```
- [ ] Implement test base classes:
  - `PluginIntegrationTestBase` - DB + DI setup.
  - `UiComponentTestBase` - Component rendering tests.
  - `ApiEndpointTestBase` - API endpoint tests.
- [ ] Add test utilities:
  - Mock plugin loader.
  - In-memory database for testing.
  - Test data builders.
  - HTTP client with auth.
- [ ] Create UI Designer testing:
  - Component drag-drop simulation.
  - Save flow testing.
  - Preview rendering validation.
- [ ] Implement test data generators:
  - Sample UiPages.
  - Sample ERP data (ledger entries, etc.).
  - User/role fixtures.
- [ ] Add example tests:
  - Plugin load test.
  - Endpoint authorization test.
  - Cache invalidation test.
  - UI component render test.
- [ ] Document testing strategy in `TESTING_GUIDE.md`.

#### Milestone
- Developers can easily test plugins with provided harness.

---

### 🛡️ Sprint 14 – Performance, Security & Launch Prep

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
- [ ] Security audit:
  - SQL injection protection.
  - XSS prevention in UI Designer.
  - CSRF protection.
  - Secure headers (HSTS, CSP, etc.).
- [ ] Performance optimizations:
  - Database query optimization.
  - Index creation.
  - Connection pooling.
  - Response compression.
- [ ] Create deployment checklist.
- [ ] Production configuration hardening.

#### Milestone
- System is stable, performant, and ready for pilot deployment.

---

### 📁 Sprint 15 – File Storage & Localization

**Goal:** Add file storage support and multi-language capabilities.

#### Objectives
- Implement file storage strategy for plugins.
- Add blob storage integration.
- Implement file upload handling in UI Designer.
- Add localization framework.

#### Deliverables
- File storage abstraction.
- Azure Blob/S3 adapters.
- File upload UI components.
- Multi-language support.

#### Tasks
- [ ] Create file storage abstraction:
  ```csharp
  public interface IFileStorageService
  {
      Task<string> UploadAsync(Stream file, string fileName, string pluginName);
      Task<Stream> DownloadAsync(string fileId);
      Task DeleteAsync(string fileId);
      Task<IEnumerable<FileMetadata>> ListAsync(string pluginName);
  }
  ```
- [ ] Implement storage adapters:
  - Local file system storage.
  - Azure Blob Storage adapter.
  - AWS S3 adapter.
- [ ] Add file metadata table:
  ```sql
  CREATE TABLE PluginFiles (
      Id UNIQUEIDENTIFIER PRIMARY KEY,
      PluginName NVARCHAR(255),
      FileName NVARCHAR(255),
      ContentType NVARCHAR(100),
      StoragePath NVARCHAR(500),
      SizeBytes BIGINT,
      UploadedAt DATETIME2,
      UploadedBy NVARCHAR(255)
  )
  ```
- [ ] Add file upload UI component plugin.
- [ ] Implement file access control:
  - Plugins can only access their own files.
  - Shared files require permission.
- [ ] Add localization framework:
  - Resource file support (.resx).
  - Database-driven translations.
  - Culture detection middleware.
- [ ] Implement localization for:
  - UI Designer interface.
  - Validation messages.
  - Plugin metadata.
- [ ] Add currency formatting support.
- [ ] Add timezone handling.
- [ ] Document file storage strategy.

#### Milestone
- Plugins can store files and system supports multiple languages.

---

## 3. Milestone Summary

| Milestone | Achieved By | Outcome |
|----------|-------------|---------|
| Core Infrastructure Ready | Sprint 2 | Host + DB + EF ready |
| Plugin System Live | Sprint 3 | MEF plugins loaded at startup |
| **Error Handling & Config** | **Sprint 3.2** | **Resilient plugin loading with proper configuration** |
| First ERP Module | Sprint 4 | Accounting + snapshots online |
| **Database Coordination** | **Sprint 4.1** | **Multi-plugin migrations coordinated** |
| UI Runtime Engine | Sprint 6 | Memory-first page rendering |
| **Cache Management** | **Sprint 5.1** | **Intelligent cache with invalidation** |
| Full Designer MVP | Sprint 9 | Design, save, load, preview pages |
| **UI Versioning** | **Sprint 8.1** | **Professional workflow with version control** |
| **Security Framework** | **Sprint 10.1** | **Complete auth/authz with plugin sandboxing** |
| **Observability** | **Sprint 11** | **Full monitoring and health checks** |
| **Plugin Versioning** | **Sprint 11.1** | **Robust version management** |
| Extended Components & Plugins | Sprint 12 | Rich UI + render extensions |
| **API Versioning & Jobs** | **Sprint 12.1** | **Versioned APIs with background processing** |
| **Testing Infrastructure** | **Sprint 13** | **Complete test harness** |
| Release Candidate | Sprint 14 | Performance & security hardened |
| **Complete Platform** | **Sprint 15** | **File storage and localization ready** |

---

## 4. Notes for Product & Tech Leads

- Sprints are **flexible**; scope can be adjusted, but **dependencies** should stay intact.
- **New sprints (X.1)** can run in parallel with main sprints if team capacity allows.
- Ensure **DEVELOPER_GUIDE.md** and this `ROADMAP.md` stay updated alongside actual implementation.
- For larger teams, some sprints can run partially in parallel (e.g., module work vs. UI Designer work), but the plugin contracts and core infrastructure must remain stable to avoid rework.
- **Security-focused sprints** (10, 10.1) are critical before any production deployment.
- **Testing infrastructure** (Sprint 13) should ideally be built progressively starting from Sprint 3.

---

## 5. Risk Mitigation

| Risk | Mitigation | Sprint |
|------|------------|--------|
| Plugin load failures crash system | Error handling framework | Sprint 3.1 |
| DI conflicts between plugins | Service registration validation | Sprint 3.2 |
| Migration conflicts | Coordinated migration system | Sprint 4.1 |
| Memory leaks in cache | LRU eviction + monitoring | Sprint 5.1 |
| Security vulnerabilities | Security framework + audit | Sprint 10-10.1 |
| Plugin compatibility issues | Versioning framework | Sprint 11.1 |
| Performance bottlenecks | Profiling + optimization | Sprint 14 |

---

_End of `ROADMAP.md`_