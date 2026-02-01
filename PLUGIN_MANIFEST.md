# 📦 ERP-CMS Platform – Plugin Manifest Guide

**File:** `PLUGIN_MANIFEST.md`  
**Owner:** Plugin Architect / Tech Lead  
**Audience:** Plugin Developers

---

## 1. Overview

This guide defines the **plugin metadata schema, versioning strategy, and dependency management** for the ERP-CMS platform.

Every plugin **must** include proper metadata to ensure:
- Version compatibility
- Dependency resolution
- Security validation
- Runtime behavior

---

## 2. Plugin Metadata Attribute

### 2.1 Required Metadata

```csharp
[PluginMetadata(
    Name = "Accounting",
    Version = "1.2.0",
    MinimumCoreVersion = "1.0.0",
    Dependencies = new[] { "AuditLog:1.0.0" },
    RequiredPermissions = new[] { "Accounting.Read", "Accounting.Write" },
    RequiredRoles = new[] { "Accountant", "Admin" }
)]
[Export(typeof(IErpModule))]
public class AccountingModule : IErpModule
{
    // Implementation
}
```

### 2.2 Metadata Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Name` | string | ✅ | Unique plugin identifier |
| `Version` | string | ✅ | Semantic version (Major.Minor.Patch) |
| `MinimumCoreVersion` | string | ✅ | Minimum compatible core version |
| `Dependencies` | string[] | ❌ | Array of "PluginName:Version" |
| `RequiredPermissions` | string[] | ❌ | Required security permissions |
| `RequiredRoles` | string[] | ❌ | Required user roles |
| `Description` | string | ❌ | Plugin description |
| `Author` | string | ❌ | Plugin author/vendor |
| `Website` | string | ❌ | Plugin documentation URL |
| `LicenseType` | string | ❌ | License (MIT, Commercial, etc.) |

---

## 3. Semantic Versioning (SemVer)

