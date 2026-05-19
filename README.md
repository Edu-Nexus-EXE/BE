# Edu-Nexus Backend API

Edu-Nexus is a modern, high-performance education and career path recommendation platform. This repository contains the backend system built on top of **.NET 10** using **Clean Architecture** and **CQRS (MediatR)** principles.

---

## 🏗️ Architecture & Folder Structure

The project strictly follows the **Clean Architecture** layout to ensure loose coupling, high testability, and a clear separation of concerns:

```
Edu-Nexus/
├── Edu-Nexus.Domain/          # Core domain models, entities, and enums (Zero dependencies)
├── Edu-Nexus.Application/     # CQRS Commands/Queries, Interfaces, DTOs, and Validators
├── Edu-Nexus.Infrastructure/  # EF Core DbContext, Repositories, Unit of Work, Security Services
└── Edu-Nexus.APIs/            # Controllers, Middleware, configurations, and API entry point
```

### ⚡ CQRS (MediatR)
All business operations are segregated into:
- **Queries**: Read-only operations (e.g., fetching profiles, searching courses).
- **Commands**: State-mutating operations (e.g., registering user, logging out).

---

## 🛠️ Technology Stack

- **Core Framework**: .NET 10 (C# 14)
- **Database Layer**: Entity Framework Core with PostgreSQL & **Pgvector** (for vector-embeddings search)
- **Design Patterns**: Generic Repository & Unit of Work (encapsulating all database operations)
- **Security**: 
  - JWT Bearer Access Tokens
  - SHA-256 Hashed Refresh Tokens (stored in DB)
  - Google OAuth 2.0 Token Verification
  - BCrypt.Net for local password hashing
- **API Documentation**: Swagger UI (via Swashbuckle) with custom cyberpunk UI theme and built-in Bearer Authorization support.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/) with [pgvector extension](https://github.com/pgvector/pgvector) (or Docker container)

### 1. Database Configuration
Update the connection string in `Edu-Nexus.APIs/appsettings.json` (or `appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=edunexus_db;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "Key": "YourSuperSecretJWTKeyWhichMustBeVeryLongAndSecure!",
    "Issuer": "http://localhost:5240",
    "Audience": "http://localhost:5240",
    "AccessTokenDurationMinutes": 15
  },
  "Google": {
    "ClientId": "your-google-client-id.apps.googleusercontent.com"
  }
}
```

### 2. Run the Application
Navigate to the API folder and run:
```bash
cd Edu-Nexus.APIs
dotnet run
```
By default, the server runs on:
- HTTP: `http://localhost:5240`
- HTTPS: `https://localhost:7157`

### 3. Open API Documentation (Swagger)
Visit the root URL in your browser:
👉 **`https://localhost:7157/`** (Automatically serves Swagger UI)

---

## 🔑 Key Features Implemented

### 🛡️ Secure Authentication Infrastructure
- **Local Login & Register**: Uses salted BCrypt hashing. Automatic database trigger-based registration of Free subscription tier.
- **Google Social Authentication**: Handled securely via `IGoogleAuthService` verifying ID tokens and mapping user details.
- **JWT & Hashed Refresh Tokens**: Implements stateless short-lived JWT access tokens and cryptographically secure random refresh tokens stored as SHA-256 hashes in PostgreSQL.

### 💼 Unit of Work & Repository Pattern
- Decouples EF Core from the **Application** layer.
- generic `IRepository<T>` supports eager loading using string-based navigation paths (e.g. `IncludeProperties: "UserSubscription,UserSubscription.Tier"`) to resolve N+1 queries.

### 🌐 Current User Context (`ICurrentUserService`)
- Fully encapsulated `ICurrentUserService` utilizing `IHttpContextAccessor` to automatically pull the logged-in user's claims directly into CQRS Handlers, eliminating token parsing boilerplate in the Controllers.