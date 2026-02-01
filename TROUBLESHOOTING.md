# 🔧 ERP-CMS Platform – Troubleshooting Guide

**File:** `TROUBLESHOOTING.md`  
**Owner:** Support Team / SRE  
**Audience:** Developers, Support Engineers, Operations

---

## 1. Overview

This guide provides **solutions to common issues** encountered during development, deployment, and runtime of the ERP-CMS platform.

---

## 2. Plugin Issues

### 2.1 Plugin Fails to Load

**Symptoms:**
- Plugin not appearing in `/admin/plugins`
- Error in logs: `Failed to load plugin: [PluginName]`

**Possible Causes & Solutions:**

#### Cause 1: Missing MEF Export Attribute

```csharp
// ❌ WRONG - Missing [Export]
public class AccountingModule : IErpModule { }

// ✅ CORRECT
[Export(typeof(IErpModule))]
public class AccountingModule : IErpModule { }
```

#### Cause 2: Missing Dependencies

**Error:**
```
Could not load file or assembly 'Newtonsoft.Json, Version=13.0.0.0'
```

**Solution:**
```bash
# Check plugin dependencies
dotnet list package --include-transitive

# Ensure all dependencies are copied to output
# In .csproj:
<PropertyGroup>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```

#### Cause 3: Wrong Target Framework

**Error:**
```
Could not load file or assembly... The module was expected to contain an assembly manifest.
```

**Solution:**
```xml
<!-- Plugin must target same framework as host -->
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

#### Cause 4: Plugin Directory Permissions

**Error:**
```
Access to the path '/app/plugins/Accounting.Plugin.dll' is denied.
```

**Solution:**
```bash
# Fix permissions (Linux/Docker)
chmod -R 755 /app/plugins

# Fix ownership
chown -R appuser:appuser /app/plugins
```

---

### 2.2 Plugin Loads But Endpoints Not Working

**Symptoms:**
- Plugin shows as "Loaded" in admin panel
- 404 error when calling plugin endpoints

**Diagnosis:**

```csharp
// Check if MapEndpoints was called
public class AccountingModule : IErpModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        Console.WriteLine("Mapping Accounting endpoints..."); // Debug log
        
        app.MapGet("/accounting/ledger", () => "Test")
            .WithName("GetLedger"); // Add name for debugging
    }
}

// Verify endpoints registered
app.MapGet("/debug/endpoints", (IEnumerable<EndpointDataSource> sources) =>
{
    var endpoints = sources.SelectMany(s => s.Endpoints);
    return Results.Json(endpoints.Select(e => new
    {
        DisplayName = e.DisplayName,
        RoutePattern = (e as RouteEndpoint)?.RoutePattern.RawText
    }));
});
```

**Solution:**
- Ensure `MapEndpoints` is called **after** `app.Build()`
- Check route conflicts with existing endpoints
- Verify authorization requirements don't block access

---

### 2.3 Plugin Crashes Application

**Symptoms:**
- App crashes on startup or when plugin loads
- Unhandled exception in plugin code

**Solution:**

Implement plugin error isolation:

```csharp
public class SafePluginLoader
{
    public async Task<PluginLoadResult> LoadPluginAsync(string pluginPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(pluginPath);
            var plugin = LoadPlugin(assembly);
            
            // Try to call RegisterServices in isolated context
            try
            {
                plugin.RegisterServices(_services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {Name} RegisterServices failed", plugin.Name);
                return PluginLoadResult.Failed(plugin.Name, ex);
            }
            
            return PluginLoadResult.Success(plugin.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from {Path}", pluginPath);
            return PluginLoadResult.Failed(Path.GetFileName(pluginPath), ex);
        }
    }
}
```

---

### 2.4 Circular Dependency Between Plugins

**Error:**
```
Circular dependency detected: PluginA -> PluginB -> PluginA
```

**Diagnosis:**

```csharp
var resolver = new PluginDependencyResolver();
try
{
    var loadOrder = resolver.Resolve(plugins);
}
catch (CircularDependencyException ex)
{
    Console.WriteLine($"Cycle: {string.Join(" -> ", ex.Cycle)}");
}
```

**Solution:**
- Refactor to remove circular dependency
- Extract shared functionality into a third plugin
- Use events/messaging instead of direct dependencies

```csharp
// ❌ BAD - Circular dependency
PluginA depends on PluginB
PluginB depends on PluginA

// ✅ GOOD - Extract common functionality
PluginA depends on SharedServices
PluginB depends on SharedServices
```

---

## 3. Database Issues

### 3.1 Migration Fails

**Error:**
```
There is already an object named 'UiPages' in the database.
```

**Solution:**

```bash
# Check current migration status
dotnet ef migrations list --project src/MyErpApp.Infrastructure

# Remove failed migration
dotnet ef migrations remove --project src/MyErpApp.Infrastructure

# Or force update to specific migration
dotnet ef database update [MigrationName] --project src/MyErpApp.Infrastructure

# Nuclear option - reset database (DEV ONLY!)
dotnet ef database drop --force --project src/MyErpApp.Infrastructure
dotnet ef database update --project src/MyErpApp.Infrastructure
```

---

### 3.2 Connection Pool Exhausted

**Error:**
```
Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool.
```

**Symptoms:**
- Slow database queries
- High CPU on database server
- App becomes unresponsive

**Diagnosis:**

```csharp
// Add connection pool logging
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
    