Plugins **must** follow [Semantic Versioning 2.0.0](https://semver.org/):

### 3.1 Version Format

```
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
```

Examples:
- `1.0.0` - Initial release
- `1.1.0` - New feature (backward compatible)
- `1.1.1` - Bug fix
- `2.0.0` - Breaking change
- `2.0.0-beta.1` - Pre-release
- `1.0.0+20230615` - Build metadata

### 3.2 Version Increment Rules

| Change Type | Version Part | Example |
|-------------|--------------|---------|
| Breaking API change | MAJOR | 1.0.0 → 2.0.0 |
| New feature (backward compatible) | MINOR | 1.0.0 → 1.1.0 |
| Bug fix | PATCH | 1.0.0 → 1.0.1 |

### 3.3 Breaking Changes

A **breaking change** includes:
- Removing public API endpoints
- Changing endpoint signatures
- Renaming database tables/columns
- Changing configuration schema
- Removing required permissions

**Non-breaking changes:**
- Adding new endpoints
- Adding optional parameters
- Adding new database tables
- Deprecating features (with warnings)

---

## 4. Plugin Dependencies

### 4.1 Dependency Declaration

```csharp
[PluginMetadata(
    Name = "Inventory",
    Version = "1.0.0",
    Dependencies = new[]
    {
        "Accounting:1.0.0",      // Exact version
        "AuditLog:^1.2.0",       // Compatible with 1.2.x
        "Notifications:~1.5.0"   // Compatible with 1.5.x (patch updates)
    }
)]
```

### 4.2 Version Constraints

| Syntax | Meaning | Example | Matches |
|--------|---------|---------|---------|
| `1.0.0` | Exact version | `1.0.0` | Only 1.0.0 |
| `^1.2.0` | Compatible (minor) | `^1.2.0` | 1.2.0 - 1.9.9 |
| `~1.5.0` | Compatible (patch) | `~1.5.0` | 1.5.0 - 1.5.9 |
| `>=1.0.0` | Greater or equal | `>=1.0.0` | 1.0.0+ |
| `1.0.0 - 2.0.0` | Range | `1.0.0 - 2.0.0` | 1.0.0 - 2.0.0 |

### 4.3 Dependency Resolution

The plugin loader resolves dependencies in this order:

1. **Detect all plugins** in `/plugins` directory
2. **Parse metadata** from each plugin
3. **Build dependency graph**
4. **Validate no circular dependencies**
5. **Topological sort** (dependencies load first)
6. **Check version compatibility**
7. **Load plugins in order**

### 4.4 Circular Dependency Detection

```csharp
public class PluginDependencyResolver
{
    public List<string> Resolve(Dictionary<string, PluginMetadata> plugins)
    {
        var graph = BuildGraph(plugins);
        
        // Detect cycles
        if (HasCycle(graph))
        {
            throw new InvalidOperationException(
                "Circular dependency detected: " + 
                string.Join(" -> ", GetCycle(graph)));
        }
        
        // Topological sort
        return TopologicalSort(graph);
    }
    
    private bool HasCycle(Dictionary<string, List<string>> graph)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        
        foreach (var node in graph.Keys)
        {
            if (HasCycleUtil(node, graph, visited, recursionStack))
                return true;
        }
        
        return false;
    }
}
```

---

## 5. Plugin Manifest File

### 5.1 Optional plugin.json

In addition to attributes, plugins can include a `plugin.json` file:

```json
{
  "name": "Accounting",
  "version": "1.2.0",
  "description": "Accounting and financial management module",
  "author": "ACME Corp",
  "website": "https://acme.com/plugins/accounting",
  "license": "MIT",
  "minimumCoreVersion": "1.0.0",
  "dependencies": {
    "AuditLog": "^1.0.0"
  },
  "permissions": [
    "Accounting.Read",
    "Accounting.Write",
    "Accounting.Delete"
  ],
  "roles": [
    "Accountant",
    "FinancialManager"
  ],
  "settings": {
    "defaultCurrency": "USD",
    "fiscalYearStart": "01-01"
  },
  "migrations": {
    "baseline": "InitAccounting",
    "current": "AddTaxSupport"
  },
  "resources": {
    "icon": "icon.png",
    "screenshots": ["screenshot1.png", "screenshot2.png"],
    "documentation": "README.md"
  }
}
```

### 5.2 Loading Manifest

```csharp
public class PluginManifest
{
    public static PluginManifest Load(string pluginDirectory)
    {
        var manifestPath = Path.Combine(pluginDirectory, "plugin.json");
        
        if (!File.Exists(manifestPath))
            return null;
        
        var json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<PluginManifest>(json);
    }
    
    public void Validate()
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("Plugin name is required");
        
        if (!SemanticVersion.TryParse(Version, out _))
            throw new InvalidOperationException($"Invalid version: {Version}");
        
        if (!SemanticVersion.TryParse(MinimumCoreVersion, out _))
            throw new InvalidOperationException($"Invalid minimum core version: {MinimumCoreVersion}");
    }
}
```

---

## 6. Compatibility Matrix

### 6.1 Core-Plugin Compatibility

| Core Version | Compatible Plugin Versions |
|--------------|---------------------------|
| 1.0.x | 1.0.0 - 1.9.9 |
| 2.0.x | 2.0.0+ |

### 6.2 Plugin-Plugin Compatibility

Plugins should declare breaking changes:

```csharp
[PluginMetadata(
    Name = "Accounting",
    Version = "2.0.0",
    BreakingChangesFrom = new[] { "1.x" },
    MigrationPath = "Upgrade from 1.x: Run migration script..."
)]
```

---

## 7. Plugin Lifecycle

### 7.1 States

```
Discovered → Validated → Loaded → Active → Disabled → Unloaded
```

### 7.2 State Transitions

```csharp
public enum PluginStatus
{
    Discovered,    // Found in directory
    Validated,     // Metadata validated
    Loaded,        // Assembly loaded
    Active,        // Registered and running
    Disabled,      // Disabled by admin or error
    Failed,        // Load/runtime error
    Unloaded       // Removed from memory
}
```

### 7.3 Status Tracking

```csharp
public class PluginStatusTracker
{
    public async Task UpdateStatus(string pluginName, PluginStatus status, string reason = null)
    {
        var record = await _db.PluginMetadata.FindAsync(pluginName);
        
        if (record == null)
        {
            record = new PluginMetadataRecord
            {
                PluginName = pluginName,
                Status = status.ToString(),
                StatusChangedAt = DateTime.UtcNow,
                StatusReason = reason
            };
            _db.PluginMetadata.Add(record);
        }
        else
        {
            record.Status = status.ToString();
            record.StatusChangedAt = DateTime.UtcNow;
            record.StatusReason = reason;
        }
        
        await _db.SaveChangesAsync();
        
        _logger.LogInformation(
            "Plugin {Plugin} status changed to {Status}: {Reason}",
            pluginName, status, reason);
    }
}
```

---

## 8. Plugin Upgrade Path

### 8.1 In-Place Upgrade

For **non-breaking** changes (MINOR/PATCH):

1. Stop plugin
2. Replace DLL
3. Restart plugin
4. Run migrations (if any)

### 8.2 Breaking Upgrade

For **breaking** changes (MAJOR):

1. Install new version alongside old
2. Migrate data
3. Update dependencies
4. Switch traffic to new version
5. Unload old version

### 8.3 Migration Script

```csharp
[PluginMetadata(Name = "Accounting", Version = "2.0.0")]
public class AccountingModule : IErpModule
{
    public async Task MigrateFromVersion(string oldVersion)
    {
        if (oldVersion.StartsWith("1."))
        {
            // Migrate from 1.x to 2.0
            await MigrateV1ToV2();
        }
    }
    
    private async Task MigrateV1ToV2()
    {
        _logger.LogInformation("Migrating Accounting from 1.x to 2.0...");
        
        // Rename table
        await _db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE LedgerEntries RENAME TO GeneralLedger");
        
        // Add new columns
        await _db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE GeneralLedger ADD TaxRate DECIMAL(5,2)");
        
        _logger.LogInformation("Migration completed");
    }
}
```

---

## 9. Plugin Registry

### 9.1 Database Schema

```sql
CREATE TABLE PluginMetadata (
    PluginName NVARCHAR(255) PRIMARY KEY,
    Version NVARCHAR(50) NOT NULL,
    MinimumCoreVersion NVARCHAR(50),
    AssemblyName NVARCHAR(255),
    AssemblyPath NVARCHAR(500),
    
    -- Dependencies
    Dependencies NVARCHAR(MAX), -- JSON array
    
    -- Security
    RequiredPermissions NVARCHAR(MAX), -- JSON array
    RequiredRoles NVARCHAR(MAX), -- JSON array
    
    -- Status
    Status NVARCHAR(50), -- Discovered, Loaded, Active, Disabled, Failed
    StatusChangedAt DATETIME2,
    StatusReason NVARCHAR(500),
    
    -- Lifecycle
    LoadedAt DATETIME2,
    DisabledAt DATETIME2,
    UnloadedAt DATETIME2,
    
    -- Metadata
    Description NVARCHAR(1000),
    Author NVARCHAR(255),
    Website NVARCHAR(500),
    LicenseType NVARCHAR(100),
    
    -- Checksums (for integrity)
    AssemblyChecksum NVARCHAR(64), -- SHA256
    ManifestChecksum NVARCHAR(64),
    
    INDEX IX_PluginMetadata_Status (Status),
    INDEX IX_PluginMetadata_LoadedAt (LoadedAt)
);
```

### 9.2 Plugin Registry Service

```csharp
public interface IPluginRegistry
{
    Task RegisterAsync(PluginMetadata metadata);
    Task<PluginMetadata> GetAsync(string pluginName);
    Task<IEnumerable<PluginMetadata>> GetAllAsync();
    Task<IEnumerable<PluginMetadata>> GetByStatusAsync(PluginStatus status);
    Task UpdateStatusAsync(string pluginName, PluginStatus status, string reason = null);
    Task UnregisterAsync(string pluginName);
}
```

---

## 10. Plugin Signing & Verification

### 10.1 Code Signing

For **production** environments, plugins should be signed:

```bash
# Sign assembly
signtool sign /f certificate.pfx /p password Accounting.Plugin.dll

# Verify signature
signtool verify /pa Accounting.Plugin.dll
```

### 10.2 Checksum Validation

```csharp
public class PluginValidator
{
    public bool ValidateChecksum(string assemblyPath, string expectedChecksum)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(assemblyPath);
        
        var hash = sha256.ComputeHash(stream);
        var actualChecksum = BitConverter.ToString(hash).Replace("-", "");
        
        return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }
}
```

---

## 11. Plugin Catalog & Marketplace

### 11.1 Catalog Schema

```json
{
  "catalog": {
    "version": "1.0",
    "plugins": [
      {
        "name": "Accounting",
        "versions": [
          {
            "version": "2.0.0",
            "releaseDate": "2024-01-15",
            "minimumCoreVersion": "1.0.0",
            "downloadUrl": "https://cdn.acme.com/plugins/accounting-2.0.0.zip",
            "checksum": "abc123...",
            "changelog": "Added tax support, fixed ledger bugs",
            "tags": ["finance", "accounting", "erp"],
            "rating": 4.5,
            "downloads": 1523
          }
        ]
      }
    ]
  }
}
```

### 11.2 Plugin Installation

```csharp
public class PluginInstaller
{
    public async Task<bool> InstallAsync(string pluginName, string version = null)
    {
        // 1. Fetch from catalog
        var catalog = await _catalogService.GetCatalogAsync();
        var plugin = catalog.FindPlugin(pluginName, version);
        
        if (plugin == null)
        {
            _logger.LogError("Plugin {Name} version {Version} not found", pluginName, version);
            return false;
        }
        
        // 2. Download
        var tempPath = await _downloader.DownloadAsync(plugin.DownloadUrl);
        
        // 3. Verify checksum
        if (!ValidateChecksum(tempPath, plugin.Checksum))
        {
            _logger.LogError("Checksum mismatch for {Plugin}", pluginName);
            return false;
        }
        
        // 4. Extract
        var pluginDir = Path.Combine(_pluginsPath, pluginName);
        ZipFile.ExtractToDirectory(tempPath, pluginDir);
        
        // 5. Validate dependencies
        var manifest = PluginManifest.Load(pluginDir);
        if (!await ValidateDependenciesAsync(manifest.Dependencies))
        {
            _logger.LogError("Dependency validation failed for {Plugin}", pluginName);
            return false;
        }
        
        // 6. Register
        await _registry.RegisterAsync(manifest);
        
        // 7. Load
        await _loader.LoadPluginAsync(pluginName);
        
        return true;
    }
}
```

---

## 12. Deprecation Policy

### 12.1 Deprecation Notice

```csharp
[Obsolete("Use CreateLedgerEntryV2 instead. This method will be removed in v3.0.0")]
public async Task<LedgerEntry> CreateLedgerEntry(LedgerEntryDto dto)
{
    // Old implementation
}

public async Task<LedgerEntry> CreateLedgerEntryV2(CreateLedgerEntryRequest request)
{
    // New implementation
}
```

### 12.2 Deprecation Timeline

| Version | Action |
|---------|--------|
| 1.5.0 | Mark as `[Obsolete]` with warning |
| 2.0.0 | Mark as `[Obsolete(error: true)]` |
| 3.0.0 | Remove deprecated API |

---

## 13. Plugin Development Checklist

Before releasing a plugin:

- [ ] Metadata complete with all required fields
- [ ] Version follows SemVer
- [ ] Dependencies declared correctly
- [ ] Permissions documented
- [ ] Unit tests written (70%+ coverage)
- [ ] Integration tests written
- [ ] Documentation complete
- [ ] Migration scripts tested
- [ ] Checksum generated
- [ ] Assembly signed (production)
- [ ] Change log updated
- [ ] Breaking changes documented
- [ ] Upgrade path tested

---

## 14. Example: Complete Plugin

```csharp
[assembly: AssemblyVersion("1.2.0")]

[PluginMetadata(
    Name = "Accounting",
    Version = "1.2.0",
    MinimumCoreVersion = "1.0.0",
    Description = "Financial accounting and ledger management",
    Author = "ACME Corp",
    Website = "https://acme.com/plugins/accounting",
    LicenseType = "MIT",
    Dependencies = new[] { "AuditLog:^1.0.0" },
    RequiredPermissions = new[] { "Accounting.Read", "Accounting.Write" },
    RequiredRoles = new[] { "Accountant", "Admin" },
    BreakingChangesFrom = new string[] { }
)]
[Export(typeof(IErpModule))]
public class AccountingModule : IErpModule
{
    public string Name => "Accounting";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddDbContext<AccountingDbContext>();
    }
    
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/accounting/ledger", GetLedger)
            .RequireAuthorization("Accounting.Read");
        
        app.MapPost("/accounting/ledger", CreateLedgerEntry)
            .RequireAuthorization("Accounting.Write");
    }
    
    public IHealthCheck GetHealthCheck()
    {
        return new AccountingHealthCheck();
    }
    
    public async Task ApplyMigrationsAsync(DbContext context)
    {
        await context.Database.MigrateAsync();
    }
}
```

---

_End of `PLUGIN_MANIFEST.md`_
