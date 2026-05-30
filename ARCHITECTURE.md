# Edu-Nexus Backend — Architecture

> Phạm vi: file này mô tả kiến trúc, layering, conventions và data-flow của solution `BE/Edu-Nexus.slnx` để Claude (hoặc dev mới) có thể implement đúng pattern, không lệch khỏi codebase hiện có.
>
> Đối chiếu spec: `API-DB/V4/API-Specification-Phase1-v2.md`, `API-DB/V4/DATABASE-Phase1-v4.1.md`, `API-DB/V4/RAG-Config-Phase1-v4.md`, `API-DB/V4/sprint.md`.

---

## 1. Solution Layout

```
BE/
├── Edu-Nexus.slnx
├── docker-compose.yml          # Postgres (pgvector/pg16) :5434, Redis :6380
├── Edu-Nexus.APIs/             # ASP.NET Core host (net10.0)
├── Edu-Nexus.Application/      # MediatR handlers, DTOs, Interfaces (no infra deps)
├── Edu-Nexus.Domain/           # Entities + Enums (POCO, không phụ thuộc EF)
└── Edu-Nexus.Infrastructure/   # EF Core, Hangfire, Parsers (Fake + OpenAI), Security, Storage
```

### Dependency rule (Clean / Onion)

```
APIs ──▶ Application ◀── Infrastructure
              ▲
              │
            Domain (no outward dep)
```

* `Application` chỉ tham chiếu `Domain`.
* `Infrastructure` tham chiếu `Application` (implement các interface) và `Domain`.
* `APIs` tham chiếu cả `Application` và `Infrastructure` để wire DI; controller chỉ gọi `IMediator`.

### Target framework / packages chính

| Project           | TargetFramework | Packages quan trọng                                                                                                                                                                                                  |
| ----------------- | --------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| APIs              | net10.0         | `Microsoft.AspNetCore.Authentication.JwtBearer` 10, `Swashbuckle.AspNetCore` 7, `Hangfire.AspNetCore` 1.8                                                                                                            |
| Application       | net10.0         | `MediatR` (qua DI), không reference EF/Hangfire                                                                                                                                                                      |
| Domain            | net10.0         | Không có package ngoài                                                                                                                                                                                               |
| Infrastructure    | net10.0         | `Npgsql.EntityFrameworkCore.PostgreSQL` 9, `Pgvector(.EFCore)` 0.3, `Hangfire.PostgreSql` 1.20, `Microsoft.SemanticKernel` 1.76 *(chỉ dùng cục bộ trong `OpenAiGapAnalyzer` — không có kernel toàn cục)*, `PdfPig`, `BCrypt.Net-Next`, `Google.Apis.Auth`, `System.IdentityModel.Tokens.Jwt` 8. Package ref nhưng KHÔNG dùng: `HtmlAgilityPack`, `Microsoft.Extensions.Caching.StackExchangeRedis`. |

---

## 2. Layer Responsibilities

### 2.1 Domain (`Edu-Nexus.Domain`)

* `Entities/` — 33 POCO bám 1-1 với 33 bảng PostgreSQL trong [`DATABASE-Phase1-v4.1.md`](../API-DB/V4/DATABASE-Phase1-v4.1.md). Mọi entity dùng `partial class`, navigation properties đầy đủ (1-1, 1-N, M-N).
* `Enums/` — tổ chức theo **folder per aggregate** (`Enums/Users/UserRole.cs`, `Enums/JdSubmissions/ParseStatus.cs`, …). Một enum chỉ đặt trong namespace `Edu_Nexus.Domain.Enums.<Aggregate>`. Khi thêm enum mới: tạo folder mới + namespace mới.
* Không có business logic trong entity (anemic model) — toàn bộ logic ở Application handlers hoặc Infrastructure jobs.

### 2.2 Application (`Edu-Nexus.Application`)

#### CQRS-lite với MediatR

