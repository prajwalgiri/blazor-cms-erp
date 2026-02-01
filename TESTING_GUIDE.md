# 🧪 ERP-CMS Platform – Testing Guide

**File:** `TESTING_GUIDE.md`  
**Owner:** QA Lead / Tech Lead  
**Audience:** Developers, QA Engineers

---

## 1. Overview

This guide defines the **testing strategy and best practices** for the ERP-CMS platform, covering:

- Unit testing
- Integration testing
- Plugin testing
- UI Designer testing
- Performance testing
- Security testing

---

## 2. Testing Pyramid

```
        ┌─────────────┐
        │   E2E (5%)  │  ← Full system tests
        ├─────────────┤
        │ Integration │  ← API, DB, Plugin integration
        │    (25%)    │
        ├─────────────┤
        │   Unit      │  ← Business logic, services
        │   (70%)     │
        └─────────────┘
```

**Target Coverage:**
- Unit: 70%+ code coverage
- Integration: All critical paths
- E2E: Key user workflows

---

## 3. Test Project Structure

```
/MyErpApp.Tests
├── Unit/
│   ├── Core/
│   ├── Services/
│   └── Plugins/
├── Integration/
│   ├── Api/
│   ├── Database/
│   └── Plugins/
├── E2E/
│   ├── UiDesigner/
│   └── Runtime/
├── Performance/
└── Security/
```

---

## 4. Unit Testing

### 4.1 Framework & Tools

- **xUnit** - Test framework
- **Moq** - Mocking library
- **FluentAssertions** - Assertion library
- **AutoFixture** - Test data generation

### 4.2 Unit Test Template

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using AutoFixture;

namespace MyErpApp.Tests.Unit.Services
{
    public class SnapshotServiceTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IDbContext> _dbContextMock;
        private readonly SnapshotService _sut; // System Under Test
        
        public SnapshotServiceTests()
        {
            _fixture = new Fixture();
            _dbContextMock = new Mock<IDbContext>();
            _sut = new SnapshotService(_dbContextMock.Object);
        }
        
        [Fact]
        public async Task CreateSnapshot_ValidEntity_CreatesSnapshot()
        {
            // Arrange
            var entity = _fixture.Create<LedgerEntry>();
            
            // Act
            var result = await _sut.CreateSnapshotAsync(entity);
            
            // Assert
            result.Should().NotBeNull();
            result.EntityName.Should().Be("LedgerEntry");
            result.JsonData.Should().NotBeNullOrEmpty();
            _dbContextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }
        
        [Fact]
        public async Task CreateSnapshot_NullEntity_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _sut.CreateSnapshotAsync<LedgerEntry>(null));
        }
        
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task RestoreSnapshot_InvalidId_ThrowsException(string id)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.RestoreSnapshotAsync<LedgerEntry>(id));
        }
    }
}
```

### 4.3 Best Practices

✅ **DO:**
- Test one thing per test
- Use descriptive test names (MethodName_Scenario_ExpectedBehavior)
- Follow Arrange-Act-Assert pattern
- Use AutoFixture for test data
- Mock external dependencies
- Test edge cases and error paths

❌ **DON'T:**
- Test framework code
- Test private methods directly
- Use hard-coded test data
- Have tests depend on each other
- Test implementation details

---

## 5. Integration Testing

### 5.1 Test Database Setup

Use **in-memory SQLite** for fast tests:

```csharp
public class IntegrationTestBase : IDisposable
{
    protected readonly AppDbContext Context;
    protected readonly IServiceProvider ServiceProvider;
    
    public IntegrationTestBase()
    {
        var services = new ServiceCollection();
        
        // Use in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite("DataSource=:memory:"));
        
        // Register services
        services.AddScoped<ISnapshotService, SnapshotService>();
        
        ServiceProvider = services.BuildServiceProvider();
        Context = ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Create schema
        Context.Database.OpenConnection();
        Context.Database.EnsureCreated();
    }
    
    public void Dispose()
    {
        Context.Database.CloseConnection();
        Context.Dispose();
    }
}
```

### 5.2 Integration Test Example

```csharp
public class UiPageRepositoryTests : IntegrationTestBase
{
    private readonly UiPageRepository _repository;
    
