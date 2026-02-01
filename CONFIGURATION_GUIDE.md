# Configuration & DI Strategy Guide

This document outlines how ERP modules and UI components should handle configuration and dependency injection to ensure isolation and resilience.

## 1. Plugin Configuration

Each module can have its own configuration section in `appsettings.Plugins.json`. 

### Standard Pattern
1. **Define Section**: Overwrite `GetConfigurationSection()` in your `IErpModule` implementation.
   ```csharp
   public string GetConfigurationSection() => "Plugins:MyModule";
   ```
2. **Add to JSON**: Add your settings to the host's `appsettings.Plugins.json`.
   ```json
   {
     "Plugins": {
       "MyModule": {
         "ApiKey": "123456",
         "RetryCount": 3
       }
     }
   }
   ```
3. **Validate**: Implement `ValidateConfiguration(IConfiguration config)` to ensure required keys are present during startup.
   ```csharp
   public void ValidateConfiguration(IConfiguration config) {
       var section = config.GetSection(GetConfigurationSection());
       if (string.IsNullOrEmpty(section["ApiKey"])) throw new Exception("ApiKey is missing.");
   }
   ```

## 2. Dependency Injection Isolation

To prevent modules from accidentally overriding each other's services:

- **Conflict Detection**: The host detects if multiple modules register the same service type.
- **Strict Mode**: By default, `AllowServiceOverride` is `false`. If a conflict is detected, the host will log a warning and **skip** the duplicate registration.
- **Explicit Override**: If you intentionally want to replace a core or other plugin's service, set `AllowServiceOverride` to `true`.

### Best Practices
- Use **Interface-based** registrations.
- Prefix your internal services to avoid name collisions (though the host handles the type level).
- Prefer `Scoped` or `Transient` unless a `Singleton` is strictly required.

## 3. Database Migrations (Preview)

Plugins can (from Sprint 4.1) participate in a coordinated migration flow.
- Each plugin should have its own `DbContext`.
- The `MigrationCoordinator` handles execution order based on dependencies.
