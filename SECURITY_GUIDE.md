# 🔐 ERP-CMS Platform – Security Guide

**File:** `SECURITY_GUIDE.md`  
**Owner:** Security Architect / Tech Lead  
**Audience:** Developers, DevOps, Security Team

---

## 1. Overview

This guide covers **security architecture, policies, and best practices** for the ERP-CMS platform, with special focus on:

- Plugin sandboxing and isolation
- Authentication and authorization
- Data protection
- API security
- Audit logging
- Threat modeling

---

## 2. Security Architecture

### 2.1 Defense in Depth Layers

```
┌─────────────────────────────────────┐
│   1. Network Security (TLS, WAF)    │
├─────────────────────────────────────┤
│   2. Authentication (JWT/OAuth)     │
├─────────────────────────────────────┤
│   3. Authorization (RBAC)           │
├─────────────────────────────────────┤
│   4. Plugin Sandboxing              │
├─────────────────────────────────────┤
│   5. Data Encryption (at rest/transit) │
├─────────────────────────────────────┤
│   6. Audit Logging                  │
└─────────────────────────────────────┘
```

---

## 3. Plugin Security Model

### 3.1 Plugin Sandboxing

Each plugin operates in a **restricted security context**:

```csharp
public class PluginSecurityPolicy
{
    public string PluginName { get; set; }
    
    // Database Access Control
    public List<string> AllowedDbTables { get; set; }
    public bool CanAccessSharedTables { get; set; }
    
    // API Access Control
    public List<string> AllowedEndpoints { get; set; }
    public List<string> AllowedExternalDomains { get; set; }
    
    // Resource Quotas
    public int MaxMemoryMb { get; set; }
    public int MaxCpuPercent { get; set; }
    public int MaxConcurrentRequests { get; set; }
    
    // File System Access
    public List<string> AllowedDirectories { get; set; }
    public bool CanExecuteNativeCode { get; set; }
}
```

### 3.2 Database Access Control

#### Rules:
1. **Default Deny**: Plugins can only access their own tables by default
2. **Explicit Permissions**: Shared tables require explicit permission in policy
3. **Row-Level Security**: Implement RLS where plugins share tables
4. **Read-Only Access**: Mark certain tables as read-only for specific plugins

#### Implementation:

```csharp
public class PluginDbContextInterceptor : DbCommandInterceptor
{
    private readonly IPluginSecurityService _security;
    
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, 
        CommandEventData eventData, 
        InterceptionResult<DbDataReader> result)
    {
        var pluginName = GetCurrentPluginName();
        var tables = ExtractTableNames(command.CommandText);
        
        foreach (var table in tables)
        {
            if (!_security.CanAccessTable(pluginName, table))
            {
                throw new SecurityException(
                    $"Plugin '{pluginName}' not authorized to access table '{table}'");
            }
        }
        
        return result;
    }
}
```

### 3.3 Resource Quotas

Monitor and enforce resource limits per plugin:

```csharp
public class PluginResourceMonitor : IHostedService
{
    public async Task MonitorResourceUsage()
    {
        foreach (var plugin in _loadedPlugins)
        {
            var usage = GetResourceUsage(plugin.Name);
            
            if (usage.MemoryMb > plugin.Policy.MaxMemoryMb)
            {
                await DisablePlugin(plugin.Name, "Memory limit exceeded");
                _logger.LogCritical(
                    "Plugin {Plugin} disabled: memory limit {Limit}MB exceeded",
                    plugin.Name, plugin.Policy.MaxMemoryMb);
            }
            
            if (usage.CpuPercent > plugin.Policy.MaxCpuPercent)
            {
                await ThrottlePlugin(plugin.Name);
                _logger.LogWarning(
                    "Plugin {Plugin} throttled: CPU limit {Limit}% exceeded",
                    plugin.Name, plugin.Policy.MaxCpuPercent);
            }
        }
    }
}
```

---

## 4. Authentication

### 4.1 Supported Methods

| Method | Use Case | Implementation |
|--------|----------|----------------|
| **JWT Bearer** | API clients, SPAs | Token-based auth |
| **Cookie Auth** | UI Designer, Admin | Session-based auth |
| **API Keys** | Service-to-service | Header-based auth |
| **OAuth 2.0** | Third-party integrations | External provider |