    public UiPageRepositoryTests()
    {
        _repository = new UiPageRepository(Context);
    }
    
    [Fact]
    public async Task SavePage_WithComponents_PersistsCorrectly()
    {
        // Arrange
        var page = new UiPage
        {
            Name = "TestPage",
            Components = new List<UiComponent>
            {
                new UiComponent
                {
                    Type = "textbox",
                    TailwindHtml = "<input class='p-2' />",
                    ConfigJson = "{ \"placeholder\": \"Test\" }"
                }
            }
        };
        
        // Act
        await _repository.SaveAsync(page);
        var retrieved = await _repository.GetByNameAsync("TestPage");
        
        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Components.Should().HaveCount(1);
        retrieved.Components.First().Type.Should().Be("textbox");
    }
}
```

---

## 6. Plugin Testing

### 6.1 Plugin Test Harness

```csharp
public class PluginTestHarness
{
    /// <summary>
    /// Creates a test host with plugin loaded
    /// </summary>
    public static IServiceProvider CreateTestHost(IErpModule module)
    {
        var services = new ServiceCollection();
        
        // Add core services
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        
        // Register plugin services
        module.RegisterServices(services);
        
        return services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Creates HTTP test client with plugin endpoints
    /// </summary>
    public static HttpClient CreateTestClient(IErpModule module)
    {
        var builder = WebApplication.CreateBuilder();
        
        module.RegisterServices(builder.Services);
        
        var app = builder.Build();
        module.MapEndpoints(app);
        
        return new HttpClient
        {
            BaseAddress = new Uri("http://localhost")
        };
    }
    
    /// <summary>
    /// Creates test database with schema
    /// </summary>
    public static AppDbContext CreateTestDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        
        return context;
    }
}
```

### 6.2 Plugin Unit Test

```csharp
public class AccountingModuleTests
{
    [Fact]
    public void RegisterServices_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var module = new AccountingModule();
        
        // Act
        module.RegisterServices(services);
        var provider = services.BuildServiceProvider();
        