    // Log connection pool stats
    options.LogTo(Console.WriteLine, LogLevel.Information);
});
```

**Solutions:**

1. **Increase pool size:**
```
Server=...;Database=...;Max Pool Size=200;
```

2. **Fix connection leaks:**
```csharp
// ❌ BAD - Connection leak
public void BadMethod()
{
    var connection = new SqlConnection(connectionString);
    connection.Open();
    // ... no dispose!
}

// ✅ GOOD - Proper disposal
public async Task GoodMethod()
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    // ... automatically disposed
}
```

3. **Use async methods:**
```csharp
// ❌ BAD - Blocks thread
var data = context.Users.ToList();

// ✅ GOOD - Async
var data = await context.Users.ToListAsync();
```

---

### 3.3 Deadlock Detected

**Error:**
```
Transaction was deadlocked on lock resources with another process and has been chosen as the deadlock victim.
```

**Diagnosis:**

```sql
-- Check for blocking sessions
SELECT 
    blocking_session_id,
    wait_type,
    wait_time,
    wait_resource
FROM sys.dm_exec_requests
WHERE blocking_session_id <> 0;

-- Get deadlock graph
SELECT * FROM sys.dm_xe_session_targets
WHERE event_session_name = 'system_health';
```

**Solutions:**

1. **Consistent lock order:**
```csharp
// ❌ BAD - Different lock order causes deadlock
// Transaction 1: Lock A, then B
// Transaction 2: Lock B, then A

// ✅ GOOD - Same lock order
// Always lock in alphabetical/ID order
var entity1 = await context.Entities.FindAsync(Math.Min(id1, id2));
var entity2 = await context.Entities.FindAsync(Math.Max(id1, id2));
```

2. **Use optimistic concurrency:**
```csharp
public class UiPage
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}

try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // Reload and retry
    await context.Entry(entity).ReloadAsync();
}
```

---

## 4. Cache Issues

### 4.1 Cache Not Refreshing

**Symptoms:**
- UI changes not appearing after save
- Stale data served to users

**Diagnosis:**

```csharp
app.MapGet("/debug/cache/stats", (IUiRenderCacheService cache) =>
{
    var stats = cache.GetStatistics();
    return Results.Json(new
    {
        TotalPages = stats.TotalPages,
        LastRefresh = stats.LastRefreshTime,
        HitRate = stats.HitRate
    });
});
```

**Solutions:**

1. **Manual cache invalidation:**
```bash
curl -X POST http://localhost:5000/admin/cache/invalidate \
  -H "Content-Type: application/json" \
  -d '{"pageName": "LoginPage"}'
```

2. **Check invalidation triggers:**
```csharp
public async Task SavePageAsync(UiPage page)
{
    await _repository.SaveAsync(page);
    
    // Ensure this is called!
    _cache.Invalidate(page.Name);
}
```

3. **Verify Singleton registration:**
```csharp
// ❌ WRONG - New instance each time
services.AddScoped<IUiRenderCacheService, UiRenderCacheService>();

