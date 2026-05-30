# AGENT.md — Edu-Nexus BE working guide

> Đọc file này trước khi sửa/thêm code trong [`BE/`](.). Mục đích: code đúng convention codebase, không lệch khỏi spec ở [`API-DB/V4/`](../API-DB/V4/).
>
> Tài liệu nền:
> * [ARCHITECTURE.md](ARCHITECTURE.md) — tổng quan layering, data flow
> * [API-DB/V4/API-Specification-Phase1-v2.md](../API-DB/V4/API-Specification-Phase1-v2.md) — contract API
> * [API-DB/V4/DATABASE-Phase1-v4.1.md](../API-DB/V4/DATABASE-Phase1-v4.1.md) — schema SQL nguồn-truth
> * [API-DB/V4/RAG-Config-Phase1-v4.md](../API-DB/V4/RAG-Config-Phase1-v4.md) — prompt + RAG pipeline
> * [API-DB/V4/sprint.md](../API-DB/V4/sprint.md) — phạm vi từng sprint
> * [REQUIREMENT-Edu-Nexus/REQUIREMENT-Edu-Nexus-VI.md](../REQUIREMENT-Edu-Nexus/REQUIREMENT-Edu-Nexus-VI.md) — yêu cầu nghiệp vụ

---

## 1. Stack

| Layer            | Tech                                                                       |
| ---------------- | -------------------------------------------------------------------------- |
| Framework        | ASP.NET Core, **`net10.0`**                                                |
| DB               | PostgreSQL 16 + `pgvector` + `pg_trgm` + `uuid-ossp` (Docker)               |
| ORM              | EF Core 9 (`Npgsql.EntityFrameworkCore.PostgreSQL`) + Pgvector EFCore       |
| Background Jobs  | Hangfire 1.8 + Hangfire.PostgreSql                                          |
| Mediator         | MediatR (CQRS-lite)                                                         |
| Auth             | JWT HS256, refresh hashed SHA-256, Google OAuth (`Google.Apis.Auth`)        |
| Password hashing | `BCrypt.Net-Next`                                                           |
| AI               | `Microsoft.SemanticKernel` 1.76 — **chỉ dùng cục bộ trong [`OpenAiGapAnalyzer`](Edu-Nexus.Infrastructure/Parsing/OpenAiGapAnalyzer.cs)** (tự `new OpenAIChatCompletionService(...)`). KHÔNG có `AddKernel`/registration toàn cục. |
| PDF              | `PdfPig`                                                                    |
| HTML scrape      | `HtmlAgilityPack` *(package ref — KHÔNG có service nào sử dụng)*            |
| Cache            | Redis *(docker-compose container chạy + package `Microsoft.Extensions.Caching.StackExchangeRedis` ref — KHÔNG có `AddStackExchangeRedisCache` call)* |

---

## 2. Code conventions — Phải tuân thủ

### 2.1 Layering
* `Domain` không reference EF/Hangfire.
* `Application` chỉ reference `Domain`. Không import `Microsoft.EntityFrameworkCore` ở Application.
* `Infrastructure` implement mọi `Interfaces/*` từ Application.
* `APIs` không gọi `IUnitOfWork` trực tiếp — chỉ qua `IMediator`.

### 2.2 Naming
* Entity = bảng PascalCase singular (`JdSubmission`, không `JdSubmissions`).
* Enum thư mục plural theo aggregate (`Enums/JdSubmissions/ParseStatus.cs`).
* MediatR: `<Verb><Aggregate>Command` / `Get<…>Query`.
* DTO suffix: `…Request` / `…Dto` / `…Data`. Tất cả là `record`.

### 2.3 Một file = một command/query
Pattern bắt buộc khi thêm mới feature:

```csharp
// Edu-Nexus.Application/Features/<Aggregate>/Commands/XxxCommand.cs
public record XxxCommand(<request type> Request) : IRequest<<response dto>>;

public class XxxCommandHandler : IRequestHandler<XxxCommand, <response dto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    // ... constructor inject

    public async Task<...> Handle(XxxCommand request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");
        // ...
    }
}
```