        // Assert
        var ledgerService = provider.GetService<ILedgerService>();
        ledgerService.Should().NotBeNull();
    }
    
    [Fact]
    public async Task MapEndpoints_RegistersLedgerEndpoints()
    {
        // Arrange
        var client = PluginTestHarness.CreateTestClient(new AccountingModule());
        
        // Act
        var response = await client.GetAsync("/accounting/ledger");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### 6.3 UI Component Plugin Test

```csharp
public class TextBoxComponentTests
{
    private readonly TextBoxComponent _component;
    
    public TextBoxComponentTests()
    {
        _component = new TextBoxComponent();
    }
    
    [Fact]
    public void DefaultConfig_ReturnsValidJson()
    {
        // Act
        var config = _component.DefaultConfig();
        
        // Assert
        config.Should().NotBeNullOrEmpty();
        var parsed = JsonDocument.Parse(config);
        parsed.RootElement.TryGetProperty("placeholder", out _).Should().BeTrue();
    }
    
    [Theory]
    [InlineData("{ \"placeholder\": \"Enter text\" }", "Enter text")]
    [InlineData("{ \"placeholder\": \"Email\" }", "Email")]
    public void RenderHtml_WithConfig_GeneratesCorrectHtml(string config, string expectedPlaceholder)
    {
        // Act
        var html = _component.RenderHtml(config);
        
        // Assert
        html.Should().Contain($"placeholder='{expectedPlaceholder}'");
        html.Should().Contain("class='");
    }
    
    [Fact]
    public void RenderHtml_WithInvalidJson_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<JsonException>(() => _component.RenderHtml("invalid json"));
    }
}
```

---

## 7. API Testing

### 7.1 WebApplicationFactory Setup

```csharp
public class ApiTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    
    public ApiTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove real database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                
                // Add in-memory database
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
                
                // Seed test data
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                SeedTestData(db);
            });
        });
        
        Client = Factory.CreateClient();
    }
    
    private void SeedTestData(AppDbContext db)
    {
        db.UiPages.Add(new UiPage
        {
            Name = "TestPage",
            Components = new List<UiComponent>()
        });
        db.SaveChanges();
    }
}
```

### 7.2 API Test Example

```csharp
public class UiPagesApiTests : ApiTestBase
{
    public UiPagesApiTests(WebApplicationFactory<Program> factory) : base(factory) { }
    
    [Fact]
    public async Task GetPage_ExistingPage_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/uipages/TestPage");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("TestPage");
    }
    
    [Fact]
    public async Task SavePage_ValidPage_Returns201()
    {
        // Arrange
        var page = new UiPage
        {
            Name = "NewPage",
            Components = new List<UiComponent>()
        };
        var json = JsonSerializer.Serialize(page);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await Client.PostAsync("/api/uipages/save", content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task DeletePage_UnauthorizedUser_Returns401()
    {
        // Act
        var response = await Client.DeleteAsync("/api/uipages/TestPage");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

## 8. UI Designer Testing

### 8.1 Blazor Component Testing (bUnit)

```csharp
public class ToolboxTests : TestContext
{
    [Fact]
    public void Toolbox_RendersAllComponents()
    {
        // Arrange
        var components = new List<IUiComponentPlugin>
        {
            new TextBoxComponent(),
            new ButtonComponent()
        };
        Services.AddSingleton<IEnumerable<IUiComponentPlugin>>(components);
        
        // Act
        var cut = RenderComponent<Toolbox>();
        
        // Assert
        cut.FindAll(".component-item").Count.Should().Be(2);
        cut.Markup.Should().Contain("Text Box");
        cut.Markup.Should().Contain("Button");
    }
    
    [Fact]
    public void Toolbox_ClickComponent_RaisesEvent()
    {
        // Arrange
        var components = new List<IUiComponentPlugin> { new TextBoxComponent() };
        Services.AddSingleton<IEnumerable<IUiComponentPlugin>>(components);
        
        var cut = RenderComponent<Toolbox>();
        var eventRaised = false;
        cut.Instance.OnComponentSelected += (type) => { eventRaised = true; };
        
        // Act
        cut.Find(".component-item").Click();
        
        // Assert
        eventRaised.Should().BeTrue();
    }
}
```

### 8.2 E2E Testing with Playwright

```csharp
[TestClass]
public class UiDesignerE2ETests
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    
    [TestInitialize]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });
    }
    
    [TestMethod]
    public async Task DesignPage_AddComponent_SavesSuccessfully()
    {
        // Arrange
        var page = await _browser.NewPageAsync();
        await page.GotoAsync("http://localhost:5000/designer");
        
        // Act - Drag component to canvas
        await page.DragAndDropAsync(".toolbox .textbox", ".canvas");
        
        // Configure component
        await page.FillAsync("#placeholder", "Enter name");
        
        // Save page
        await page.ClickAsync("#save-button");
        
        // Assert
        var successMessage = await page.TextContentAsync(".success-message");
        Assert.AreEqual("Page saved successfully", successMessage);
    }
    
    [TestCleanup]
    public async Task Cleanup()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}
```

---

## 9. Performance Testing

### 9.1 Load Testing with K6

```javascript
// load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
    stages: [
        { duration: '2m', target: 100 },  // Ramp up to 100 users
        { duration: '5m', target: 100 },  // Stay at 100 users
        { duration: '2m', target: 0 },    // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% of requests < 500ms
        http_req_failed: ['rate<0.01'],   // Error rate < 1%
    },
};

