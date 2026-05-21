# Edu-Nexus Backend API

Edu-Nexus is a modern education and career-path recommendation platform. This repository hosts the backend, built on **.NET 10** using **Clean Architecture** + **CQRS (MediatR)**, with an LLM/RAG pipeline that is abstracted behind interfaces so the system can run end-to-end without an LLM key during development.

---

## 🏗️ Architecture & Folder Structure

```
Edu-Nexus/
├── Edu-Nexus.Domain/             # Entities, enums (zero dependencies)
├── Edu-Nexus.Application/        # CQRS commands/queries, DTOs, interfaces
│   ├── DTOs/
│   ├── Features/
│   │   ├── Auth/
│   │   ├── Onboarding/
│   │   ├── JdSubmissions/
│   │   ├── AssessmentPaths/
│   │   ├── CvSubmissions/
│   │   └── AssessmentSessions/
│   └── Interfaces/
│       ├── Data/                 # IRepository, IUnitOfWork
│       ├── Security/             # ICurrentUserService, ITokenService, ...
│       ├── BackgroundJobs/       # IJdParseQueue, ICvParseQueue, IAssessmentGenerateQueue
│       ├── Storage/              # IFileStorage
│       └── Parsing/              # IJdParser, ICvParser, IAssessmentQuestionGenerator,
│                                 #   IPdfTextExtractor, IAnonymizer
├── Edu-Nexus.Infrastructure/     # EF Core, security, Hangfire jobs, parser impls
│   ├── Data/                     # DbContext, Repository, UnitOfWork
│   ├── Security/                 # BCrypt, JWT, Google OAuth
│   ├── BackgroundJobs/           # Hangfire dispatcher adapters
│   ├── Jobs/                     # Hangfire worker classes
│   ├── Parsing/                  # Fake*, PdfPig, RegexAnonymizer
│   └── Storage/                  # LocalFileStorage (wwwroot-backed)
├── Edu-Nexus.APIs/               # Controllers, Program.cs, DI wiring
│   ├── Controllers/
│   ├── Extensions/               # AddPresentation (Swagger + JWT)
│   └── wwwroot/                  # Static files + uploaded CVs
└── docker-compose.yml            # Postgres (pgvector) + Redis
```

### ⚡ CQRS (MediatR)
- **Commands** mutate state (submit JD, upload CV, submit assessment, ...).
- **Queries** read-only (list JDs, get session questions, ...).
- Handlers depend only on `IUnitOfWork`, `ICurrentUserService`, and parser/queue abstractions — they never touch HTTP or EF directly.

---

## 🛠️ Technology Stack

| Layer | Stack |
|---|---|
| Runtime | .NET 10 (C# 14) |
| Database | PostgreSQL 16 + **pgvector** (HNSW indices for RAG embeddings) |
| ORM | EF Core 9.0.4 (Npgsql + Pgvector.EntityFrameworkCore) |
| Async jobs | **Hangfire 1.8.23** with Postgres storage + dashboard |
| Cache | StackExchange.Redis (planned for Sprint 2 / RAG cache) |
| Security | JWT bearer, SHA-256 refresh-token hashing, BCrypt, Google ID-token verify |
| AI SDK | Microsoft.SemanticKernel 1.76 (kept behind `I*Parser` abstractions) |
| PDF/text | PdfPig (CV text extraction), regex-based anonymizer |
| Docs | Swagger UI with custom theme |

---

## 🚀 Getting Started

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker Desktop (for Postgres + Redis)

### 2. Start infrastructure

```bash
docker compose up -d
```

Brings up:
- `edu-nexus-db` — `pgvector/pgvector:pg16` on `localhost:5434`
- `edu-nexus-redis` — `redis:7-alpine` on `localhost:6380`

> The compose file attaches `edu-nexus-db` to a named external volume so existing data is preserved across rebuilds.

### 3. Configure secrets

`appsettings.json` ships placeholders only. Real values go into **User Secrets** (per-developer, never committed):

```bash
cd Edu-Nexus.APIs
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-proj-..."
```

The DB connection string is already pointed at the compose port:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=edu_nexus;Username=postgres;Password=edunexus123;",
    "Redis": "localhost:6380"
  }
}
```

### 4. Run

```bash
dotnet run --project Edu-Nexus.APIs
```

| URL | Purpose |
|---|---|
| `https://localhost:<port>/` | Swagger UI (root) |
| `https://localhost:<port>/hangfire` | Hangfire dashboard (dev only, **unauthenticated**) |

---

## 🔑 Implemented Sprint 1 Features

### S1.1 Auth (FR7.1)
Local register/login, Google OAuth, refresh-token rotation, `GET/PUT /users/me`.

### S1.2 Onboarding (FR1)
`GET/POST/PUT /onboarding`, gates downstream features via `users.is_survey_completed`.

### S1.3 JD Submission (FR2.1-2.2)
`POST/GET/GET-by-id/DELETE /jd-submissions`. Quota check (Free=3, fallback when no active subscription), onboarding pre-check, soft delete frees the slot. **JD parsing runs async via Hangfire** behind `IJdParser` — default binding `FakeJdParser` (heuristic keyword scan) so the flow works without an LLM.

### S1.4 Assessment Path (FR2.3)
`POST/DELETE /jd-submissions/:jdId/assessment-path`. Unique-per-JD with `409 PATH_ALREADY_EXISTS`. Path B (`assessment`) gated by `AssessmentQuota`. DELETE refuses with `422 CANNOT_RESET_AFTER_GAP` once a completed Gap Analysis exists, and relies on DB-level `ON DELETE CASCADE` to clean child rows.