### 4.2 JWT Configuration

```json
{
  "Jwt": {
    "SecretKey": "[SECURE_KEY_FROM_VAULT]",
    "Issuer": "MyErpApp",
    "Audience": "MyErpApp.Api",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "RequireHttpsMetadata": true
  }
}
```

### 4.3 Implementation

```csharp
public class AuthenticationService
{
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        // 1. Validate credentials
        var user = await _userRepository.FindByUsernameAsync(request.Username);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            await LogFailedLogin(request.Username);
            return AuthResult.Failed("Invalid credentials");
        }
        
        // 2. Check account status
        if (user.IsLocked)
        {
            return AuthResult.Failed("Account locked");
        }
        
        // 3. Generate tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        
        // 4. Store refresh token
        await StoreRefreshToken(user.Id, refreshToken);
        
        // 5. Log successful login
        await LogSuccessfulLogin(user.Id);
        
        return AuthResult.Success(accessToken, refreshToken);
    }
    
    private string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("tenant_id", user.TenantId.ToString())
        };
        
        // Add role claims
        foreach (var role in user.Roles)
        {
            claims = claims.Append(new Claim(ClaimTypes.Role, role));
        }
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## 5. Authorization

### 5.1 Role-Based Access Control (RBAC)

#### Predefined Roles:

| Role | Permissions | Description |
|------|-------------|-------------|
| **SystemAdmin** | All | Full system access |
| **PluginAdmin** | Plugin management | Install/uninstall plugins |
| **Designer** | UI Designer access | Create/edit UI pages |
| **Developer** | API access | Call plugin APIs |
| **Viewer** | Read-only | View UI pages |

### 5.2 Permission Model

```csharp
public class Permission
{
    public string Resource { get; set; }     // "UiPages", "Accounting"
    public string Action { get; set; }       // "Read", "Write", "Delete"
    public string Scope { get; set; }        // "Own", "Tenant", "Global"
}

public static class Permissions
{
    public const string UiPages_Read = "UiPages.Read";
    public const string UiPages_Write = "UiPages.Write";
    public const string UiPages_Delete = "UiPages.Delete";
    public const string UiPages_Publish = "UiPages.Publish";
    
    public const string Plugins_View = "Plugins.View";
    public const string Plugins_Install = "Plugins.Install";
    public const string Plugins_Uninstall = "Plugins.Uninstall";
    public const string Plugins_Configure = "Plugins.Configure";
}
```

### 5.3 Plugin-Level Authorization

```csharp
[PluginMetadata(
    Name = "Accounting",
    RequiredPermissions = new[] { "Accounting.Read", "Accounting.Write" },
    RequiredRoles = new[] { "Accountant", "Admin" }
)]
public class AccountingModule : IErpModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/accounting/ledger", GetLedger)
            .RequireAuthorization(policy => 
                policy.RequirePermission("Accounting.Read"));
                
        app.MapPost("/accounting/ledger", CreateLedgerEntry)
            .RequireAuthorization(policy => 
                policy.RequirePermission("Accounting.Write"));
    }
}
```

### 5.4 Custom Authorization Policies

```csharp
public class PluginAuthorizationHandler : AuthorizationHandler<PluginPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PluginPermissionRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var pluginName = requirement.PluginName;
        
        // Check if user has permission for this plugin
        if (_permissionService.HasPermission(userId, pluginName, requirement.Permission))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}
```

---

## 6. Data Protection

### 6.1 Encryption at Rest

**Sensitive fields** must be encrypted:

```csharp
[Encrypted]
public class UserCredentials
{
    public string PasswordHash { get; set; }  // Already hashed
    
    [Encrypted]
    public string ApiKey { get; set; }        // Encrypted in DB
    
    [Encrypted]
    public string ConnectionString { get; set; }  // Encrypted config
}
```

**Implementation:**

```csharp
public class EncryptedFieldConverter : ValueConverter<string, string>
{
    private readonly IDataProtectionProvider _protector;
    
    public EncryptedFieldConverter(IDataProtectionProvider protector)
        : base(
            v => Encrypt(v, protector),
            v => Decrypt(v, protector))
    {
    }
    