export default function () {
    // Test UI rendering
    let res = http.get('http://localhost:5000/ui/render/TestPage');
    check(res, {
        'status is 200': (r) => r.status === 200,
        'response time < 200ms': (r) => r.timings.duration < 200,
    });
    
    sleep(1);
}
```

Run with: `k6 run load-test.js`

### 9.2 Cache Performance Test

```csharp
[Fact]
public async Task UiRenderCache_LoadTest_MaintainsPerformance()
{
    // Arrange
    var cache = new UiRenderCacheService();
    var pages = GenerateTestPages(1000); // 1000 pages
    cache.Refresh(pages);
    
    var stopwatch = Stopwatch.StartNew();
    var tasks = new List<Task>();
    
    // Act - Simulate 100 concurrent requests
    for (int i = 0; i < 100; i++)
    {
        tasks.Add(Task.Run(() =>
        {
            for (int j = 0; j < 100; j++)
            {
                var html = cache.GetHtml($"Page{j % 1000}");
            }
        }));
    }
    
    await Task.WhenAll(tasks);
    stopwatch.Stop();
    
    // Assert - 10,000 requests should complete in < 1 second
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
}
```

---

## 10. Security Testing

### 10.1 Authentication Tests

```csharp
[Fact]
public async Task Login_InvalidCredentials_Returns401()
{
    // Arrange
    var request = new LoginRequest
    {
        Username = "admin",
        Password = "wrongpassword"
    };
    
    // Act
    var response = await Client.PostAsJsonAsync("/auth/login", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task ProtectedEndpoint_NoToken_Returns401()
{
    // Act
    var response = await Client.GetAsync("/admin/plugins");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

### 10.2 Authorization Tests

```csharp
[Fact]
public async Task PluginEndpoint_InsufficientPermissions_Returns403()
{
    // Arrange
    var token = GenerateToken(roles: new[] { "Viewer" });
    Client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    // Act
    var response = await Client.PostAsync("/accounting/ledger", null);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### 10.3 SQL Injection Test

```csharp
[Theory]
[InlineData("admin' OR '1'='1")]
[InlineData("'; DROP TABLE Users;--")]
public async Task Login_SqlInjectionAttempt_ReturnsSafeError(string username)
{
    // Arrange
    var request = new LoginRequest
    {
        Username = username,
        Password = "password"
    };
    
    // Act
    var response = await Client.PostAsJsonAsync("/auth/login", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    
    // Verify database is intact
    var users = await Context.Users.ToListAsync();
    users.Should().NotBeEmpty(); // Table still exists
}
```

---

## 11. Test Data Builders

### 11.1 Fluent Builder Pattern

```csharp
public class UiPageBuilder
{
    private string _name = "TestPage";
    private List<UiComponent> _components = new();
    
    public UiPageBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public UiPageBuilder WithComponent(UiComponent component)
    {
        _components.Add(component);
        return this;
    }
    
    public UiPageBuilder WithTextBox(string placeholder = "Text")
    {
        _components.Add(new UiComponent
        {
            Type = "textbox",
            TailwindHtml = $"<input placeholder='{placeholder}' />",
            ConfigJson = JsonSerializer.Serialize(new { placeholder })
        });
        return this;
    }
    
    public UiPage Build()
    {
        return new UiPage
        {
            Id = Guid.NewGuid(),
            Name = _name,
            Components = _components,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// Usage
var page = new UiPageBuilder()
    .WithName("LoginPage")
    .WithTextBox("Username")
    .WithTextBox("Password")
    .Build();
```

---

## 12. Continuous Integration

### 12.1 GitHub Actions Workflow

```yaml
# .github/workflows/tests.yml
name: Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Unit Tests
      run: dotnet test MyErpApp.Tests.Unit --no-build --verbosity normal
    
    - name: Integration Tests
      run: dotnet test MyErpApp.Tests.Integration --no-build --verbosity normal
    
    - name: Code Coverage
      run: |
        dotnet test --collect:"XPlat Code Coverage"
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coverage
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        files: ./coverage/Cobertura.xml
```

---

## 13. Test Naming Conventions

### 13.1 Unit Tests

Format: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `CreateSnapshot_ValidEntity_ReturnsSnapshot`
- `CreateSnapshot_NullEntity_ThrowsArgumentNullException`
- `RestoreSnapshot_InvalidId_ThrowsException`

### 13.2 Integration Tests

Format: `Feature_Scenario_ExpectedResult`

Examples:
- `SavePage_WithComponents_PersistsCorrectly`
- `LoadPlugin_ValidDll_LoadsSuccessfully`
- `CacheInvalidation_OnPageUpdate_RefreshesCache`

---

## 14. Code Coverage Requirements

| Component | Minimum Coverage |
|-----------|------------------|
| Core Services | 80% |
| Plugins | 70% |
| API Controllers | 60% |
| UI Components | 50% |
| Overall | 70% |

---

## 15. Testing Checklist

Before merging a PR:

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Code coverage meets requirements
- [ ] No new security vulnerabilities
- [ ] Performance tests pass (if applicable)
- [ ] Manual testing completed
- [ ] Test plan documented

---

_End of `TESTING_GUIDE.md`_