// ✅ CORRECT - Single instance
services.AddSingleton<IUiRenderCacheService, UiRenderCacheService>();
```

---

### 4.2 Out of Memory Errors

**Error:**
```
System.OutOfMemoryException: Exception of type 'System.OutOfMemoryException' was thrown.
```

**Diagnosis:**

```csharp
// Monitor memory usage
app.MapGet("/debug/memory", () =>
{
    var gc = GC.GetGCMemoryInfo();
    var process = Process.GetCurrentProcess();
    
    return Results.Json(new
    {
        HeapSizeBytes = gc.HeapSizeBytes,
        MemoryLoadBytes = gc.MemoryLoadBytes,
        WorkingSetBytes = process.WorkingSet64,
        PrivateMemoryBytes = process.PrivateMemorySize64
    });
});
```

**Solutions:**

1. **Implement LRU eviction:**
```csharp
public class LruCacheService : IUiRenderCacheService
{
    private readonly int _maxSize;
    private readonly LinkedList<string> _accessOrder = new();
    private readonly Dictionary<string, (string Html, LinkedListNode<string> Node)> _cache = new();
    
    public void Add(string key, string html)
    {
        if (_cache.Count >= _maxSize)
        {
            // Evict least recently used
            var lru = _accessOrder.First.Value;
            _cache.Remove(lru);
            _accessOrder.RemoveFirst();
        }
        
        var node = _accessOrder.AddLast(key);
        _cache[key] = (html, node);
    }
}
```

2. **Configure cache size:**
```json
{
  "UiCache": {
    "MaxSizeBytes": 536870912,  // 512 MB
    "MaxPages": 1000,
    "EvictionPolicy": "LRU"
  }
}
```

---

## 5. UI Designer Issues

### 5.1 Components Not Appearing in Toolbox

**Symptoms:**
- Toolbox is empty or missing components
- Component plugins not discovered

**Diagnosis:**

```csharp
app.MapGet("/debug/components", (IEnumerable<IUiComponentPlugin> components) =>
{
    return Results.Json(components.Select(c => new
    {
        c.Type,
        c.DisplayName,
        AssemblyName = c.GetType().Assembly.GetName().Name
    }));
});
```

**Solutions:**

1. **Check MEF exports:**
```csharp
// ❌ WRONG
public class TextBoxComponent : IUiComponentPlugin { }

// ✅ CORRECT
[Export(typeof(IUiComponentPlugin))]
public class TextBoxComponent : IUiComponentPlugin { }
```

2. **Verify plugin directory:**
```bash
ls -la /app/plugins/
# Should contain Ui.*.Plugin.dll files
```

---

### 5.2 Drag and Drop Not Working

**Symptoms:**
- Cannot drag components to canvas
- Drop events not firing

**Solution (Blazor):**

```razor
@* Ensure proper event handlers *@
<div class="component"
     draggable="true"
     @ondragstart="@(() => OnDragStart(component))">
    @component.DisplayName
</div>

<div class="canvas"
     @ondragover="@OnDragOver"
     @ondragover:preventDefault
     @ondrop="@OnDrop"
     @ondrop:preventDefault>
    @* Canvas content *@
</div>

@code {
    private void OnDragOver(DragEventArgs e)
    {
        // Must call preventDefault via attribute
    }
    
    private void OnDrop(DragEventArgs e)
    {
        // Handle drop
    }
}
```

---

## 6. Authentication Issues

### 6.1 JWT Token Invalid

**Error:**
```
401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token"
```

**Diagnosis:**

```bash
# Decode JWT (use jwt.io or cli tool)
echo "eyJhbGc..." | base64 -d

# Check token expiration
jq -R 'split(".") | .[1] | @base64d | fromjson' <<< "$TOKEN"
```

**Solutions:**

1. **Token expired:**
```csharp
// Refresh token
var refreshRequest = new RefreshTokenRequest
{
    RefreshToken = oldToken
};

var newToken = await client.PostAsJsonAsync("/auth/refresh", refreshRequest);
```

2. **Wrong secret key:**
```csharp
// Ensure same key used for signing and validation
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:SecretKey"])),
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"]
        };
    });
```

---

### 6.2 CORS Errors

**Error (Browser Console):**
```
Access to fetch at 'https://api.example.com' from origin 'https://app.example.com' 
has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header
```

**Solution:**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", builder =>
    {
        builder
            .WithOrigins("https://app.example.com", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();  // Required for cookies/auth
    });
});

// IMPORTANT: Order matters!
app.UseCors("AllowedOrigins");
app.UseAuthentication();
app.UseAuthorization();
```

---

## 7. Performance Issues

### 7.1 Slow Query Performance

**Symptoms:**
- API response time > 1 second
- Database CPU high

**Diagnosis:**

```csharp
// Enable EF Core query logging
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
        .LogTo(Console.WriteLine, LogLevel.Information);
});
```

**Solutions:**