### 2.4 Error signalling — KHÔNG tự thiết kế lại

* Handler luôn ném `Exception` với message format `"<HTTP> <CODE>"`.
* Quota: `"403 QUOTA_EXCEEDED|<quotaType>|<current>|<limit>"`.
* Controller bắt từng case tường minh:

```csharp
catch (Exception ex) when (ex.Message == "404 JD_NOT_FOUND")
{
    return NotFound(new { error = new { code = "JD_NOT_FOUND", message = "..." } });
}
```

* **Không** introduce global exception middleware trừ khi user yêu cầu — codebase hiện đang phụ thuộc per-action catch và đổi sẽ làm vỡ behaviour.
* Tin nhắn user-facing dùng tiếng Việt (đối chiếu các controller hiện có).

### 2.5 Repository / UnitOfWork

* Inject `IUnitOfWork`, không bao giờ inject `EduNexusDbContext` vào handler.
* Truy cập child entity qua `includeProperties` CSV:

```csharp
await _unitOfWork.AssessmentPaths.FirstOrDefaultAsync(
    p => p.Id == pathId && p.UserId == userId,
    includeProperties: nameof(AssessmentPath.Jd),
    cancellationToken: ct);
```

* `Repository.FindAsync` returns `IEnumerable<T>` đã materialize → pagination/count đang in-memory. Đây là tradeoff hiện tại. **Đừng refactor sang `IQueryable` trừ khi user yêu cầu.**
* `Add/Update/Remove` không async; phải gọi `_unitOfWork.SaveChangesAsync(ct)` để commit. Multiple changes trong cùng 1 handler → 1 lần save (transaction implicit).
* Khi cần update entity đã load qua `FirstOrDefaultAsync` thì có thể chỉ sửa property và save (EF tracking). Nhưng codebase hiện vẫn gọi `Update()` để explicit — giữ pattern này.

### 2.6 Soft delete

* Mọi query `Users`, `JdSubmissions` bắt buộc filter `... && u.DeletedAt == null`.
* Khi delete soft: set `DeletedAt = DateTime.UtcNow` + `Update + SaveChangesAsync`.

### 2.7 Async pipeline pattern

Khi thêm endpoint trigger LLM/async task:

1. Handler INSERT row với `status = pending` (hoặc `generating` cho roadmap), gọi `_queue.Enqueue(id)`.
2. Trả **`202 Accepted`** với DTO chứa `id` + `status`.
3. Job class re-fetch entity, set `status = processing`, gọi service, ghi kết quả + child rows, set `status = completed`.
4. Catch all → set `status = failed`, ghi `error/parse_error`, `throw` để Hangfire retry.

### 2.8 Quota enforcement

Trước khi insert entity tạo mới (JD/Gap/Assessment path/Career track/Roadmap/Cert/Project):

* Load `UserSubscription` active + include `Tier`.
* Đọc `*Quota` của tier. `-1` = unlimited → bypass.
* Đếm theo rules ở [DATABASE-Phase1-v4.1.md §19](../API-DB/V4/DATABASE-Phase1-v4.1.md) (Option A):
  * **JD**: count `WHERE deleted_at IS NULL` (active).
  * **Gap Analysis**: count distinct JD đã có completed gap.
  * **Assessment (Path B)**: count `AssessmentPaths WHERE PathType = Assessment`.
  * **Roadmap**: count `WHERE Status = Active`.
  * Re-run/retake KHÔNG trừ.
* Vượt → throw `"403 QUOTA_EXCEEDED|<type>|<used>|<limit>"`.

Tham chiếu pattern mẫu: [`SubmitJdCommand.EnforceJdQuotaAsync`](Edu-Nexus.Application/Features/JdSubmissions/Commands/SubmitJdCommand.cs).

### 2.9 Versioning state (Gap, Roadmap, AssessmentSession)