### S1.5 CV Submission — Path A (FR2.3)
`POST/GET /assessment-paths/:pathId/cv` (multipart, 5 MB PDF). Hard-replace on re-upload (old file + row deleted), file lands in `wwwroot/uploads/cv/{guid}.pdf` via `LocalFileStorage`. Pipeline: open file → `PdfPigTextExtractor` → `RegexAnonymizer` (email/phone/URL) → `ICvParser`. Default binding `FakeCvParser` performs keyword-mention heuristics. FR3.5: on re-upload after a completed Gap Analysis, active roadmaps are marked `is_outdated = TRUE` (Gap re-enqueue lands in S2.1).

### S1.6 Assessment Module — Path B (FR3)
- `POST /assessment-paths/:pathId/sessions` — generate or **reuse** (`reuseSessionId`).
- `GET /assessment-sessions/:sessionId/questions` — returns questions without `correctOption`/`explanation`.
- `POST /assessment-sessions/:sessionId/submit` — auto-grades, aggregates skill scores into `skill_scores` JSONB (thresholds 75/50/25 → advanced/intermediate/beginner/none).
- `GET /assessment-sessions/:sessionId` — final result.
- `GET /jd-submissions/:jdId/reusable-sessions` — surfaces submitted sessions whose `job_role_category_snapshot` matches the current JD (FR3.4).

`FakeAssessmentQuestionGenerator` ships a curated MCQ bank for Java, Spring Boot, C#/.NET, SQL, Docker, React, JavaScript, OOP fundamentals, plus soft skills. Unknown skills fall back to templated questions. Default split: 11 Part 1 + 7 Part 2.

---

## 🧱 Architecture Notes

### Async pipelines via Hangfire
Every AI-shaped task (`JdParse`, `CvParse`, `AssessmentGenerate`) follows the same shape:

1. An **Application-layer interface** in `Interfaces/BackgroundJobs/` — e.g. `IJdParseQueue.Enqueue(id)`.
2. An **Infrastructure adapter** (`Hangfire*Queue`) implementing it via `IBackgroundJobClient`.
3. A **Hangfire worker class** in `Infrastructure/Jobs/` (e.g. `JdParseJob`) that performs the work.

Workers update the entity's status from `pending → processing → completed/failed` and persist any parse error so the API can surface it on GET.

### Swap fake parsers for real LLM
Every parser is bound through DI in `Edu-Nexus.Infrastructure/DependencyInjection.cs`. To switch a single pipeline to a real LLM-backed implementation:

```csharp
// services.AddScoped<IJdParser, FakeJdParser>();
services.AddScoped<IJdParser, OpenAiJdParser>();
```

No controller / handler / job touches the change.

### Generic repository + UoW
- Generic `IRepository<T>` exposes `GetById`, `FirstOrDefault`, `Find` with **string-based** `includeProperties` so handlers can pre-load navigation paths (`"AssessmentPath.Jd"`, `"JdSkills,AssessmentPath"`, ...) without leaking IQueryable.
- `IUnitOfWork.SaveChangesAsync()` commits the EF change tracker; repositories never call `SaveChanges` themselves.

### `ICurrentUserService`
Reads JWT claims via `IHttpContextAccessor`, exposes `UserId` / `Email` to handlers. Hangfire jobs do **not** have an HTTP context — they pass `userId` explicitly in the job signature.

---

## 🗃️ Database

The schema is provisioned from SQL scripts under `DATABASE-Phase1-v4.1/`. Hangfire creates its own `hangfire` schema on first run (no manual setup needed).

Key FK delete rules (verified on running DB):

| Constraint | Rule |
|---|---|
| `assessment_sessions → assessment_paths` | CASCADE |
| `assessment_questions → assessment_sessions` | CASCADE |
| `assessment_answers → assessment_questions / sessions` | CASCADE |
| `cv_submissions → assessment_paths` | CASCADE |
| `gap_analyses → assessment_paths` | CASCADE |
| `assessment_paths → jd_submissions` | CASCADE |

Soft delete applies to `users` and `jd_submissions` only (`deleted_at` column).

---

## 🧪 Testing the Sprint 1 flow

End-to-end happy path on Swagger:

1. `POST /auth/register` → `POST /auth/login` → click **Authorize** with `accessToken`.
2. `POST /onboarding` with any valid survey.
3. `POST /jd-submissions` (`sourceType: "text"`, paste a JD into `rawContent`).
4. `POST /jd-submissions/{jdId}/assessment-path` (`pathType: "cv"` or `"assessment"`).
5a. **Path A:** `POST /assessment-paths/{pathId}/cv` (multipart PDF) → poll `GET /assessment-paths/{pathId}/cv`.
5b. **Path B:** `POST /assessment-paths/{pathId}/sessions` → poll `GET /assessment-sessions/{sessionId}/questions` → answer → `POST /assessment-sessions/{sessionId}/submit`.

Open `/hangfire` in parallel to watch each pipeline finish.

---

## 🛣️ Roadmap

- **S2.1 Gap Analysis** (FR2.4) — wires `IGapAnalysisQueue`, fills the FR3.5 auto-trigger that S1.5/S1.6 left as TODOs.
- **S2.2 Roadmap** (FR4) — LLM roadmap generation + resource matching.
- **S2.3 Career Track**, **S2.4 Resources**.
- **Sprint 3** — Portfolio, Subscriptions, Admin.

When the real LLM/RAG pipelines come online, the only changes are DI rebinds in `Edu-Nexus.Infrastructure/DependencyInjection.cs` plus environment configuration.
