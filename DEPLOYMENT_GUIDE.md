# 🚀 ERP-CMS Platform – Deployment Guide

**File:** `DEPLOYMENT_GUIDE.md`  
**Owner:** DevOps Lead / Platform Engineer  
**Audience:** DevOps Engineers, SREs, Platform Engineers

---

## 1. Overview

This guide covers **deployment strategies, CI/CD pipelines, and operational procedures** for the ERP-CMS platform including:

- Environment setup
- CI/CD pipelines
- Blue-green deployment
- Plugin deployment
- Rollback procedures
- Monitoring & alerting

---

## 2. Environment Architecture

### 2.1 Environment Tiers

```
Development → Staging → UAT → Production
```

| Environment | Purpose | Data | Uptime SLA |
|-------------|---------|------|------------|
| **Development** | Active development | Synthetic | None |
| **Staging** | Integration testing | Sanitized production copy | 95% |
| **UAT** | User acceptance testing | Production-like | 98% |
| **Production** | Live system | Real data | 99.9% |

### 2.2 Infrastructure Components

```
┌────────────────────────────────────┐
│         Load Balancer (ALB)        │
└────────────┬───────────────────────┘
             │
    ┌────────┴────────┐
    ▼                 ▼
┌─────────┐      ┌─────────┐
│  App 1  │      │  App 2  │  ← ASP.NET Core instances
└────┬────┘      └────┬────┘
     │                │
     └────────┬───────┘
              ▼
     ┌─────────────────┐
     │   Redis Cache   │
     └─────────────────┘
              │
              ▼
     ┌─────────────────┐
     │   SQL Database  │
     │   (Primary +    │
     │    Read Replica)│
     └─────────────────┘
              │
              ▼
     ┌─────────────────┐
     │  Blob Storage   │  ← Plugin DLLs, files
     └─────────────────┘
```

---

## 3. Pre-Deployment Setup

### 3.1 Azure Resources (Example)

```bash
# Resource Group
az group create --name myerp-rg --location eastus

# App Service Plan
az appservice plan create \
  --name myerp-plan \
  --resource-group myerp-rg \
  --sku P1V2 \
  --is-linux

# App Service
az webapp create \
  --name myerp-app \
  --resource-group myerp-rg \
  --plan myerp-plan \
  --runtime "DOTNET|8.0"

# SQL Database
az sql server create \
  --name myerp-sql \
  --resource-group myerp-rg \
  --admin-user sqladmin \
  --admin-password [SECURE_PASSWORD]

az sql db create \
  --name myerp-db \
  --server myerp-sql \
  --resource-group myerp-rg \
  --service-objective S1

# Redis Cache
az redis create \
  --name myerp-redis \
  --resource-group myerp-rg \
  --location eastus \
  --sku Basic \
  --vm-size c0

# Storage Account (for plugins)
az storage account create \
  --name myerpstorage \
  --resource-group myerp-rg \
  --location eastus \
  --sku Standard_LRS

az storage container create \
  --name plugins \
  --account-name myerpstorage
```

### 3.2 Configuration Management

Use **Azure Key Vault** for secrets:

```bash
# Create Key Vault
az keyvault create \
  --name myerp-vault \
  --resource-group myerp-rg \
  --location eastus

# Add secrets
az keyvault secret set \
  --vault-name myerp-vault \
  --name "ConnectionStrings--Database" \
  --value "[CONNECTION_STRING]"

az keyvault secret set \
  --vault-name myerp-vault \
  --name "Jwt--SecretKey" \
  --value "[SECURE_JWT_KEY]"
```

---

## 4. CI/CD Pipeline

### 4.1 GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Deploy

