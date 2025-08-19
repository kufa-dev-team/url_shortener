# Multi-stage Dockerfile for ASP.NET Core URL Shortener
# Target: <250 MB final image size using Alpine base

# Build stage - Uses full SDK for compilation
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /app

# Copy solution and project files first for better layer caching
COPY *.sln ./
COPY src/API/API.csproj ./src/API/
COPY src/Application/Application.csproj ./src/Application/
COPY src/Domain/Domain.csproj ./src/Domain/
COPY src/Infrastructure/Infrastructure.csproj ./src/Infrastructure/
COPY tests/UrlShortener.Api.Tests/UrlShortener.Api.Tests.csproj ./tests/UrlShortener.Api.Tests/
COPY tests/UrlShortener.Infrastructure.Tests/UrlShortener.Infrastructure.Tests.csproj ./tests/UrlShortener.Infrastructure.Tests/

# Restore dependencies - This layer will be cached if project files don't change
RUN dotnet restore --runtime linux-musl-x64

# Copy source code
COPY src/ ./src/
COPY tests/ ./tests/

# Build the application for the target runtime
RUN dotnet build src/API/API.csproj -c Release --no-restore --runtime linux-musl-x64

# Publish the application with optimizations
RUN dotnet publish src/API/API.csproj -c Release --no-build --no-restore -o /app/publish \
    --runtime linux-musl-x64 \
    --self-contained false \
    --verbosity quiet \
    /p:PublishReadyToRun=false \
    /p:PublishSingleFile=false \
    /p:PublishTrimmed=false

# Runtime stage - Minimal runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Install dependencies for health checks and PostgreSQL
RUN apk add --no-cache \
    icu-libs \
    tzdata \
    wget

# Create non-root user for security
RUN addgroup -g 1001 -S appuser && \
    adduser -S appuser -G appuser -u 1001

WORKDIR /app

# Copy published application from build stage
COPY --from=build --chown=appuser:appuser /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://*:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "API.dll"]