* **Gap Analysis** rerun: flip `IsLatest=false` cho cái cũ, INSERT new `Version=max+1, IsLatest=true`. Unique partial index `idx_gap_jd_latest` enforce.
* **AssessmentSession** retake: flip `IsCurrent=false` cho cái cũ, INSERT new `IsCurrent=true`. Unique partial index `idx_assessment_sessions_current`.
* **Roadmap** (Sprint 2): khi regenerate → archive cái cũ (`Status=Archived`), INSERT new `Status=Generating` → khi xong `Status=Active`. Unique index `idx_roadmaps_jd_active` filter `status IN ('active','generating')`.

### 2.10 FR3.5 — Auto re-trigger Gap khi retake/re-upload

Đã implement ở 2 nơi, GIỮ pattern:
* [SubmitAssessmentSessionCommand.BuildAutoTriggeredAsync](Edu-Nexus.Application/Features/AssessmentSessions/Commands/SubmitAssessmentSessionCommand.cs) — sau khi submit assessment retake, nếu **đã** có completed gap cho JD → tạo gap mới (version++) + enqueue.
* [CvParseJob.EnqueueGapRerunAsync](Edu-Nexus.Infrastructure/Jobs/CvParseJob.cs) — sau khi CV parse xong và là `isReupload` → cùng logic.
* [GapAnalysisJob](Edu-Nexus.Infrastructure/Jobs/GapAnalysisJob.cs) khi `Version > 1` → mark active roadmaps `IsOutdated = true`. **Không** auto-regenerate roadmap (đúng spec — user chủ động).

### 2.11 DI registration

Mọi service mới phải đăng ký trong [`Infrastructure/DependencyInjection.cs`](Edu-Nexus.Infrastructure/DependencyInjection.cs). 4 method group hiện có: `AddPersistence`, `AddSecurity`, `AddBackgroundJobs`, `AddParsing`. Khi thêm group mới (vd: RAG), tạo extension method riêng và gọi từ `AddInfrastructure`.

* Stateless utility (PDF extractor, anonymizer, file storage) → `AddSingleton`.
* Có state hoặc dùng DbContext / Scoped service → `AddScoped`.
* Parser có Fake + Real → đăng ký cả 2 concrete, rồi `services.AddScoped<I…>(sp => aiEnabled ? sp.GetRequiredService<OpenAi…>() : sp.GetRequiredService<Fake…>())`.

### 2.12 Response envelope

Controller luôn return:
* Success: `Ok(new { data = result })`, `StatusCode(201, new { data = ... })`, `StatusCode(202, new { data = ... })`, `NoContent()`.
* Paged: `new { data = items, pagination = paginationDto }`.
* Error: `new { error = new { code, message, ...extra } }`.

---

## 3. Error codes (đang dùng)

| Mã string ném từ handler                              | HTTP | Mục đích                                       |
| ---------------------------------------------------- | ---- | ---------------------------------------------- |
| `401 UNAUTHORIZED`                                   | 401  | Thiếu JWT / token sai                          |
| `401 INVALID_CREDENTIALS`                            | 401  | Sai email/password                             |
| `401 INVALID_GOOGLE_TOKEN`                           | 401  | Google ID token verify fail                    |
| `401 INVALID_TOKEN`                                  | 401  | Refresh token sai                              |
| `403 ACCOUNT_BANNED`                                 | 403  | `is_banned=true`                               |
| `403 QUOTA_EXCEEDED\|<type>\|<used>\|<limit>`        | 403  | Vượt quota                                     |
| `404 USER_NOT_FOUND` / `404 JD_NOT_FOUND` / `404 *`  | 404  | Entity không tồn tại hoặc không own            |
| `409 EMAIL_EXISTS` / `409 SLUG_TAKEN`                | 409  | Conflict unique                                |
| `409 PATH_ALREADY_EXISTS` / `409 ALREADY_COMPLETED` / `409 ALREADY_SUBMITTED` | 409 | Conflict state |
| `422 ONBOARDING_REQUIRED`                            | 422  | Chưa làm survey                                |
| `422 INVALID_*` / `422 *_REQUIRED` / `422 *_MISMATCH`| 422  | Validation                                     |
| `422 CANNOT_RESET_AFTER_GAP`                         | 422  | Reset path sau khi gap đã chạy                 |
| `422 CV_NOT_READY` / `422 JD_NOT_PARSED` / ...       | 422  | Pre-condition chưa thoả                        |