on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_WEBAPP_NAME: myerp-app
  DOTNET_VERSION: '8.0.x'

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish src/MyErpApp.Host/MyErpApp.Host.csproj \
        --configuration Release \
        --output ./publish
    
    - name: Upload artifact
      uses: actions/upload-artifact@v3
      with:
        name: webapp
        path: ./publish

  deploy-staging:
    needs: build
    runs-on: ubuntu-latest
    environment: staging
    
    steps:
    - name: Download artifact
      uses: actions/download-artifact@v3
      with:
        name: webapp
        path: ./publish
    
    - name: Deploy to Azure Web App (Staging)
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        slot-name: staging
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE_STAGING }}
        package: ./publish
    
    - name: Run smoke tests
      run: |
        curl -f https://${{ env.AZURE_WEBAPP_NAME }}-staging.azurewebsites.net/health || exit 1

  deploy-production:
    needs: deploy-staging
    runs-on: ubuntu-latest
    environment: production
    
    steps:
    - name: Download artifact
      uses: actions/download-artifact@v3
      with:
        name: webapp
        path: ./publish
    
    - name: Deploy to Azure Web App (Production)
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
    
    - name: Warm up cache
      run: |
        curl -X POST https://${{ env.AZURE_WEBAPP_NAME }}.azurewebsites.net/admin/cache/warmup \
          -H "Authorization: Bearer ${{ secrets.ADMIN_TOKEN }}"
    
    - name: Health check
      run: |
        for i in {1..5}; do
          curl -f https://${{ env.AZURE_WEBAPP_NAME }}.azurewebsites.net/health && break
          sleep 10
        done
```

### 4.2 Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
    - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  dotnetVersion: '8.0.x'

stages:
- stage: Build
  jobs:
  - job: BuildApp
    steps:
    - task: UseDotNet@2
      inputs:
        version: $(dotnetVersion)
    
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
    
    - task: DotNetCoreCLI@2
      displayName: 'Build application'
      inputs:
        command: 'build'
        arguments: '--configuration $(buildConfiguration)'
    
    - task: DotNetCoreCLI@2
      displayName: 'Run tests'
      inputs:
        command: 'test'
        arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage"'
    
    - task: DotNetCoreCLI@2
      displayName: 'Publish application'
      inputs:
        command: 'publish'
        publishWebProjects: true
        arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'
    
    - task: PublishBuildArtifacts@1
      inputs:
        pathToPublish: '$(Build.ArtifactStagingDirectory)'
        artifactName: 'drop'

- stage: DeployStaging
  dependsOn: Build
  jobs:
  - deployment: DeployToStaging
    environment: 'staging'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'Azure-Subscription'
              appName: 'myerp-app'
              deployToSlotOrASE: true
              slotName: 'staging'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'

- stage: DeployProduction
  dependsOn: DeployStaging
  jobs:
  - deployment: DeployToProduction
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'Azure-Subscription'
              appName: 'myerp-app'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

---

## 5. Blue-Green Deployment

### 5.1 Deployment Strategy

```
┌──────────────────────────────────────┐
│          Load Balancer               │
└────────────┬─────────────────────────┘
             │
    ┌────────┴──────────┐
    │                   │
┌───▼────┐         ┌────────┐
│ Blue   │         │ Green  │
│ (Live) │         │ (New)  │
└────────┘         └────────┘
    ↓                   ↓
  100%                 0%

After validation, switch:
    ↓                   ↓
   0%                 100%
```

### 5.2 Implementation

```bash
# Deploy to Green slot (staging)
az webapp deployment slot create \
  --name myerp-app \
  --resource-group myerp-rg \
  --slot green

# Deploy new version to green
az webapp deployment source config-zip \
  --name myerp-app \
  --resource-group myerp-rg \
  --slot green \
  --src ./publish.zip

# Test green slot
curl https://myerp-app-green.azurewebsites.net/health

# Swap slots (go live)
az webapp deployment slot swap \
  --name myerp-app \
  --resource-group myerp-rg \
  --slot green \
  --target-slot production

# If issues, swap back
az webapp deployment slot swap \
  --name myerp-app \
  --resource-group myerp-rg \
  --slot production \
  --target-slot green
```

---

## 6. Plugin Deployment

### 6.1 Plugin Deployment Process

```
1. Build Plugin    → 2. Sign DLL      → 3. Generate Checksum
         ↓                                        ↓
4. Upload to Blob Storage ← ────────────────────
         ↓
5. Update Plugin Catalog
         ↓
6. Trigger Plugin Refresh (Hot Reload or App Restart)
         ↓
7. Validate Plugin Load
         ↓
8. Monitor & Rollback if needed
```

### 6.2 Plugin Upload Script

```bash
#!/bin/bash
# deploy-plugin.sh