1. **Add indexes:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<UiPage>()
        .HasIndex(p => p.Name);  // Index frequently queried columns
    
    modelBuilder.Entity<UiComponent>()
        .HasIndex(c => c.PageId);
}
```

2. **Avoid N+1 queries:**
```csharp
// ❌ BAD - N+1 problem
var pages = await context.UiPages.ToListAsync();
foreach (var page in pages)
{
    var components = await context.UiComponents
        .Where(c => c.PageId == page.Id)
        .ToListAsync();  // Separate query for each page!
}

// ✅ GOOD - Single query with Include
var pages = await context.UiPages
    .Include(p => p.Components)
    .ToListAsync();
```

3. **Use AsNoTracking for read-only:**
```csharp
// Faster for read-only scenarios
var pages = await context.UiPages
    .AsNoTracking()
    .ToListAsync();
```

---

### 7.2 High Memory Usage

**Diagnosis:**

```bash
# Monitor memory over time
dotnet counters monitor --process-id [PID] \
  System.Runtime \
  Microsoft.AspNetCore.Hosting

# Take memory dump
dotnet dump collect --process-id [PID]

# Analyze dump
dotnet dump analyze [dump-file]
```

**Solutions:**

1. **Dispose resources:**
```csharp
// Implement IDisposable/IAsyncDisposable
public class PluginManager : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        foreach (var plugin in _loadedPlugins)
        {
            await UnloadPluginAsync(plugin);
        }
    }
}
```

2. **Limit cache size** (see 4.2)

3. **Use object pooling:**
```csharp
services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
services.AddSingleton(sp =>
{
    var provider = sp.GetRequiredService<ObjectPoolProvider>();
    return provider.Create(new DefaultPooledObjectPolicy<StringBuilder>());
});
```

---

## 8. Deployment Issues

### 8.1 Health Check Fails After Deployment

**Error:**
```
Health check failed: HTTP 503 Service Unavailable
```

**Diagnosis:**

```bash
# Check application logs
az webapp log tail --name myerp-app --resource-group myerp-rg

# Check health endpoint directly
curl https://myerp-app.azurewebsites.net/health/ready -v
```

**Solutions:**

1. **Database migration not applied:**
```bash
# Run migrations as part of deployment
dotnet ef database update --project src/MyErpApp.Infrastructure
```

2. **Missing configuration:**
```bash
# Check app settings
az webapp config appsettings list --name myerp-app --resource-group myerp-rg
```

3. **Startup timeout:**
```csharp
// Increase startup timeout in health check
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        // Return immediately during startup, allow 60s grace period
        if (DateTime.UtcNow - _startTime < TimeSpan.FromSeconds(60))
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Starting up...");
            return;
        }
        
        // Normal health check logic
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
    }
});
```

---

## 9. Common Error Messages

### "Plugin assembly could not be loaded"

**Fix:** Ensure plugin targets same framework as host (net8.0)

### "DBContext not registered for plugin"

**Fix:** Call `services.AddDbContext<T>` in plugin's `RegisterServices` method

### "MEF container initialization failed"

**Fix:** Check all plugins have valid metadata and exports

### "Cache initialization timeout"

**Fix:** Reduce number of pages or implement progressive loading

### "Migration has already been applied"

**Fix:** Use `dotnet ef migrations remove` or `dotnet ef database update [previous-migration]`

---

## 10. Debug Endpoints

Add these to development environment:

```csharp
if (app.Environment.IsDevelopment())
{
    // List all loaded plugins
    app.MapGet("/debug/plugins", (IEnumerable<IErpModule> modules) =>
        Results.Json(modules.Select(m => m.Name)));
    
    // List all endpoints
    app.MapGet("/debug/endpoints", (IEnumerable<EndpointDataSource> sources) =>
        Results.Json(sources.SelectMany(s => s.Endpoints).Select(e => e.DisplayName)));
    
    // Cache statistics
    app.MapGet("/debug/cache", (IUiRenderCacheService cache) =>
        Results.Json(cache.GetStatistics()));
    
    // Database connection test
    app.MapGet("/debug/db", async (AppDbContext db) =>
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Json(new { canConnect });
    });
}
```

---

## 11. Getting Help

1. **Check logs first:**
   - Application Insights
   - Azure App Service logs
   - Console output

2. **Use debug endpoints** (see section 10)

3. **Enable verbose logging:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information",
      "MyErpApp": "Trace"
    }
  }
}
```

4. **Create issue with:**
   - Error message
   - Stack trace
   - Steps to reproduce
   - Environment details

---

_End of `TROUBLESHOOTING.md`_
