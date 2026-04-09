# ─── Build stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY Directory.Build.props .
COPY src/GitLabClone.Domain/GitLabClone.Domain.csproj src/GitLabClone.Domain/
COPY src/GitLabClone.Application/GitLabClone.Application.csproj src/GitLabClone.Application/
COPY src/GitLabClone.Infrastructure/GitLabClone.Infrastructure.csproj src/GitLabClone.Infrastructure/
COPY src/GitLabClone.Api/GitLabClone.Api.csproj src/GitLabClone.Api/
RUN dotnet restore src/GitLabClone.Api/GitLabClone.Api.csproj

# Copy everything and build
COPY . .
RUN dotnet publish src/GitLabClone.Api/GitLabClone.Api.csproj \
    -c Release -o /app/publish --no-restore

# ─── Runtime stage ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install git (required for Smart HTTP clone/push)
RUN apt-get update && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser -d /app appuser

# Create repos directory
RUN mkdir -p /data/repos && chown -R appuser:appuser /data/repos

COPY --from=build /app/publish .

# Switch to non-root
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GitLabClone.Api.dll"]
