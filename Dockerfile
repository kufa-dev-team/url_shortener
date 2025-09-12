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



# Copy source code
COPY src/ ./src/
COPY tests/ ./tests/

# Build the application for the target runtime
RUN dotnet restore src/API/API.csproj -r linux-musl-x64
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

# Install dependencies for health checks, networking, and certs
RUN apk add --no-cache \
    icu-libs \
    tzdata \
    wget \
    ca-certificates

# Install New Relic .NET agent (musl build for Alpine)
RUN mkdir -p /usr/local/newrelic-dotnet-agent && \
    wget -O /tmp/newrelic-dotnet-agent-musl.tar.gz \
      https://download.newrelic.com/dot_net_agent/latest_release/newrelic-dotnet-agent-musl.tar.gz && \
    tar -xzf /tmp/newrelic-dotnet-agent-musl.tar.gz -C /usr/local/newrelic-dotnet-agent && \
    rm -f /tmp/newrelic-dotnet-agent-musl.tar.gz

# Create non-root user for security
RUN addgroup -g 1001 -S appuser && \
    adduser -S appuser -G appuser -u 1001

# Ensure the agent files are readable by the app user
RUN chown -R appuser:appuser /usr/local/newrelic-dotnet-agent

WORKDIR /app

# Copy published application from build stage
COPY --from=build --chown=appuser:appuser /app/publish .

# Set environment variables (inject NEW_RELIC_LICENSE_KEY at runtime via compose/CI)
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://*:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    NEW_RELIC_DOTNET_AGENT_PATH=/usr/local/newrelic-dotnet-agent \
    NEW_RELIC_APP_NAME="URL Shortener"

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "API.dll"]