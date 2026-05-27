# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Edu-Nexus.sln", "./"]
COPY ["Edu-Nexus.APIs/Edu-Nexus.APIs.csproj", "Edu-Nexus.APIs/"]
COPY ["Edu-Nexus.Application/Edu-Nexus.Application.csproj", "Edu-Nexus.Application/"]
COPY ["Edu-Nexus.Domain/Edu-Nexus.Domain.csproj", "Edu-Nexus.Domain/"]
COPY ["Edu-Nexus.Infrastructure/Edu-Nexus.Infrastructure.csproj", "Edu-Nexus.Infrastructure/"]

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build and publish
WORKDIR "/src/Edu-Nexus.APIs"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Optional: Add any required apt packages here (e.g., tzdata, libfontconfig1 for PDF)
RUN apt-get update && apt-get install -y --no-install-recommends \
    tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=Asia/Ho_Chi_Minh

COPY --from=build /app/publish .

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Edu-Nexus.APIs.dll"]
