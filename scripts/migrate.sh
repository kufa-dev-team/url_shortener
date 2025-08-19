#!/bin/bash

# EF Core Migration Script for URL Shortener
# This script runs Entity Framework Core migrations against the PostgreSQL database running in Docker

set -e  # Exit on any error

echo "🔄 Starting EF Core migration process..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Error: Docker is not running. Please start Docker first."
    exit 1
fi

# Check if PostgreSQL container is running
if ! docker ps --filter "name=urlshortener-postgres" --filter "status=running" | grep -q urlshortener-postgres; then
    echo "❌ Error: PostgreSQL container 'urlshortener-postgres' is not running."
    echo "   Please start your Docker Compose services first:"
    echo "   docker-compose -f docker/docker-compose.yml up -d"
    exit 1
fi

# Check if network exists
if ! docker network ls | grep -q urlshortener_network; then
    echo "❌ Error: Docker network 'urlshortener_network' not found."
    echo "   Please start your Docker Compose services first:"
    echo "   docker-compose -f docker/docker-compose.yml up -d"
    exit 1
fi

# Load environment variables from .env file if it exists
if [ -f .env ]; then
    echo "📋 Loading environment variables from .env file..."
    source .env
else
    echo "⚠️  Warning: .env file not found. Using default values."
    POSTGRES_DB=${POSTGRES_DB:-urlshortener}
    POSTGRES_USER=${POSTGRES_USER:-postgres}
    POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-SecurePassword123!}
fi

echo "🏗️  Building migration Docker image..."
docker build -f Dockerfile.migration -t migration-runner .

echo "🚀 Running EF Core migrations..."
docker run --rm --network urlshortener_network migration-runner \
    dotnet ef database update --project src/Infrastructure --startup-project src/API \
    --connection "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};"

echo "✅ Migration completed successfully!"
echo ""
echo "📊 You can verify the migration by checking your database:"
echo "   docker exec -it urlshortener-postgres psql -U ${POSTGRES_USER} -d ${POSTGRES_DB} -c '\\dt'"