Khi thêm error mới: tuân thủ format, thêm `catch` tương ứng trong controller, không silent.

---

## 4. Implementation status

### 4.1 Sprint 1 — endpoints (đối chiếu [API-Specification-Phase1-v2.md §SPRINT 1](../API-DB/V4/API-Specification-Phase1-v2.md))

| Endpoint                                                   | Status   | File                                                                                                                                            |
| ---------------------------------------------------------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `POST /auth/register`                                      | ✅        | [AuthController](Edu-Nexus.APIs/Controllers/AuthController.cs) / [RegisterCommand](Edu-Nexus.Application/Features/Auth/Commands/RegisterCommand.cs) |
| `POST /auth/login`                                         | ✅        | [LoginQuery](Edu-Nexus.Application/Features/Auth/Queries/LoginQuery.cs)                                                                          |
| `POST /auth/google`                                        | ✅        | [GoogleLoginCommand](Edu-Nexus.Application/Features/Auth/Commands/GoogleLoginCommand.cs)                                                         |
| `POST /auth/refresh`                                       | ✅        | [RefreshTokenCommand](Edu-Nexus.Application/Features/Auth/Commands/RefreshTokenCommand.cs)                                                       |
| `POST /auth/logout`                                        | ✅        | [LogoutCommand](Edu-Nexus.Application/Features/Auth/Commands/LogoutCommand.cs)                                                                   |
| `GET /users/me`                                            | ✅        | [GetCurrentUserQuery](Edu-Nexus.Application/Features/Auth/Queries/GetCurrentUserQuery.cs)                                                        |
| `PUT /users/me`                                            | ✅        | [UpdateCurrentUserCommand](Edu-Nexus.Application/Features/Auth/Commands/UpdateCurrentUserCommand.cs)                                             |
| `GET/POST/PUT /onboarding`                                 | ✅        | [Onboarding folder](Edu-Nexus.Application/Features/Onboarding/)                                                                                  |
| `POST/GET/GET:id/DELETE /jd-submissions`                   | ✅        | [JdSubmissions folder](Edu-Nexus.Application/Features/JdSubmissions/) — **lưu ý:** xem §4.3 issue về URL fetcher                                |
| `POST/DELETE /jd-submissions/:jdId/assessment-path`        | ✅        | [AssessmentPaths folder](Edu-Nexus.Application/Features/AssessmentPaths/)                                                                        |
| `POST/GET /assessment-paths/:pathId/cv`                    | ✅        | [CvSubmissions folder](Edu-Nexus.Application/Features/CvSubmissions/)                                                                            |
| `POST /assessment-paths/:pathId/sessions`                  | ✅        | [StartAssessmentSessionCommand](Edu-Nexus.Application/Features/AssessmentSessions/Commands/StartAssessmentSessionCommand.cs)                     |
| `GET /assessment-sessions/:sessionId/questions`            | ✅        | [GetSessionQuestionsQuery](Edu-Nexus.Application/Features/AssessmentSessions/Queries/GetSessionQuestionsQuery.cs)                                |
| `POST /assessment-sessions/:sessionId/submit`              | ✅        | [SubmitAssessmentSessionCommand](Edu-Nexus.Application/Features/AssessmentSessions/Commands/SubmitAssessmentSessionCommand.cs)                   |
| `GET /assessment-sessions/:sessionId`                      | ✅        | [GetSessionResultQuery](Edu-Nexus.Application/Features/AssessmentSessions/Queries/GetSessionResultQuery.cs)                                      |
| `GET /jd-submissions/:jdId/reusable-sessions`              | ✅        | [GetReusableSessionsQuery](Edu-Nexus.Application/Features/AssessmentSessions/Queries/GetReusableSessionsQuery.cs)                                |

