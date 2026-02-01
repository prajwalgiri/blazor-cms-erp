# Build script for local CI
Write-Host "Starting build and test..." -ForegroundColor Cyan

dotnet build MyErpApp.slnx

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Add test execution here when tests are added
# dotnet test MyErpApp.slnx