    private static string Encrypt(string plaintext, IDataProtectionProvider protector)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;
        var p = protector.CreateProtector("EncryptedFields");
        return p.Protect(plaintext);
    }
    
    private static string Decrypt(string ciphertext, IDataProtectionProvider protector)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;
        var p = protector.CreateProtector("EncryptedFields");
        return p.Unprotect(ciphertext);
    }
}
```

### 6.2 Encryption in Transit

- **Enforce HTTPS** for all endpoints
- **TLS 1.2+** only
- **HSTS** enabled

```csharp
app.UseHttpsRedirection();
app.UseHsts();

services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
```

### 6.3 Secrets Management

**Never store secrets in code or config files.**

Use:
- **Azure Key Vault**
- **AWS Secrets Manager**
- **HashiCorp Vault**
- **User Secrets** (dev only)

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var keyVaultUrl = Configuration["KeyVault:Url"];
    var credential = new DefaultAzureCredential();
    var client = new SecretClient(new Uri(keyVaultUrl), credential);
    
    var dbSecret = await client.GetSecretAsync("DatabaseConnectionString");
    Configuration["ConnectionStrings:Default"] = dbSecret.Value.Value;
}
```

---

## 7. API Security

### 7.1 Rate Limiting

Prevent abuse with rate limiting:

```csharp
services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10
        });
    });
});
```