**Bonus đã implement trước (Sprint 2 — S2.1):** `POST /jd-submissions/:jdId/gap-analysis`, `GET /jd-submissions/:jdId/gap-analysis` (xem [GapAnalysisController](Edu-Nexus.APIs/Controllers/GapAnalysisController.cs)).

> Tổng kết: **toàn bộ deliverable Sprint 1 đã có endpoint + handler + Hangfire pipeline**.

### 4.2 Database / EF

| Item                                                                | Trạng thái                                                                                                                                                                                                |
| ------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 33 entities / 33 tables                                             | ✅ Đầy đủ. DbContext map column name + enum conversion khớp `DATABASE-Phase1-v4.1.md`.                                                                                                                     |
| 3 extensions (`uuid-ossp`, `vector`, `pg_trgm`)                     | ✅ khai báo qua `HasPostgresExtension(...)` + `o.UseVector()` trong `AddDbContext`.                                                                                                                        |
| pgvector `VECTOR(1536)` + HNSW index                                | ✅ ([EduNexusDbContext §RagChunk](Edu-Nexus.Infrastructure/Data/EduNexusDbContext.cs)).                                                                                                                     |
| Partial unique indexes (roadmaps active+generating, assessment is_current, user_subs active, gap is_latest) | ✅ Trong DbContext.                                                                                                                                                          |
| Soft-delete index `idx_jd_user WHERE deleted_at IS NULL`            | ✅                                                                                                                                                                                                         |
| EF Migrations folder                                                | ✗ **Không có**. Code không sinh migration — phải bootstrap DB bằng cách chạy file SQL `DATABASE-Phase1-v4.1.md` trên Postgres (port 5434, docker-compose đã setup).                                       |
| Triggers (`trg_users_create_free_sub`, `trg_set_updated_at`, `trg_update_roadmap_progress`, `trg_cleanup_skill_in_rag_docs`) | ✗ Code BE phụ thuộc trigger DB. Phải chạy SQL trigger SAU khi tạo schema. Nếu thiếu trigger Free subscription → handler register sẽ tạo user **mà không có** subscription → quota check fallback default `3`. |
| Seed data (`subscription_tiers`, `skills`, admin user)              | ✗ Phải seed bằng SQL theo `DATABASE-Phase1-v4.1.md §17`.                                                                                                                                                   |

> **Tóm gọn:** kết nối + map schema đúng. Nhưng codebase **không tự khởi tạo DB**. Phải chạy file SQL spec trước. Đặc biệt nhớ trigger auto-create Free subscription.

### 4.3 RAG / AI — Trạng thái thực tế Sprint 1

> **Tóm gọn:** chỉ có DB schema cho RAG + 1 LLM-backed parser standalone (`OpenAiGapAnalyzer`). **KHÔNG có RAG retrieval pipeline, KHÔNG có embedding service, KHÔNG có Semantic Kernel registration toàn cục, KHÔNG có Redis cache wire-up.** Sprint 1 theo `sprint.md` chỉ cần MVP với Fake parsers — đúng theo yêu cầu BE dev: "Sprint 1 chưa cần RAG".