* Mỗi feature ở `Features/<Aggregate>/Commands/` hoặc `Features/<Aggregate>/Queries/`.
* Một file = **một `record` Command/Query + handler đi kèm**.
  Ví dụ: [Features/JdSubmissions/Commands/SubmitJdCommand.cs](Edu-Nexus.Application/Features/JdSubmissions/Commands/SubmitJdCommand.cs).
* Naming: `<Verb><Aggregate>Command` (ghi), `Get<…>Query` (đọc).
* DI: chỉ `AddMediatR(...RegisterServicesFromAssembly(...))` trong [DependencyInjection.cs](Edu-Nexus.Application/DependencyInjection.cs).

#### DTOs (`DTOs/`)

* Đặt theo aggregate (`AuthDTOs.cs`, `JdDTOs.cs`, …).
* Toàn bộ là `record` immutable, PascalCase.
* DTO không leak entity ra controller — handler convert thủ công.

#### Interfaces (`Interfaces/`)

* `Data/` — `IRepository<T>`, `IUnitOfWork` (repo gốc generic + 16 named DbSet properties).
* `Security/` — `IPasswordHasher`, `ITokenService`, `IGoogleAuthService`, `ICurrentUserService`.
* `Parsing/` — `IJdParser`, `ICvParser`, `IAssessmentQuestionGenerator`, `IGapAnalyzer`, `IPdfTextExtractor`, `IAnonymizer`. Mỗi interface có record DTO đi kèm trong cùng file.
* `Storage/` — `IFileStorage` (Save/OpenRead/Delete).
* `BackgroundJobs/` — `IJdParseQueue`, `ICvParseQueue`, `IAssessmentGenerateQueue`, `IGapAnalysisQueue`. Mỗi queue chỉ có 1 method `Enqueue(...)` — handler không biết Hangfire.

### 2.3 Infrastructure (`Edu-Nexus.Infrastructure`)

#### Data (`Data/`)

* [`EduNexusDbContext.cs`](Edu-Nexus.Infrastructure/Data/EduNexusDbContext.cs) — Fluent API explicit cho **mọi** table/index/relationship. **Match 1-1 với SQL trong [DATABASE-Phase1-v4.1.md](../API-DB/V4/DATABASE-Phase1-v4.1.md).**
  * 3 PostgreSQL extensions: `pg_trgm`, `uuid-ossp`, `vector`.
  * Enum được convert sang string snake_case (`HasConversion(v => v.ToString().ToLower(), …)`).
  * Partial unique indexes mô phỏng đúng DB: `roadmaps` (status IN active/generating), `assessment_sessions` (is_current=true), `user_subscriptions` (status=active), `gap_analyses` (is_latest=true).
  * pgvector: `RagChunk.Embedding` map `VECTOR(1536)` với HNSW index (`vector_cosine_ops`).
* `Repository<T>` — generic, có `Add/Update/Remove/GetById/FirstOrDefault/Find/GetAll` + `includeProperties` string CSV.
* `UnitOfWork` — composite 16 typed repo + `SaveChangesAsync`. Mọi handler inject `IUnitOfWork` chứ không inject `DbContext`.

> Quan trọng: `Repository.FindAsync` trả `IEnumerable<T>` đã materialize. Pagination / Count đang chạy in-memory. Cần lưu ý khi data lớn — nhưng KHÔNG refactor trừ khi user yêu cầu (xem [AGENT.md](AGENT.md) §Conventions).

#### Background Jobs (`BackgroundJobs/` + `Jobs/`)

* `BackgroundJobs/<Queue>Queue.cs` — implement interface, gọi `IBackgroundJobClient.Enqueue<TJob>(...)`.
* `Jobs/<X>Job.cs` — class chứa method `RunAsync(id, ct)` để Hangfire reflect. Pattern thống nhất:
  1. Re-fetch entity bằng `IUnitOfWork`.
  2. Set status → `processing`, save.
  3. Gọi parser/analyzer.
  4. Map kết quả → child entities.
  5. Set status → `completed`, save.
  6. Catch all → set `failed` + `error`, save, `throw` (để Hangfire retry).
