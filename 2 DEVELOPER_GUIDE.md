Absolutely — below is your **complete Developer Guide** for the ERP-CMS platform, written as the **primary source of truth** for all developers contributing to the system.

This document is structured and written for **senior backend and full-stack .NET developers** and includes all critical specs: architecture, patterns, folder structure, naming conventions, and plugin strategy.

It's fully **Markdown compatible** — ready to paste into your repository’s `README.md` or `DEVELOPER_GUIDE.md`.

---

# 🧭 ERP-CMS Modular Monolith – Developer Guide

> **System Architecture, Coding Standards, and Implementation Patterns**

---

## 📌 Overview

This ERP-CMS platform is a **modular monolith** with plugin-based extensibility for:

* ERP modules (Accounting, Inventory, HR, etc.)
* UI component plugins (Textbox, Button, FormBuilder)
* UI rendering plugins
* A memory-first, low-code runtime engine

The system is designed for:

* Runtime performance
* Low coupling, high cohesion
* Extensible modularity using **MEF**
* Config-driven, drag-and-drop UI

---

## 🚧 Core Architecture Overview

### Key Concepts

| Concept               | Description                                              |
| --------------------- | -------------------------------------------------------- |
| **Modular Monolith**  | Core app loads all modules/plugins into a single process |
| **MEF Plugin Loader** | All ERP modules and UI components are plugins            |
| **UI Builder IDE**    | Blazor-based drag/drop designer                          |
| **UI Runtime Cache**  | Singleton in-memory service to render UI instantly       |
| **State Versioning**  | Entities support snapshot + rollback via JSON            |
| **Dynamic UI**        | Stored in DB, rendered via plugin-based components       |

---

## 🏗️ System Architecture Diagram

```
┌────────────────────────────┐
│       ASP.NET Core Host    │
│        (Startup.cs)        │
└────────────┬───────────────┘
             ▼
┌────────────────────────────┐
│       Plugin Loader (MEF)  │
└────────────┬───────────────┘
      ┌──────┴──────┐
      ▼             ▼
ERP Modules    UI Component Plugins
(Accounting)   (Textbox, Button...)

┌────────────────────────────┐
│       UI Cache Service     │
│   (HTML + Config preload)  │
└────────────┬───────────────┘
             ▼
       UI Renderer Engine
```

---

## 📁 Folder & Project Structure

```
/MyErpApp
│
├── src/
│   ├── MyErpApp.Host                # ASP.NET Core API Host
│   ├── MyErpApp.Core                # Shared contracts/interfaces
│   ├── MyErpApp.Infrastructure      # EFCore setup, caching, DB context
│   ├── MyErpApp.UiDesigner          # Blazor drag-and-drop IDE
│   ├── MyErpApp.UiRenderer          # Runtime UI rendering engine
│   ├── MyErpApp.UiRuntimeCache      # In-memory HTML + config cache
│
│   ├── Plugins/                     # Plugins only reference Core
│   │   ├── Accounting.Plugin
│   │   ├── Inventory.Plugin
│   │   ├── Ui.TextBox.Plugin
│   │   └── Ui.Renderer.Html.Plugin
│
└── plugins/                         # Deployed plugin DLLs (loaded by MEF)
```

---

## 📦 Plugin Contracts

### ERP Modules

```csharp
public interface IErpModule
{
    string Name { get; }
    void RegisterServices(IServiceCollection services);
    void MapEndpoints(IEndpointRouteBuilder app);
}
```

---

### UI Component Plugins

```csharp
public interface IUiComponentPlugin
{
    string Type { get; }               // "textbox", "button"
    string DisplayName { get; }        // "Text Box"
    string DefaultConfig();            // Initial config JSON
    string RenderHtml(string config);  // Output Tailwind HTML
}
```

---

### UI Renderer Plugins (Optional)

```csharp
public interface IUiRenderExtension
{
    string Name { get; }
    string RenderPage(string html, string configJson);
}
```

---

## 🧰 Plugin Loader (MEF)

```csharp
public static class PluginLoader
{
    public static CompositionHost LoadPlugins(string path)
    {
        var assemblies = Directory.GetFiles(path, "*.dll")
            .Select(Assembly.LoadFrom);

        var config = new ContainerConfiguration()
            .WithAssemblies(assemblies);

        return config.CreateContainer();
    }
}
```

---

## ⚙️ Startup Configuration

### `Program.cs` (partial)

```csharp
var pluginHost = PluginLoader.LoadPlugins("plugins");

var modules = pluginHost.GetExports<IErpModule>();
foreach (var module in modules)
{
    module.RegisterServices(builder.Services);
}

// After app starts
foreach (var module in modules)
{
    module.MapEndpoints(app);
}
```

---

## 📖 Coding Patterns & Practices

### ✅ Domain-Driven Patterns

| Pattern                    | Rule                                                 |
| -------------------------- | ---------------------------------------------------- |
| **Modules**                | Should contain their own domain, services, endpoints |
| **Separation of Concerns** | API, Domain, Infrastructure are separate per module  |
| **Application Layer**      | Handles orchestration, not business logic            |
| **Entities**               | Only contain logic/data related to themselves        |
| **Snapshot Versioning**    | Keep history via `EntitySnapshot` table              |

---

### ✅ UI Components as Plugins

Each UI component must:

* Implement `IUiComponentPlugin`
* Return valid Tailwind HTML string from `RenderHtml`
* Store configuration as JSON (in DB)
* Be discoverable via MEF

---

## 📐 Naming Conventions