| Item                                                                                       | Trạng thái |
| ------------------------------------------------------------------------------------------ | ---------- |
| Tables `rag_documents`, `rag_chunks`, `rag_query_logs` mapped trong DbContext              | ✅ — có mapping (pgvector `VECTOR(1536)`, HNSW index `vector_cosine_ops`), nhưng **code không INSERT/SELECT từ các bảng này**. |
| `FakeJdParser` / `FakeCvParser` / `FakeAssessmentQuestionGenerator` / `FakeGapAnalyzer`     | ✅ — heuristic fallback, đủ chạy được Sprint 1 không cần OpenAI key. |
| `OpenAiGapAnalyzer` (LLM via Semantic Kernel)                                              | ✅ — switch bằng `Ai:Enabled`. **Standalone**: tự `new OpenAIChatCompletionService(model, apiKey)` từ `IConfiguration`, không qua kernel/registration chung. Không log vào `rag_query_logs`. Không dùng RAG context. |
| `OpenAiJdParser` / `OpenAiCvParser` / `OpenAiAssessmentQuestionGenerator`                   | ✗ **KHÔNG có**. Chỉ có `FakeJdParser` / `FakeCvParser` / `FakeAssessmentQuestionGenerator`. |
| `PdfPigTextExtractor` + `RegexAnonymizer`                                                  | ✅ |
| `LocalFileStorage` (lưu PDF vào `wwwroot/uploads/cv`)                                       | ✅ |
| **Semantic Kernel registration toàn cục** (`AddKernel`, `AddOpenAIChatCompletion("fast"/"smart")`, `AddOpenAITextEmbeddingGeneration`) theo RAG-Config §4 | ✗ **KHÔNG có**. Không có `AddSemanticKernel` extension method nào trong `Infrastructure/DependencyInjection.cs`. SK chỉ được dùng cục bộ trong `OpenAiGapAnalyzer`. |
| `ILlmService` / `IEmbeddingService` / `IRagService` / `ISkillMatcherService` interfaces    | ✗ **KHÔNG có**. Folder `Edu-Nexus.Infrastructure/Ai/` không tồn tại. |
| RAG retrieval pipeline (embed → similarity search trên `rag_chunks`)                       | ✗ **KHÔNG có**. Sprint 2 mới bắt đầu (admin upload FPTU PDF → embedding → store). |
| `TextSplitter` (chunking helper cho RAG ingestion)                                         | ✗ **KHÔNG có**. |
| `IJdUrlFetcher` (fetch HTML JD, strip tag bằng HtmlAgilityPack)                            | ✗ **KHÔNG có**. `HtmlAgilityPack` package ref trong csproj nhưng không có file `HttpJdUrlFetcher.cs` hay tương đương. |
| Redis cache (`AddStackExchangeRedisCache`)                                                 | ✗ **KHÔNG được register** dù package `Microsoft.Extensions.Caching.StackExchangeRedis` đã ref và `ConnectionStrings:Redis` có sẵn trong appsettings. |
| `appsettings.json` keys cho `OpenAI:*`, `Rag:*`, `ConnectionStrings:Redis`                  | ✅ — declared (sẵn sàng để Sprint 2 wire). `OpenAI:ApiKey` để rỗng → user-secrets override. |

### 4.4 Other notable gaps so với spec

* **JD URL flow (FR2.1):** Spec yêu cầu khi `sourceType='url'`, BE tự fetch URL → fill `raw_content`. Hiện tại [SubmitJdCommand.ValidateContent](Edu-Nexus.Application/Features/JdSubmissions/Commands/SubmitJdCommand.cs) **bắt buộc `rawContent`** kể cả khi `sourceType='url'`. Chưa có URL fetcher service → FE phải tự fetch + paste content trước khi submit.
* **Portfolio slug auto-gen (API spec PUT /users/me):** Spec "auto-generate từ fullName khi register". Có helper [`SlugHelper`](Edu-Nexus.Application/Helpers/SlugHelper.cs) — cần verify đã wire vào `RegisterCommand` / `UpdateCurrentUserCommand` chưa.
* **Refresh token có thể xoay vòng?** [RefreshTokenCommand](Edu-Nexus.Application/Features/Auth/Commands/RefreshTokenCommand.cs) — verify logic revoke + issue mới có set `revoked_at` không.
* **Global exception handling:** không có middleware. Mỗi controller bắt 5-7 catch theo từng error string. Đây là convention hiện tại — KHÔNG được phép đổi nếu user không yêu cầu.
* **OpenAiGapAnalyzer standalone:** tự `new OpenAIChatCompletionService(...)` từ `IConfiguration`, không qua DI/SK kernel chung, không log vào `rag_query_logs`, không dùng RAG context. Sprint 2 sẽ cần refactor khi build RAG infrastructure (`ILlmService`/`IEmbeddingService`/`IRagService` chưa có).

---

## 5. Bootstrap & Run

