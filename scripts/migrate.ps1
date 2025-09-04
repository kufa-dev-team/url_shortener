# EF Core Migration Script for URL Shortener (PowerShell)
# Usage: .\migrate.ps1 [-Environment <dev|staging>] [-Help]

param(
    [string]$Environment = "dev",
    [switch]$Help
)

if ($Help) {
    Write-Host "EF Core Migration Script for URL Shortener" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\migrate.ps1 [-Environment <dev|staging>]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Environment    Target environment (default: dev)"
    Write-Host "  -Help           Show this help message"
    Write-Host ""
    exit 0
}

# Validate the input argument
if ($Environment -notin @("dev", "staging")) {
    Write-Host "❌ Error: Unknown environment '$Environment'. Use 'dev' or 'staging'." -ForegroundColor Red
    exit 1
}

Write-Host "🔄 Starting EF Core migration process for the '$Environment' environment..." -ForegroundColor Cyan

# Define environment-specific variables
if ($Environment -eq "staging") {
    $COMPOSE_FILE = "compose/docker-compose.staging.yml"
    $ENV_FILE = "compose/.env.staging"
    $PROJECT_PREFIX = "compose" # Docker Compose project prefix for staging
} else {
    $COMPOSE_FILE = "docker/docker-compose.yml"
    $ENV_FILE = ".env"
    $PROJECT_PREFIX = "docker" # Docker Compose project prefix for dev
}

# Check if Docker is running
try {
    docker info | Out-Null
} catch {
    Write-Host "❌ Error: Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Check if PostgreSQL container is running
$POSTGRES_CONTAINER_NAME = "${PROJECT_PREFIX}-postgres-1"
$postgresRunning = docker ps --filter "name=$POSTGRES_CONTAINER_NAME" --filter "status=running" | Select-String $POSTGRES_CONTAINER_NAME
if (-not $postgresRunning) {
    Write-Host "❌ Error: PostgreSQL container '$POSTGRES_CONTAINER_NAME' is not running." -ForegroundColor Red
    Write-Host "   Please start your Docker Compose services first:" -ForegroundColor Yellow
    Write-Host "   docker-compose -f $COMPOSE_FILE up -d" -ForegroundColor Yellow
    exit 1
}

# Check if network exists
$NETWORK_NAME = "${PROJECT_PREFIX}_default"
$networkExists = docker network ls | Select-String $NETWORK_NAME
if (-not $networkExists) {
    Write-Host "❌ Error: Docker network '$NETWORK_NAME' not found." -ForegroundColor Red
    Write-Host "   Please start your Docker Compose services first:" -ForegroundColor Yellow
    Write-Host "   docker-compose -f $COMPOSE_FILE up -d" -ForegroundColor Yellow
    exit 1
}

# Load environment variables from the appropriate .env file
if (Test-Path $ENV_FILE) {
    Write-Host "📋 Loading environment variables from $ENV_FILE file..." -ForegroundColor Green
    Get-Content $ENV_FILE | ForEach-Object {
        if ($_ -match "^([^#][^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
    $POSTGRES_DB = $env:POSTGRES_DB
    $POSTGRES_USER = $env:POSTGRES_USER
    $POSTGRES_PASSWORD = $env:POSTGRES_PASSWORD
} else {
    Write-Host "⚠️  Warning: $ENV_FILE file not found. Using default values." -ForegroundColor Yellow
    $POSTGRES_DB = "urlshortener"
    $POSTGRES_USER = "postgres"
    $POSTGRES_PASSWORD = "SecurePassword123!"
}

Write-Host "🏗️  Building migration Docker image..." -ForegroundColor Cyan
docker build -f Dockerfile.migration -t "migration-runner-$Environment" .

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: Failed to build migration Docker image." -ForegroundColor Red
    exit 1
}

Write-Host "🚀 Running EF Core migrations for $Environment..." -ForegroundColor Cyan
docker run --rm --network $NETWORK_NAME "migration-runner-$Environment" `
    dotnet ef database update --project src/Infrastructure --startup-project src/API `
    --connection "Host=postgres;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD;"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ $Environment migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 You can verify the migration by checking your database:" -ForegroundColor Cyan
    Write-Host "   docker exec -it $POSTGRES_CONTAINER_NAME psql -U $POSTGRES_USER -d $POSTGRES_DB -c '\dt'" -ForegroundColor Gray
} else {
    Write-Host "❌ Error: Migration failed!" -ForegroundColor Red
    exit 1
}