| Item             | Convention                                    |
| ---------------- | --------------------------------------------- |
| Namespace        | `MyErpApp.[Module].[Layer]`                   |
| Plugin Class     | `*Plugin` suffix                              |
| Component Plugin | Prefix with `Ui.`                             |
| HTML classes     | Tailwind‑based only                           |
| UI Page Name     | PascalCase (`CustomerForm`, `InvoiceBuilder`) |
| Config keys      | camelCase JSON (`placeholder`, `label`)       |

---

## 📂 Example: Textbox Component Plugin

```csharp
[Export(typeof(IUiComponentPlugin))]
public class TextBoxPlugin : IUiComponentPlugin
{
    public string Type => "textbox";
    public string DisplayName => "Text Box";

    public string DefaultConfig() =>
        JsonSerializer.Serialize(new { placeholder = "Enter text..." });

    public string RenderHtml(string configJson)
    {
        var cfg = JsonSerializer.Deserialize<Dictionary<string, string>>(configJson);
        return $"<input class='border p-2 w-full' placeholder='{cfg["placeholder"]}' />";
    }
}
```

---

## UI Component Library

The system provides a set of core UI components via the `CommonUi.Plugin`. These components use Tailwind CSS for styling.

### Available Components

| Component | Type | Display Name | Config Keys | Description |
|-----------|------|--------------|-------------|-------------|
| **Heading** | `Heading` | Heading | `Text`, `Level` (1-6) | Standard HTML heading |
| **Input** | `Input` | Input Box | `Label`, `Placeholder`, `Type` | Text input with label |
| **Select** | `Select` | Dropdown List | `Label`, `Items` (v1:l1,v2:l2) | Standard dropdown select |
| **Checkbox** | `Checkbox` | Checkbox | `Label`, `Checked` (true/false) | Checkbox with label |
| **Button** | `Button` | Button | `Text`, `Class` (Tailwind) | Action button |

### Usage Example (C#)
```csharp
page.Components.Add(new UiComponent 
{ 
    Type = "Input", 
    Order = 1, 
    ConfigJson = "{\"Label\": \"Email\", \"Placeholder\": \"user@example.com\"}" 
});
```

### Developing New Components
1. Create a class implementing `IUiComponentPlugin`.
2. Add `[Export(typeof(IUiComponentPlugin))]` attribute.
3. Implement `RenderHtml(string config)` returning Tailwind-compatible HTML.
4. Build and place the DLL in the `plugins/` folder.

---

## 🧠 Memory-First UI Runtime

All UI pages are:

* Stored in database as:

  * `TailwindHtml`
  * `ConfigJson`
* Loaded into memory cache at app startup
* Served instantly without DB hits

### Render Cache Service

```csharp
public class UiRenderCacheService : IUiRenderCacheService
{
    private Dictionary<string, string> _htmlCache = new();
    private Dictionary<string, string> _configCache = new();

    public void Refresh(IEnumerable<UiPage> pages)
    {
        foreach (var page in pages)
        {
            _htmlCache[page.Name] =
                string.Join("\n", page.Components.Select(c => c.TailwindHtml));

            _configCache[page.Name] =
                JsonSerializer.Serialize(page.Components);
        }
    }

    public string GetHtml(string pageName) => _htmlCache.TryGetValue(pageName, out var html) ? html : null;
    public string GetConfig(string pageName) => _configCache.TryGetValue(pageName, out var cfg) ? cfg : null;
}
```

---

## 🔁 State Versioning

Use `EntitySnapshot` to track entity changes and allow rollback.

```csharp
public class EntitySnapshot
{
    public Guid Id { get; set; }
    public string EntityName { get; set; }
    public Guid EntityId { get; set; }
    public string JsonData { get; set; }
    public DateTime SnapshotDate { get; set; }
}
```

---

## 🛠 UI Builder Responsibilities

| Feature | Description                                |
| ------- | ------------------------------------------ |
| Toolbox | Pulls components via MEF                   |
| Canvas  | Drag-and-drop editor                       |
| Sidebar | JSON config editor                         |
| Save    | Stores `TailwindHtml` + `ConfigJson` to DB |
| Preview | Renders live HTML from plugins             |

---

## 🚀 Developer Workflow

### Adding an ERP Module

1. Create class library `Accounting.Plugin`
2. Reference `MyErpApp.Core`
3. Implement `IErpModule`
4. Compile and drop DLL in `/plugins`
5. Restart app

---

### Adding a UI Component

1. Create `Ui.X.Plugin` project
2. Implement `IUiComponentPlugin`
3. Compile and drop into `/plugins`
4. Use in UI Builder instantly

---

### Updating Runtime Rendering

1. Implement `IUiRenderExtension`
2. Register via MEF
3. Process HTML/config during runtime

---

## 🔐 Security Notes

* All plugins run **in-process** — validate code before deployment.
* Plugin discovery is **not isolated** — consider **AppDomain** separation if required.
* Consider future plugin **versioning and sandboxing** for multi-tenant safety.

---

## ✅ Summary

| Feature                   | Covered |
| ------------------------- | ------- |
| Modular ERP               | ✅       |
| Plugin Loader (MEF)       | ✅       |
| Dynamic UI (HTML + JSON)  | ✅       |
| Preload to memory         | ✅       |
| Low-code UI Builder       | ✅       |
| State Versioning          | ✅       |
| Component-based UI system | ✅       |

---

## 🔚 This Is Your Developer Source of Truth

This guide contains everything required to:

* Understand the system design
* Extend the platform
* Contribute new modules
* Follow conventions

---

## 📎 Need More?

To extend this guide, you could add:

* CI/CD deployment pipeline
* Plugin version compatibility
* Admin portal for plugin registration
* Runtime assembly hot reload via Roslyn

---