1. **Database & Redis (Docker):**

   ```powershell
   cd BE
   docker compose up -d
   # Postgres: localhost:5434, DB=edu_nexus, user=postgres, pwd=edunexus123 (theo docker-compose)
   # Redis:    localhost:6380
   ```

   > **Lưu ý:** `appsettings.json` hiện đặt `Host=localhost;Port=5432;...;Password=12345` — đang trỏ vào local Postgres của dev, không phải container. Đổi connection string nếu dùng docker-compose.

2. **Apply schema:** chạy SQL trong [`API-DB/V4/DATABASE-Phase1-v4.1.md`](../API-DB/V4/DATABASE-Phase1-v4.1.md) qua `psql` / pgAdmin / DBeaver. Bắt buộc seed `subscription_tiers` ('free' + 'student') TRƯỚC khi register user (trigger `trg_create_free_subscription` đòi).

3. **Default config (`Ai:Enabled=false`):** App chạy ngay không cần OpenAI key. ILlmService/IEmbeddingService/IRagService bind NoOp* → JD/CV/Assessment dùng Fake* parsers như Sprint 1. Đủ chạy được toàn bộ flow.

4. **Bật AI thật (sau khi có OpenAI key):**

   ```powershell
   cd BE/Edu-Nexus.APIs
   dotnet user-secrets init                     # nếu chưa init
   dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
   dotnet user-secrets set "Ai:Enabled" "true"
   ```

   Khi `Ai:Enabled=true`, **CHỈ** thay đổi 1 binding:
   * `IGapAnalyzer` → `OpenAiGapAnalyzer` (thay vì `FakeGapAnalyzer`). `OpenAiGapAnalyzer` tự khởi tạo `OpenAIChatCompletionService` từ `OpenAI:ApiKey` + `OpenAI:Models:Smart`.
   * `IJdParser`, `ICvParser`, `IAssessmentQuestionGenerator` **VẪN dùng Fake*** — chưa có OpenAI variant.
   * Không có SK Kernel toàn cục, không có embedding service, không có RAG service. Sprint 2 sẽ build các thứ này.

5. **Run:**

   ```powershell
   cd BE/Edu-Nexus.APIs
   dotnet run
   ```

6. **Swagger:** root `/` (RoutePrefix rỗng).
   **Hangfire dashboard:** `/hangfire` (Dev only).

7. **Sau scaffold lại DbContext** (DB v5+): `dotnet ef dbcontext scaffold ... --force` từ thư mục `Edu-Nexus.Infrastructure` với connection string đến DB đã apply schema mới. Không dùng `migrations add`.

---

## 6. Khi thêm endpoint mới — checklist

1. Tìm trong [API-Specification-Phase1-v2.md](../API-DB/V4/API-Specification-Phase1-v2.md) endpoint cần làm → ghi nhớ method/path, request/response shape, error codes, RAG tag (nếu có).
2. **DTO** trong `Edu-Nexus.Application/DTOs/<Aggregate>DTOs.cs` (record).
3. **Command/Query handler** trong `Features/<Aggregate>/{Commands,Queries}/`. Tuân thủ pattern §2.3 + §2.4.
4. Nếu cần entity mới → check [DATABASE-Phase1-v4.1.md](../API-DB/V4/DATABASE-Phase1-v4.1.md), thêm POCO trong `Domain/Entities/`, mapping trong `EduNexusDbContext.OnModelCreating`, thêm `IRepository<T>` vào `IUnitOfWork`+`UnitOfWork`.
5. Nếu là pipeline LLM:
   * **Heuristic / Fake parser:** interface trong `Application/Interfaces/Parsing/` (DTO record cùng file), `Fake*` impl trong `Infrastructure/Parsing/`. Register cả `Fake*` và (nếu có) `OpenAi*` concrete trong `AddParsing`, rồi `services.AddScoped<I…Parser>(sp => aiEnabled ? sp.GetRequiredService<OpenAi…>() : sp.GetRequiredService<Fake…>())`.
   * **Real LLM parser (chỉ `OpenAiGapAnalyzer` đang có làm mẫu):** inject `IConfiguration` + `ILogger`, tự `new OpenAIChatCompletionService(model, apiKey)`, build prompt (system + user) → gọi `_chat.GetChatMessageContentAsync(history, settings, ct)` với `OpenAIPromptExecutionSettings { ResponseFormat = "json_object", Temperature = 0.2, MaxTokens = ... }` → parse JSON bằng `JsonDocument.Parse`. Đảm bảo defensive fallback khi LLM bỏ sót field (xem `OpenAiGapAnalyzer.ParseResponse`).
   * **RAG context (Sprint 2+):** hiện chưa có `IRagService`. Khi build, gọi RAG retrieval trước LLM, fold context vào system prompt. Sprint 1 prompt KHÔNG có RAG context.
   * Khi Deserialize JSON từ LLM: dùng `new JsonSerializerOptions { PropertyNameCaseInsensitive = true }` hoặc `JsonDocument.Parse` + manual `TryGetProperty`.
