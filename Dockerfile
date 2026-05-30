# syntax=docker/dockerfile:1.7

# ============================================================================
# Stage 1: Build & publish
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj/slnx first to leverage Docker layer cache for restore
COPY Edu-Nexus.slnx ./
COPY Edu-Nexus.APIs/Edu-Nexus.APIs.csproj            Edu-Nexus.APIs/
COPY Edu-Nexus.Application/Edu-Nexus.Application.csproj   Edu-Nexus.Application/
COPY Edu-Nexus.Domain/Edu-Nexus.Domain.csproj        Edu-Nexus.Domain/
COPY Edu-Nexus.Infrastructure/Edu-Nexus.Infrastructure.csproj Edu-Nexus.Infrastructure/

RUN dotnet restore Edu-Nexus.APIs/Edu-Nexus.APIs.csproj

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish Edu-Nexus.APIs/Edu-Nexus.APIs.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============================================================================
# Stage 2: Runtime
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=Asia/Ho_Chi_Minh

# Persistent uploads (CVs). Mount a volume to this path in docker-compose.prod.yml
RUN mkdir -p /app/wwwroot/uploads/cv

COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "Edu-Nexus.APIs.dll"]