* Cross-pipeline triggers (FR3.5):
  * [`CvParseJob`](Edu-Nexus.Infrastructure/Jobs/CvParseJob.cs) khi `isReupload` → mark roadmaps `is_outdated` + enqueue Gap re-run mới (version++).
  * [`GapAnalysisJob`](Edu-Nexus.Infrastructure/Jobs/GapAnalysisJob.cs) khi `version > 1` → mark active roadmaps `is_outdated = true`.

#### Parsing (`Parsing/`) — AI layer hiện tại

> **Trạng thái Sprint 1:** Có 4 Fake parsers + 1 LLM-backed analyzer (`OpenAiGapAnalyzer`). KHÔNG có RAG/Embedding/LlmService abstraction. Sprint 2 sẽ build các thứ này.

* **`FakeJdParser`** — heuristic keyword extract job title/category/seniority + hard/soft skills.
* **`FakeCvParser`** — keyword detection trên anonymized text.
* **`FakeAssessmentQuestionGenerator`** — curated MCQ bank cho top skill (Java, Spring Boot, C#/.NET, SQL, Docker, React, JS, OOP) + templated fallback. Default split 11 Part 1 + 7 Part 2.
* **`FakeGapAnalyzer`** — heuristic so sánh JD skills vs CV/Assessment.
* **`OpenAiGapAnalyzer`** — **standalone LLM**: tự `new OpenAIChatCompletionService(model, apiKey)` trong constructor từ `IConfiguration`. Không qua SK kernel chung. Force `ResponseFormat = "json_object"`, temperature 0.2. Parse JSON bằng `JsonDocument.Parse` + defensive fallback nếu LLM bỏ sót skill. **Không log token cost vào `rag_query_logs`** (chưa wire).
* **Stateless helpers (Singleton):** `PdfPigTextExtractor`, `RegexAnonymizer`, `LocalFileStorage`.

Switch fake ↔ LLM hiện chỉ áp dụng cho 1 pipeline duy nhất:

```csharp
services.AddScoped<IGapAnalyzer>(sp =>
    aiEnabled
        ? sp.GetRequiredService<OpenAiGapAnalyzer>()
        : sp.GetRequiredService<FakeGapAnalyzer>());
```

Các parser khác (`IJdParser`, `ICvParser`, `IAssessmentQuestionGenerator`) hiện **luôn** bind về Fake — chưa có OpenAI variant.

> Khi build Sprint 2 RAG infrastructure, dự kiến tạo folder `Infrastructure/Ai/` với `ILlmService`/`IEmbeddingService`/`IRagService`/`ISkillMatcherService` + `AddSemanticKernel` extension. Hiện chưa có.

#### Security (`Security/`)

* JWT HS256, 15 phút access, 30 ngày refresh — config trong [`appsettings.json`](Edu-Nexus.APIs/appsettings.json) (`Jwt` section).
* Claim `sub` = `user.Id.ToString()` (mặc định JWT sẽ map sang `ClaimTypes.NameIdentifier` mà `CurrentUserService` đọc).
* Refresh token: random 32 bytes → base64. Hash bằng SHA-256 trước khi lưu (`RefreshToken.TokenHash`).
* Password: BCrypt qua `BCrypt.Net.BCrypt.HashPassword`.
* Google: `Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync` với audience = `Google:ClientId`.

### 2.4 APIs (`Edu-Nexus.APIs`)

* `Program.cs` — chain DI: `AddApplication().AddInfrastructure(config).AddPresentation(config)`. Middleware order: Swagger (Dev only) → HttpsRedirect → StaticFiles → Authentication → Authorization → Hangfire dashboard (Dev) → MapControllers.
* `Extensions/DependencyInjection.cs` — JWT bearer + Swagger (with bearer security).
* `Controllers/` — 8 controllers, **mỗi controller chỉ inject `IMediator`**. Action method gọi `_mediator.Send(...)`, bọc try/catch theo từng error string.
* Error handling: **per-action `catch (Exception ex) when (ex.Message == "404 NOT_FOUND")` mapping**. Đây là convention hiện tại — KHÔNG có global exception middleware. Mới thêm endpoint cũng phải bắt theo pattern này (xem [AGENT.md §Error Codes](AGENT.md#error-codes)).

---

## 3. Cross-Cutting Patterns

### 3.1 Error signalling

Handler ném `new Exception("<HTTP_CODE> <ERROR_CODE>")`, ví dụ:
* `throw new Exception("401 UNAUTHORIZED")`
* `throw new Exception("404 JD_NOT_FOUND")`
* `throw new Exception("422 INVALID_DATA")`
* `throw new Exception("409 EMAIL_EXISTS")`
* Quota: `throw new Exception($"403 QUOTA_EXCEEDED|{quotaType}|{current}|{limit}")` — controller split bằng `|` để build response JSON.

Controller mapping:

```csharp
catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
{
    return NotFound(new { error = new { code = "NOT_FOUND", message = "..." } });
}
```

Response shape:
* Success: `{ "data": <payload> }` hoặc `{ "data": [...], "pagination": {...} }`
* Error: `{ "error": { "code": "...", "message": "...", ...extra } }`

### 3.2 Auth & Current User

Mọi controller (ngoại trừ AuthController endpoints `Public`) gắn `[Authorize]` ở class-level. Handler:

```csharp
var userId = _currentUserService.UserId
    ?? throw new Exception("401 UNAUTHORIZED");
```

`ICurrentUserService.UserId` đọc claim `ClaimTypes.NameIdentifier` từ `IHttpContextAccessor`.

### 3.3 Quota enforcement (Option A — DB v4 §19)

* Quota check **luôn ở Application handler**, ngay trước khi insert entity tạo mới.
* Lookup `UserSubscription` (status=active) → join `Tier` (include `nameof(UserSubscription.Tier)`) → đọc field `*Quota`. `-1` = unlimited.
* Đếm theo rules:
  * **JD**: `JdSubmissions WHERE UserId AND DeletedAt IS NULL` — đếm bản ghi active. Soft-delete giải phóng slot.
  * **Gap Analysis**: `JdId` khác nhau có completed gap. Re-run cùng JD KHÔNG trừ.
  * **Assessment**: `AssessmentPaths WHERE PathType = Assessment` (mỗi JD chọn Path B chỉ trừ 1 lần, retake free).
  * **Roadmap** *(chưa implement)*: count `status='active'`.
* Pattern code:

```csharp
private async Task EnforceXxxQuotaAsync(Guid userId, CancellationToken ct)
{
    var subscription = await _unitOfWork.UserSubscriptions.FirstOrDefaultAsync(
        s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active,
        includeProperties: nameof(UserSubscription.Tier),
        cancellationToken: ct);
    var quota = subscription?.Tier?.XxxQuota ?? 3;          // fallback Free
    if (quota < 0) return;                                  // unlimited
    var current = ...;
    if (current >= quota)
        throw new Exception($"403 QUOTA_EXCEEDED|<type>|{current}|{quota}");
}
```

### 3.4 Async processing

* Mọi pipeline nặng (JD parse, CV parse, Assessment gen, Gap analysis) trả về **202 Accepted** + entity với status `pending`/`generating`.
* FE poll endpoint chi tiết (`GET /jd-submissions/:id`, `GET /assessment-sessions/:id`, ...) để check `status`.
* Hangfire job re-load entity, không pass entity object qua queue (thread-safety).

### 3.5 Soft delete

* Chỉ áp dụng cho `JdSubmissions` và `Users`.
* Tất cả query đều cần filter `j.DeletedAt == null`. **Convention chung — luôn thêm filter này khi viết query mới.**

### 3.6 Versioning Gap Analysis

* Khi rerun: load tất cả `GapAnalyses` cùng `jdId + userId`, flip `IsLatest=false` cho cái cũ, insert mới với `Version = max + 1, IsLatest = true`.
* Unique index `idx_gap_jd_latest` đảm bảo chỉ 1 latest per JD.
* Wrap trong 1 `SaveChangesAsync` (transaction implicit).

### 3.7 Assessment session current flag

* Khi user start session mới (không reuse), flip `is_current=false` cho session cũ trước, save, INSERT mới với `is_current=true` → Unique partial index `idx_assessment_sessions_current` enforce.

---

## 4. Data Flow Examples

### 4.1 Submit JD (URL hoặc text) → Parse → Hiển thị

```
[FE] POST /jd-submissions { sourceType, sourceUrl/rawContent }
        │
        ▼
[JdSubmissionsController.Submit]
        │ MediatR
        ▼
[SubmitJdCommandHandler]
   1. currentUserId from JWT
   2. Validate sourceType + content (max 50_000 chars)
   3. Check user.IsSurveyCompleted → else 422 ONBOARDING_REQUIRED
   4. Enforce JD quota
   5. INSERT jd_submissions (parse_status='pending')
   6. _jdParseQueue.Enqueue(jd.Id)
   7. Return 202 { id, sourceType, parseStatus, createdAt }
        │
        ▼ (async, Hangfire worker thread)
[JdParseJob.RunAsync]
   1. parse_status='processing' + save
   2. _jdParser.ParseAsync(rawContent)     ← FakeJdParser hiện tại
   3. UPDATE job_title, role_category, seniority, salary
   4. INSERT jd_skills (hard + soft, skill_id = null vì chưa fuzzy match)
   5. parse_status='completed' + parsed_at
   FAIL → parse_status='failed' + parse_error + throw

[FE] poll GET /jd-submissions/:id → khi parse_status='completed' hiển thị chi tiết
```

### 4.2 Assessment retake → Auto-rerun Gap Analysis (FR3.5)

```
[FE] POST /assessment-sessions/:id/submit { answers }
        │
        ▼
[SubmitAssessmentSessionCommandHandler]
   1. Load session + questions
   2. Validate count match
   3. INSERT assessment_answers (auto-grade)
   4. Aggregate skill_scores → JSONB
   5. session.Status='submitted' + submitted_at + skill_scores
   6. BuildAutoTriggeredAsync:
      - find prior completed gap for cùng JD
      - if none → return null  (lần đầu, không auto-trigger)
      - else: flip is_latest=false cho gap cũ
      - INSERT new gap (version=old+1, is_latest=true, status='pending')
      - _gapAnalysisQueue.Enqueue(newGap.Id)
   7. Return 200 { ..., autoTriggered: { gapAnalysisId, status } | null }
        │
        ▼ async
[GapAnalysisJob.RunAsync]
   1. status='processing'
   2. BuildInputAsync: load JD+skills, CV/Assessment depending path, onboarding
   3. _analyzer.AnalyzeAsync(input)  ← FakeGapAnalyzer or OpenAiGapAnalyzer
   4. INSERT gap_analysis_skills (skill_id fuzzy by name)
   5. status='completed' + summary JSON
   6. if version > 1 → MarkRoadmapsOutdatedAsync (set is_outdated=true on active)
```

### 4.3 CV upload (Path A)

```
[FE] POST /assessment-paths/:pathId/cv (multipart/form-data, max 5MB, .pdf)
        │
        ▼
[UploadCvCommandHandler]
   1. Validate size + extension + content-type
   2. Load assessment_paths → must be PathType.Cv
   3. If existing CV → DELETE blob + DELETE row (hard-replace)
   4. _fileStorage.SaveAsync (LocalFileStorage → wwwroot/uploads/cv/<guid>.pdf)
   5. isReupload = exists completed gap for this JD?
   6. INSERT cv_submissions (parse_status='pending')
   7. _cvParseQueue.Enqueue(cv.Id, isReupload)
   8. Return 202
        │
        ▼ async
[CvParseJob.RunAsync]
   1. status='processing'
   2. _fileStorage.OpenReadAsync(fileUrl)
   3. _pdfExtractor.Extract(stream)  → PdfPig
   4. _anonymizer.Mask(text)         → [EMAIL] [PHONE] [URL]
   5. _cvParser.ParseAsync(masked)   → FakeCvParser keyword detection
   6. parsed_text + parsed_skills JSONB + status='completed'
   7. If isReupload: mark roadmaps outdated + EnqueueGapRerun (FR3.5)
```

---

## 5. Configuration Map

[`Edu-Nexus.APIs/appsettings.json`](Edu-Nexus.APIs/appsettings.json):

| Key                                   | Mục đích                                          | Trạng thái |
| ------------------------------------- | ------------------------------------------------- | ---------- |
| `ConnectionStrings:DefaultConnection` | Postgres connection                               | ✓ default `localhost:5432;...;Password=12345`. Đổi sang `5434` + `edunexus123` nếu dùng docker-compose. |
| `ConnectionStrings:Redis`             | Redis cache — package ref nhưng **chưa wire**     | ✓ declared `localhost:6380`. Không có `AddStackExchangeRedisCache` call trong DI → key không có tác dụng ở Sprint 1. |
| `Jwt:Key/Issuer/Audience/...`         | JWT HS256                                          | ✓          |
| `Google:ClientId`                     | Google OAuth audience                              | ✓          |
| `Ai:Enabled`                          | Switch `IGapAnalyzer` Fake ↔ OpenAi (default `false`) | ✓ default `false` — Fake* parsers chạy đầy đủ Sprint 1. CHỈ switch 1 binding (`IGapAnalyzer`); 3 parser còn lại luôn dùng Fake. |
| `OpenAI:ApiKey`                       | API key (CHỈ override qua user-secrets — không commit) | ✓ default rỗng. `dotnet user-secrets set "OpenAI:ApiKey" "sk-..."`. Khi `Ai:Enabled=true` mà ApiKey rỗng → `OpenAiGapAnalyzer` constructor throw. |
| `OpenAI:Models:Fast / Smart`          | Model ID (`gpt-4o-mini` / `gpt-4o`). `OpenAiGapAnalyzer` đọc `Smart` | ✓          |
| `OpenAI:Embedding`                    | Embedding model (`text-embedding-3-small`, 1536d) — **chưa có service nào đọc key này** | ✓ declared, Sprint 2 sẽ dùng |
| `OpenAI:MaxTokens:*`                  | Per-pipeline token cap. Hiện chỉ `OpenAiGapAnalyzer` đọc `GapAnalysis` key | ✓ |
| `Rag:ChunkSize / ChunkOverlap / MinSimilarityScore / TopK` | RAG retrieval params (RAG-Config §2) — **chưa có service nào đọc** | ✓ declared, Sprint 2 sẽ dùng |

---

## 6. Database Bootstrapping

Codebase **không** tự migrate database. Trình tự:

1. `docker compose up -d` (đã có `BE/docker-compose.yml` — pgvector/pg16 + Redis).
2. Chạy SQL từ [`API-DB/V4/DATABASE-Phase1-v4.1.md`](../API-DB/V4/DATABASE-Phase1-v4.1.md) (toàn bộ DDL, triggers, seeds).
3. Đặc biệt phải có **triggers** mà code BE đang phụ thuộc:
   * `trg_users_create_free_sub` — tự tạo Free subscription khi INSERT user. Nếu thiếu → `GET /users/me`, quota check, ... sẽ fail/sai.
   * `trg_*_updated_at` — auto update `updated_at`.
   * `trg_node_status_change` → recalc `roadmaps.progress_percent` (sẽ cần khi Sprint 2 implement roadmap).
4. Verify (`SELECT extname FROM pg_extension WHERE extname IN ('uuid-ossp','vector','pg_trgm')` → 3 rows).

> Không có folder `Migrations/` trong `Edu-Nexus.Infrastructure` — DbContext được generate (scaffold) từ DB sẵn có chứ không driven from code. Đừng tự ý `dotnet ef migrations add` vì sẽ lệch.

---

## 7. Folder-by-folder map

| Path                                                    | Vai trò                                                                       |
| ------------------------------------------------------- | ----------------------------------------------------------------------------- |
| [Edu-Nexus.Domain/Entities/](Edu-Nexus.Domain/Entities/)                    | 33 POCO entities (`User`, `JdSubmission`, `RagChunk` …)                       |
| [Edu-Nexus.Domain/Enums/](Edu-Nexus.Domain/Enums/)                          | 16 sub-folders, mỗi cái 1 enum                                                |
| [Edu-Nexus.Application/DTOs/](Edu-Nexus.Application/DTOs/)                  | Request/Response records                                                       |
| [Edu-Nexus.Application/Features/](Edu-Nexus.Application/Features/)          | 7 aggregates × {Commands, Queries} = các MediatR handler                       |
| [Edu-Nexus.Application/Interfaces/](Edu-Nexus.Application/Interfaces/)      | Contracts cho Infrastructure                                                   |
| [Edu-Nexus.Infrastructure/Data/](Edu-Nexus.Infrastructure/Data/)            | `EduNexusDbContext`, `Repository`, `UnitOfWork`                                |
| [Edu-Nexus.Infrastructure/BackgroundJobs/](Edu-Nexus.Infrastructure/BackgroundJobs/) | Hangfire-backed queue implementations                                  |
| [Edu-Nexus.Infrastructure/Jobs/](Edu-Nexus.Infrastructure/Jobs/)            | 4 jobs: JdParse, CvParse, AssessmentGenerate, GapAnalysis                      |
| [Edu-Nexus.Infrastructure/Parsing/](Edu-Nexus.Infrastructure/Parsing/)      | 4 Fake heuristic parsers (`FakeJdParser`/`FakeCvParser`/`FakeAssessmentQuestionGenerator`/`FakeGapAnalyzer`) + `OpenAiGapAnalyzer` (LLM) + `PdfPigTextExtractor` + `RegexAnonymizer` |
| [Edu-Nexus.Infrastructure/Security/](Edu-Nexus.Infrastructure/Security/)    | JWT, BCrypt, Google OAuth, CurrentUser                                         |
| [Edu-Nexus.Infrastructure/Storage/](Edu-Nexus.Infrastructure/Storage/)      | `LocalFileStorage` (lưu file vào `wwwroot/`)                                   |
| [Edu-Nexus.APIs/Controllers/](Edu-Nexus.APIs/Controllers/)                  | 8 controllers (Sprint 1 + S2.1 Gap)                                            |
| [Edu-Nexus.APIs/Extensions/](Edu-Nexus.APIs/Extensions/)                    | `AddJwtAuth`, `AddSwaggerWithJwt`                                              |
| [Edu-Nexus.APIs/Program.cs](Edu-Nexus.APIs/Program.cs)                      | App composition + middleware                                                   |

---

## 8. Đối chiếu với spec — Trạng thái hiện tại

**Sprint 1 endpoints (API-Specification-Phase1-v2):** ✅ **đã implement đầy đủ** (Auth, Onboarding, JD CRUD, AssessmentPath, CV upload, AssessmentSession, Reusable sessions). Bonus: Sprint 2.1 Gap Analysis cũng đã có endpoint + job.

**RAG / Sprint 2+ foundation (RAG-Config-Phase1-v4):** ❌ **chưa có**. Cụ thể chưa có:
* SK kernel toàn cục (`AddKernel`/`AddOpenAIChatCompletion`/`AddOpenAITextEmbeddingGeneration`).
* Folder `Infrastructure/Ai/` cùng `ILlmService`/`IEmbeddingService`/`IRagService`/`ISkillMatcherService`/`TextSplitter`.
* RAG ingestion (admin upload PDF → chunk → embed → insert `rag_chunks`).
* RAG retrieval (cosine-distance query pgvector).
* `rag_query_logs` chưa được INSERT bởi bất kỳ service nào.
* Redis cache (`AddStackExchangeRedisCache` không có).
* `IJdUrlFetcher` (HtmlAgilityPack-based) — `sourceType='url'` flow chưa hoàn thiện theo spec FR2.1.

Theo `sprint.md`, Sprint 1 **không yêu cầu** RAG. RAG ingestion thuộc Sprint 2 deliverable ("Admin upload tài liệu FPTU vào hệ thống RAG").

Xem [AGENT.md §4 Implementation status](AGENT.md#4-implementation-status) để biết chi tiết từng endpoint + per-component status.