6. Nếu cần job nền → tạo `IXxxQueue` (Application) + `HangfireXxxQueue` (Infrastructure) + `XxxJob` (Infrastructure).
7. **Controller** route khớp spec: `[Route("...")]`, `[Authorize]` ngoại trừ `Public`. Try/catch theo error codes.
8. Quota check (nếu là tạo mới): pattern §2.8.
9. Swagger sẽ tự pick lên — bearer token đã configure sẵn.
10. Update [AGENT.md §4.1 status table](#41-sprint-1--endpoints-đối-chiếu-api-specification-phase1-v2md-sprint-1) nếu là endpoint Sprint 1/2/3 trong spec.

---

## 7. Anti-patterns / không được làm

* Không inject `EduNexusDbContext` ra ngoài `Infrastructure`.
* Không truy cập `HttpContext` trong handler (dùng `ICurrentUserService`).
* Không refactor `Repository`/`UnitOfWork` sang `IQueryable` chỉ vì performance khi user chưa yêu cầu.
* Không tự ý chạy `dotnet ef migrations add` — DbContext đang scaffold-from-DB, không phải code-first.
* Không silent error: mọi exception phải có error code rõ ràng để controller map.
* Không trả entity / DbSet thẳng cho controller. Luôn project sang DTO record trong handler.
* Không thêm endpoint Admin (Sprint 3) trước khi user yêu cầu — hiện tại chưa có Admin controller nào, đừng vội build.
* Không hard-code response tiếng Anh — codebase đang Vietnamese-first cho user-facing messages.
* Không bật `Ai:Enabled=true` mà chưa cung cấp `OpenAI:ApiKey` qua user-secrets; sẽ throw ngay khi DI resolve `OpenAiGapAnalyzer`.

---

## 8. Câu hỏi nhanh khi không chắc

| Tình huống                                       | Tham chiếu                                                                              |
| ------------------------------------------------ | --------------------------------------------------------------------------------------- |
| Schema field/index có đúng không?                | [DATABASE-Phase1-v4.1.md](../API-DB/V4/DATABASE-Phase1-v4.1.md) + DbContext mapping     |
| Endpoint trả gì?                                 | [API-Specification-Phase1-v2.md](../API-DB/V4/API-Specification-Phase1-v2.md)           |
| Logic AI / prompt LLM viết sao?                  | [RAG-Config-Phase1-v4.md](../API-DB/V4/RAG-Config-Phase1-v4.md) (§8.x prompts)          |
| Sprint hiện tại làm gì?                          | [sprint.md](../API-DB/V4/sprint.md) + [§4.1](#41-sprint-1--endpoints-đối-chiếu-api-specification-phase1-v2md-sprint-1) ở đây |
| Yêu cầu nghiệp vụ FRx.y nói gì?                  | [REQUIREMENT-Edu-Nexus-VI.md](../REQUIREMENT-Edu-Nexus/REQUIREMENT-Edu-Nexus-VI.md)     |
| Risk mitigation patch (validation gate, is_active filter…) | [Risk-Mitigation-Patch.md](../API-DB/V4/Risk-Mitigation-Patch.md)              |
