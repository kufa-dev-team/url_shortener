# EF Core Migration Script for URL Shortener (PowerShell)
# This script runs Entity Framework Core migrations against the PostgreSQL database running in Docker

param(
    [switch]$Help
)

if ($Help) {
    Write-Host "EF Core Migration Script for URL Shortener" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\scripts\migrate.ps1"
    Write-Host ""
    Write-Host "Prerequisites:"
    Write-Host "  - Docker Desktop running"
    Write-Host "  - PostgreSQL container running (urlshortener-postgres)"
    Write-Host "  - Docker network 'urlshortener_network' exists"
    Write-Host ""
    Write-Host "This script will:"
    Write-Host "  1. Check Docker prerequisites"
    Write-Host "  2. Build migration Docker image"
    Write-Host "  3. Run EF Core database update"
    exit 0
}

Write-Host "🔄 Starting EF Core migration process..." -ForegroundColor Blue

# Check if Docker is running
try {
    docker info | Out-Null
} catch {
    Write-Host "❌ Error: Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Check if PostgreSQL container is running
$postgresRunning = docker ps --filter "name=urlshortener-postgres" --filter "status=running" | Select-String "urlshortener-postgres"
if (-not $postgresRunning) {
    Write-Host "❌ Error: PostgreSQL container 'urlshortener-postgres' is not running." -ForegroundColor Red
    Write-Host "   Please start your Docker Compose services first:" -ForegroundColor Yellow
    Write-Host "   docker-compose -f docker/docker-compose.yml up -d" -ForegroundColor Yellow
    exit 1
}

# Check if network exists
$networkExists = docker network ls | Select-String "urlshortener_network"
if (-not $networkExists) {
    Write-Host "❌ Error: Docker network 'urlshortener_network' not found." -ForegroundColor Red
    Write-Host "   Please start your Docker Compose services first:" -ForegroundColor Yellow
    Write-Host "   docker-compose -f docker/docker-compose.yml up -d" -ForegroundColor Yellow
    exit 1
}

# Load environment variables from .env file if it exists
if (Test-Path ".env") {
    Write-Host "📋 Loading environment variables from .env file..." -ForegroundColor Green
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
    $POSTGRES_DB = $env:POSTGRES_DB
    $POSTGRES_USER = $env:POSTGRES_USER
    $POSTGRES_PASSWORD = $env:POSTGRES_PASSWORD
} else {
    Write-Host "⚠️  Warning: .env file not found. Using default values." -ForegroundColor Yellow
    $POSTGRES_DB = "urlshortener"
    $POSTGRES_USER = "postgres"
    $POSTGRES_PASSWORD = "SecurePassword123!"
}

Write-Host "🏗️  Building migration Docker image..." -ForegroundColor Blue
docker build -f Dockerfile.migration -t migration-runner .

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: Failed to build migration Docker image." -ForegroundColor Red
    exit 1
}

Write-Host "🚀 Running EF Core migrations..." -ForegroundColor Blue
docker run --rm --network urlshortener_network migration-runner `
    dotnet ef database update --project src/Infrastructure --startup-project src/API `
    --connection "Host=postgres;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD;"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 You can verify the migration by checking your database:" -ForegroundColor Cyan
    Write-Host "   docker exec -it urlshortener-postgres psql -U $POSTGRES_USER -d $POSTGRES_DB -c '\dt'" -ForegroundColor Gray
} else {
    Write-Host "❌ Error: Migration failed!" -ForegroundColor Red
    exit 1
}