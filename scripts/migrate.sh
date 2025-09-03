#!/bin/bash

# EF Core Migration Script for URL Shortener
# This script runs Entity Framework Core migrations against the PostgreSQL database running in Docker

set -e  # Exit on any error

echo "🔄 Starting EF Core migration process..."
# Default to 'dev' if no argument is provided
TARGET_ENV="${1:-dev}"

if [[ ! "$TARGET_ENV" =~ ^(dev|staging)$ ]]; then
    echo "❌ Error: Unknown environment '$TARGET_ENV'. Use 'dev' or 'staging'."
    echo "   Usage: ./migrate.sh [dev|staging]"
    exit 1
fi

# Define environment-specific variables
if [ "$TARGET_ENV" == "staging" ]; then
    COMPOSE_FILE="compose/docker-compose.staging.yml"
    ENV_FILE="compose/.env.staging"
    PROJECT_PREFIX="compose" # Changed from "docker" to "compose" for staging
else
    COMPOSE_FILE="docker/docker-compose.yml"
    ENV_FILE=".env"
    PROJECT_PREFIX="docker" # Docker Compose project prefix for dev
fi

# Calculate container name based on prefix
POSTGRES_CONTAINER_NAME="${PROJECT_PREFIX}-postgres-1"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Error: Docker is not running. Please start Docker first."
    exit 1
fi

# Check if PostgreSQL container is running
if ! docker ps --filter "name=${POSTGRES_CONTAINER_NAME}" --filter "status=running" | grep -q "$POSTGRES_CONTAINER_NAME"; then
    echo "❌ Error: PostgreSQL container '$POSTGRES_CONTAINER_NAME' is not running."
    echo "   Please start your Docker Compose services first:"
    echo "   docker-compose -f $COMPOSE_FILE up -d"
    exit 1
fi

# Check if network exists (Docker Compose creates a default network named after the directory)
NETWORK_NAME="${PROJECT_PREFIX}_default"
if ! docker network ls | grep -q "$NETWORK_NAME"; then
    echo "❌ Error: Docker network '$NETWORK_NAME' not found."
    echo "   Please start your Docker Compose services first:"
    echo "   docker-compose -f $COMPOSE_FILE up -d"
    exit 1
fi

# Load environment variables from .env file if it exists
if [ -f "$ENV_FILE" ]; then
    echo "📋 Loading environment variables from $ENV_FILE file..."
    source "$ENV_FILE"
else
    echo "⚠️  Warning: $ENV_FILE file not found. Using default values."
    POSTGRES_DB=${POSTGRES_DB:-urlshortener}
    POSTGRES_USER=${POSTGRES_USER:-postgres}
    POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-SecurePassword123!}
fi

echo "🏗️  Building migration Docker image..."
docker build -f Dockerfile.migration -t "migration-runner-$TARGET_ENV" .

echo "🚀 Running EF Core migrations for $TARGET_ENV..."
docker run --rm --network "$NETWORK_NAME" "migration-runner-$TARGET_ENV"  \
    dotnet ef database update --project src/Infrastructure --startup-project src/API \
    --connection "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};"

echo "✅ $TARGET_ENV Migration completed successfully!"
echo ""
echo "📊 You can verify the migration by checking your database:"
echo "   docker exec -it $POSTGRES_CONTAINER_NAME psql -U ${POSTGRES_USER} -d ${POSTGRES_DB} -c '\\dt'"