PLUGIN_NAME=$1
PLUGIN_VERSION=$2
DLL_PATH=$3

echo "Deploying plugin: $PLUGIN_NAME v$PLUGIN_VERSION"

# 1. Generate checksum
CHECKSUM=$(sha256sum "$DLL_PATH" | awk '{print $1}')
echo "Checksum: $CHECKSUM"

# 2. Upload to blob storage
az storage blob upload \
  --account-name myerpstorage \
  --container-name plugins \
  --name "$PLUGIN_NAME/$PLUGIN_VERSION/$PLUGIN_NAME.dll" \
  --file "$DLL_PATH" \
  --overwrite

# 3. Update plugin catalog
cat > plugin-metadata.json <<EOF
{
  "name": "$PLUGIN_NAME",
  "version": "$PLUGIN_VERSION",
  "checksum": "$CHECKSUM",
  "uploadedAt": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF

az storage blob upload \
  --account-name myerpstorage \
  --container-name plugins \
  --name "$PLUGIN_NAME/$PLUGIN_VERSION/metadata.json" \
  --file plugin-metadata.json \
  --overwrite

# 4. Trigger plugin refresh
curl -X POST https://myerp-app.azurewebsites.net/admin/plugins/refresh \
  -H "Authorization: Bearer $ADMIN_TOKEN"

echo "Plugin deployed successfully"
```

### 6.3 Hot Reload vs Restart

| Method | Downtime | Risk | Use Case |
|--------|----------|------|----------|
| **Hot Reload** | None | Medium | Non-breaking plugin updates |
| **App Restart** | ~30s | Low | Breaking changes, core updates |

```csharp
// Hot Reload Implementation
public class PluginHotReloader
{
    public async Task<bool> ReloadPluginAsync(string pluginName)
    {
        try
        {
            // 1. Download new version
            var newDll = await DownloadPluginAsync(pluginName);
            
            // 2. Unload old plugin
            await UnloadPluginAsync(pluginName);
            
            // 3. Load new plugin
            await LoadPluginAsync(newDll);
            
            // 4. Verify health
            return await VerifyPluginHealthAsync(pluginName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hot reload failed for {Plugin}", pluginName);
            
            // Rollback
            await RollbackPluginAsync(pluginName);
            return false;
        }
    }
}
```

---

## 7. Database Migration

### 7.1 Migration Strategy

**Zero-Downtime Migrations:**

1. **Backward-compatible changes first** (add columns, tables)
2. **Deploy code** that works with old & new schema
3. **Data migration** (background job)
4. **Deploy code** using new schema
5. **Drop old schema** (after validation)

### 7.2 Migration Script

```bash
#!/bin/bash
# migrate-database.sh

ENVIRONMENT=$1

echo "Running migrations for $ENVIRONMENT environment"

# 1. Backup database
az sql db export \
  --name myerp-db \
  --server myerp-sql \
  --resource-group myerp-rg \
  --admin-user sqladmin \
  --admin-password "$SQL_PASSWORD" \
  --storage-key-type StorageAccessKey \
  --storage-key "$STORAGE_KEY" \
  --storage-uri "https://myerpstorage.blob.core.windows.net/backups/backup-$(date +%Y%m%d-%H%M%S).bacpac"

# 2. Run core migrations
dotnet ef database update --project src/MyErpApp.Infrastructure --context AppDbContext

# 3. Run plugin migrations
for PLUGIN in plugins/*; do
  if [ -d "$PLUGIN" ]; then
    PLUGIN_NAME=$(basename "$PLUGIN")
    echo "Migrating plugin: $PLUGIN_NAME"
    
    dotnet ef database update \
      --project "plugins/$PLUGIN_NAME/$PLUGIN_NAME.csproj" \
      --context "${PLUGIN_NAME}DbContext" || echo "No migrations for $PLUGIN_NAME"
  fi
done

echo "Migrations completed"
```

---

## 8. Rollback Procedures

### 8.1 Application Rollback

```bash
#!/bin/bash
# rollback.sh

# Option 1: Swap deployment slots back
az webapp deployment slot swap \
  --name myerp-app \
  --resource-group myerp-rg \
  --slot production \
  --target-slot green

# Option 2: Redeploy previous version
PREVIOUS_VERSION=$(az webapp deployment list-publishing-profiles \
  --name myerp-app \
  --resource-group myerp-rg \
  --query "[0].publishUrl" -o tsv)

az webapp deployment source config-zip \
  --name myerp-app \
  --resource-group myerp-rg \
  --src ./previous-version.zip
```

### 8.2 Database Rollback

```bash
#!/bin/bash
# rollback-database.sh

BACKUP_FILE=$1

echo "Rolling back database from: $BACKUP_FILE"

# 1. Stop application (prevent writes)
az webapp stop --name myerp-app --resource-group myerp-rg

# 2. Restore database
az sql db import \
  --name myerp-db \
  --server myerp-sql \
  --resource-group myerp-rg \
  --admin-user sqladmin \
  --admin-password "$SQL_PASSWORD" \
  --storage-key-type StorageAccessKey \
  --storage-key "$STORAGE_KEY" \
  --storage-uri "$BACKUP_FILE"

# 3. Restart application
az webapp start --name myerp-app --resource-group myerp-rg

echo "Rollback completed"
```

### 8.3 Plugin Rollback

```bash
#!/bin/bash
# rollback-plugin.sh

PLUGIN_NAME=$1
PREVIOUS_VERSION=$2

echo "Rolling back plugin: $PLUGIN_NAME to version $PREVIOUS_VERSION"

# Download previous version
az storage blob download \
  --account-name myerpstorage \
  --container-name plugins \
  --name "$PLUGIN_NAME/$PREVIOUS_VERSION/$PLUGIN_NAME.dll" \
  --file "/tmp/$PLUGIN_NAME.dll"

# Copy to plugins directory (requires app restart)
cp "/tmp/$PLUGIN_NAME.dll" "/var/app/plugins/$PLUGIN_NAME.dll"

# Trigger hot reload
curl -X POST https://myerp-app.azurewebsites.net/admin/plugins/reload \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{ \"pluginName\": \"$PLUGIN_NAME\" }"

echo "Plugin rollback completed"
```

---

## 9. Health Checks & Readiness

### 9.1 Health Check Endpoints

```csharp
public static class HealthCheckConfiguration
{
    public static void ConfigureHealthChecks(this IServiceCollection services, IConfiguration config)
    {
        services.AddHealthChecks()
            // Database
            .AddSqlServer(
                config.GetConnectionString("Database"),
                name: "database",
                tags: new[] { "db", "sql" })
            
            // Redis cache
            .AddRedis(
                config.GetConnectionString("Redis"),
                name: "redis",
                tags: new[] { "cache" })
            
            // Blob storage
            .AddAzureBlobStorage(
                config.GetConnectionString("BlobStorage"),
                name: "blob-storage",
                tags: new[] { "storage" })
            
            // Custom plugin health checks
            .AddCheck<PluginHealthCheck>("plugins", tags: new[] { "plugins" });
    }
}

public class PluginHealthCheck : IHealthCheck
{
    private readonly IPluginManager _pluginManager;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var plugins = await _pluginManager.GetAllAsync();
        var failedPlugins = plugins.Where(p => p.Status == PluginStatus.Failed).ToList();
        
        if (failedPlugins.Any())
        {
            return HealthCheckResult.Degraded(
                $"Failed plugins: {string.Join(", ", failedPlugins.Select(p => p.Name))}");
        }
        
        return HealthCheckResult.Healthy($"{plugins.Count()} plugins loaded");
    }
}
```

### 9.2 Kubernetes Probes (if using K8s)

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myerp-app
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: myerp
        image: myerp:latest
        ports:
        - containerPort: 80
        
        # Liveness probe - restart if unhealthy
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        
        # Readiness probe - remove from load balancer if not ready
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        
        # Startup probe - allow time for startup
        startupProbe:
          httpGet:
            path: /health/startup
            port: 80
          initialDelaySeconds: 0
          periodSeconds: 10
          timeoutSeconds: 3
          failureThreshold: 30
```

---

## 10. Monitoring & Alerting

### 10.1 Application Insights

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
});

// Track custom metrics
public class MetricsCollector
{
    private readonly TelemetryClient _telemetry;
    
    public void TrackPluginLoad(string pluginName, TimeSpan duration, bool success)
    {
        _telemetry.TrackMetric("PluginLoadTime", duration.TotalMilliseconds, 
            new Dictionary<string, string>
            {
                { "PluginName", pluginName },
                { "Success", success.ToString() }
            });
    }
    
    public void TrackCacheHit(string pageName, bool hit)
    {
        _telemetry.TrackMetric("CacheHitRate", hit ? 1 : 0,
            new Dictionary<string, string>
            {
                { "PageName", pageName }
            });
    }
}
```

### 10.2 Alert Rules

```bash
# Create alert for failed health checks
az monitor metrics alert create \
  --name "HealthCheck-Failed" \
  --resource-group myerp-rg \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/myerp-rg/providers/Microsoft.Web/sites/myerp-app" \
  --condition "avg HealthCheckStatus < 1" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action email admin@company.com

# Alert for high error rate
az monitor metrics alert create \
  --name "HighErrorRate" \
  --resource-group myerp-rg \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/myerp-rg/providers/Microsoft.Web/sites/myerp-app" \
  --condition "total Http5xx > 10" \
  --window-size 5m \
  --evaluation-frequency 1m
```

---

## 11. Scaling Strategy

### 11.1 Auto-scaling Rules

```bash
# Scale up when CPU > 70%
az monitor autoscale create \
  --resource-group myerp-rg \
  --resource myerp-app \
  --resource-type Microsoft.Web/serverfarms \
  --name autoscale-rules \
  --min-count 2 \
  --max-count 10 \
  --count 2

az monitor autoscale rule create \
  --resource-group myerp-rg \
  --autoscale-name autoscale-rules \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 2

# Scale down when CPU < 30%
az monitor autoscale rule create \
  --resource-group myerp-rg \
  --autoscale-name autoscale-rules \
  --condition "Percentage CPU < 30 avg 10m" \
  --scale in 1
```

---

## 12. Disaster Recovery

### 12.1 Backup Strategy

| Component | Frequency | Retention | Storage |
|-----------|-----------|-----------|---------|
| **Database** | Daily | 30 days | Geo-redundant |
| **Plugins** | On change | 90 days | Blob storage |
| **Configuration** | On change | Version control | Git |
| **Logs** | Real-time | 90 days | Log Analytics |

### 12.2 DR Procedure

```bash
#!/bin/bash
# disaster-recovery.sh

# 1. Provision new infrastructure in DR region
az group create --name myerp-dr-rg --location westus

# 2. Restore database from geo-backup
az sql db restore \
  --resource-group myerp-dr-rg \
  --server myerp-dr-sql \
  --name myerp-db \
  --time "2024-01-15T12:00:00Z"

# 3. Deploy application
az webapp create \
  --name myerp-dr-app \
  --resource-group myerp-dr-rg \
  --plan myerp-dr-plan

# 4. Update DNS to point to DR site
az network traffic-manager endpoint update \
  --name primary \
  --type azureEndpoints \
  --profile-name myerp-tm \
  --resource-group myerp-rg \
  --target-resource-id "/subscriptions/{sub-id}/resourceGroups/myerp-dr-rg/providers/Microsoft.Web/sites/myerp-dr-app"

echo "DR failover completed"
```

---

## 13. Deployment Checklist

### Pre-Deployment

- [ ] All tests passing
- [ ] Code review completed
- [ ] Security scan passed
- [ ] Database backup completed
- [ ] Deployment approved
- [ ] Stakeholders notified
- [ ] Maintenance window scheduled (if downtime expected)

### During Deployment

- [ ] Deploy to staging first
- [ ] Run smoke tests
- [ ] Verify health checks
- [ ] Monitor error logs
- [ ] Check performance metrics
- [ ] Validate database migrations

### Post-Deployment

- [ ] Verify production health checks
- [ ] Run smoke tests in production
- [ ] Monitor for 30 minutes
- [ ] Warm up cache
- [ ] Notify stakeholders of completion
- [ ] Update runbook/documentation

### Rollback Decision Criteria

Rollback immediately if:
- Health checks fail for > 2 minutes
- Error rate > 5%
- Critical functionality broken
- Database corruption detected
- Security vulnerability discovered

---

_End of `DEPLOYMENT_GUIDE.md`_