### 7.2 CORS Policy

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", builder =>
    {
        builder
            .WithOrigins(Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

### 7.3 Input Validation

Always validate and sanitize input:

```csharp
public class CreateLedgerEntryRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Description { get; set; }
    
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }
    
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$")]
    public string Date { get; set; }
}

// Use FluentValidation for complex rules
public class LedgerEntryValidator : AbstractValidator<CreateLedgerEntryRequest>
{
    public LedgerEntryValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(200)
            .Must(NotContainSqlKeywords)
            .WithMessage("Invalid description");
    }
}
```

### 7.4 SQL Injection Prevention

**Always use parameterized queries:**

```csharp
// ❌ BAD - SQL Injection vulnerability
var sql = $"SELECT * FROM Users WHERE Username = '{username}'";

// ✅ GOOD - Parameterized query
var user = await _context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Username = {0}", username)
    .FirstOrDefaultAsync();

// ✅ BETTER - LINQ (automatically parameterized)
var user = await _context.Users
    .Where(u => u.Username == username)
    .FirstOrDefaultAsync();
```

### 7.5 XSS Prevention

Sanitize HTML in UI Designer:

```csharp
public class HtmlSanitizer
{
    private static readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();
    
    static HtmlSanitizer()
    {
        // Only allow safe tags and attributes
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("div");
        _sanitizer.AllowedTags.Add("span");
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("input");
        
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("id");
        
        // Block dangerous attributes
        _sanitizer.AllowedAttributes.Remove("onclick");
        _sanitizer.AllowedAttributes.Remove("onerror");
    }
    
    public static string Sanitize(string html)
    {
        return _sanitizer.Sanitize(html);
    }
}
```

---

## 8. Audit Logging

### 8.1 What to Log

Log all security-relevant events:

- User login/logout
- Authentication failures
- Authorization failures
- Plugin load/unload
- Configuration changes
- Data access (sensitive tables)
- File uploads
- API calls to sensitive endpoints

### 8.2 Audit Log Schema

```sql
CREATE TABLE AuditLog (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL,
    UserId NVARCHAR(255),
    Username NVARCHAR(255),
    IpAddress NVARCHAR(45),
    Action NVARCHAR(100) NOT NULL,
    Resource NVARCHAR(255),
    ResourceId NVARCHAR(255),
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(MAX),
    AdditionalData NVARCHAR(MAX), -- JSON
    
    INDEX IX_AuditLog_Timestamp (Timestamp),
    INDEX IX_AuditLog_UserId (UserId),
    INDEX IX_AuditLog_Action (Action)
);
```

### 8.3 Implementation

```csharp
public class AuditLogService : IAuditLogService
{
    public async Task LogAsync(AuditLogEntry entry)
    {
        entry.Timestamp = DateTime.UtcNow;
        entry.IpAddress = _httpContext.Connection.RemoteIpAddress?.ToString();
        
        await _context.AuditLogs.AddAsync(entry);
        await _context.SaveChangesAsync();
        
        // Also log to external SIEM if configured
        await _siemLogger.LogAsync(entry);
    }
}

// Usage
await _audit.LogAsync(new AuditLogEntry
{
    UserId = userId,
    Action = "Plugin.Install",
    Resource = "Accounting.Plugin",
    Success = true,
    AdditionalData = JsonSerializer.Serialize(new { Version = "1.0.0" })
});
```

### 8.4 Audit Middleware

```csharp
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditLogService _audit;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var originalBodyStream = context.Response.Body;
        
        try
        {
            await _next(context);
            
            // Log successful request
            if (ShouldAudit(context))
            {
                await LogRequest(context, startTime, success: true);
            }
        }
        catch (Exception ex)
        {
            // Log failed request
            await LogRequest(context, startTime, success: false, error: ex.Message);
            throw;
        }
    }
    
    private bool ShouldAudit(HttpContext context)
    {
        // Audit sensitive endpoints
        var path = context.Request.Path.Value?.ToLower();
        return path?.Contains("/admin/") == true ||
               path?.Contains("/accounting/") == true ||
               context.Request.Method != "GET";
    }
}
```

---

## 9. Security Headers

### 9.1 Required Headers

```csharp
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    
    // XSS protection
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' 'unsafe-inline' cdn.tailwindcss.com; style-src 'self' 'unsafe-inline' cdn.tailwindcss.com;");
    
    // Referrer policy
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Permissions policy
    context.Response.Headers.Add("Permissions-Policy", 
        "geolocation=(), microphone=(), camera=()");
    
    await next();
});
```

---

## 10. Threat Model

### 10.1 Identified Threats

| Threat | Risk | Mitigation |
|--------|------|------------|
| Malicious plugin | HIGH | Sandboxing, code review, signing |
| SQL Injection | HIGH | Parameterized queries, ORM |
| XSS in UI Designer | HIGH | HTML sanitization, CSP |
| Credential theft | HIGH | Password hashing, MFA |
| DoS attack | MEDIUM | Rate limiting, quotas |
| Plugin conflicts | MEDIUM | Dependency resolution |
| Data exfiltration | HIGH | Access control, audit logging |

### 10.2 Security Checklist

Before deploying a plugin:

- [ ] Code review completed
- [ ] No hardcoded secrets
- [ ] Input validation implemented
- [ ] Parameterized queries only
- [ ] Authorization checks on all endpoints
- [ ] Resource quotas configured
- [ ] Audit logging enabled
- [ ] Security policy defined
- [ ] Dependencies scanned for vulnerabilities
- [ ] Penetration testing completed

---

## 11. Incident Response

### 11.1 Security Incident Procedure

1. **Detection**: Monitor logs, alerts
2. **Containment**: Disable affected plugin
3. **Analysis**: Review audit logs, identify scope
4. **Eradication**: Remove malicious code, patch vulnerability
5. **Recovery**: Restore from backup if needed
6. **Post-Incident**: Update policies, improve defenses

### 11.2 Emergency Plugin Disable

```csharp
// Emergency disable endpoint (admin only)
app.MapPost("/admin/plugins/{name}/emergency-disable", 
    async (string name, IPluginManager manager) =>
{
    await manager.DisablePlugin(name, reason: "Security incident");
    
    await _audit.LogAsync(new AuditLogEntry
    {
        Action = "Plugin.EmergencyDisable",
        Resource = name,
        Success = true
    });
    
    return Results.Ok();
})
.RequireAuthorization("SystemAdmin");
```

---

## 12. Security Training

All developers must:

1. Complete OWASP Top 10 training
2. Understand plugin security model
3. Know how to use audit logging
4. Understand authentication/authorization flows
5. Know incident response procedures

---

## 13. Compliance

This system should comply with:

- **GDPR** (if handling EU data)
- **SOC 2 Type II** (for enterprise clients)
- **PCI DSS** (if handling payment data)
- **HIPAA** (if handling health data)

---

## 14. Regular Security Tasks

| Task | Frequency | Owner |
|------|-----------|-------|
| Dependency vulnerability scan | Weekly | DevOps |
| Audit log review | Daily | Security Team |
| Access review | Monthly | Admin |
| Penetration testing | Quarterly | Security Team |
| Security training | Annually | All Developers |
| Policy review | Annually | Security Architect |

---

_End of `SECURITY_GUIDE.md`_
