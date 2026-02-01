# Build script for local CI
Write-Host "Starting build and test..." -ForegroundColor Cyan

dotnet build MyErpApp.slnx

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    
    # Deployment of plugins
    $pluginTarget = "plugins"
    if (-not (Test-Path $pluginTarget)) { New-Item -ItemType Directory -Path $pluginTarget }
    
    Write-Host "Deploying plugins..." -ForegroundColor Cyan
    Get-ChildItem -Path "src/Plugins/**/*.dll" -Recurse | Where-Object { $_.FullName -notmatch "obj" } | Copy-Item -Destination $pluginTarget -Force
    
    Write-Host "Plugins deployed to $pluginTarget" -ForegroundColor Green
}
else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Add test execution here when tests are added
# dotnet test MyErpApp.slnx
