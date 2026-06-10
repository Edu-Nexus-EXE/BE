-- ============================================================================
-- Edu-Nexus — Database Schema Phase 1 (PostgreSQL)
-- Generated from: DATABASE-Phase1-v4.1.md
-- Target: PostgreSQL 15+ với pgvector, pg_trgm, uuid-ossp
-- ============================================================================
--
-- HƯỚNG DẪN CHẠY:
--   1) Đảm bảo dùng image `pgvector/pgvector:pg16` hoặc Postgres đã cài pgvector
--   2) psql -h localhost -p 5432 -U postgres -d edu_nexus -f DATABASE-Phase1-v4.1.sql
--   3) Verify bằng SQL ở cuối file (section 19).
--
-- Idempotent: dùng IF NOT EXISTS / OR REPLACE ở mọi nơi có thể.
-- Chạy lại trên DB đã có data sẽ KHÔNG xoá data (DDL bỏ qua nếu object đã tồn tại).
-- ============================================================================

-- ====================== 1. EXTENSIONS ======================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";       -- pgvector: VECTOR(1536) cho embeddings
CREATE EXTENSION IF NOT EXISTS "pg_trgm";      -- fuzzy match skill taxonomy

-- ====================== 2. SUBSCRIPTION TIERS (seed trước users) ======================
CREATE TABLE IF NOT EXISTS subscription_tiers (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tier_code                   VARCHAR(20) NOT NULL UNIQUE CHECK (tier_code IN ('free', 'student')),
    display_name                VARCHAR(50) NOT NULL,
    price_monthly               DECIMAL(10,2) NOT NULL DEFAULT 0,
    currency                    CHAR(3) NOT NULL DEFAULT 'VND',
    jd_quota                    INTEGER NOT NULL,
    gap_analysis_quota          INTEGER NOT NULL,
    assessment_quota            INTEGER NOT NULL,
    roadmap_active_quota        INTEGER NOT NULL,
    career_track_quota          INTEGER NOT NULL,
    portfolio_certificate_quota INTEGER NOT NULL,
    portfolio_project_quota     INTEGER NOT NULL,
    full_gap_history            BOOLEAN NOT NULL DEFAULT FALSE,
    is_active                   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ====================== 3. SKILLS TAXONOMY ======================
CREATE TABLE IF NOT EXISTS skills (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name             VARCHAR(150) NOT NULL,
    slug             VARCHAR(150) NOT NULL UNIQUE,
    category         VARCHAR(50) NOT NULL,
    major            VARCHAR(50) NOT NULL,
    description      TEXT,
    difficulty_level SMALLINT NOT NULL DEFAULT 1 CHECK (difficulty_level BETWEEN 1 AND 5),
    is_active        BOOLEAN NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS skill_prerequisites (
    skill_id        UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    prerequisite_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    PRIMARY KEY (skill_id, prerequisite_id),
    CHECK (skill_id <> prerequisite_id)
);

CREATE INDEX IF NOT EXISTS idx_skills_name_trgm ON skills USING GIN (name gin_trgm_ops);

-- ====================== 4. USERS & AUTH ======================
CREATE TABLE IF NOT EXISTS users (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email               VARCHAR(255) NOT NULL UNIQUE,
    password_hash       VARCHAR(255),
    auth_provider       VARCHAR(20) NOT NULL DEFAULT 'email'
                            CHECK (auth_provider IN ('email', 'google')),
    google_sub          VARCHAR(255) UNIQUE,
    full_name           VARCHAR(255) NOT NULL,
    avatar_url          TEXT,
    role                VARCHAR(20) NOT NULL DEFAULT 'user' CHECK (role IN ('user', 'admin')),
    is_banned           BOOLEAN NOT NULL DEFAULT FALSE,
    is_survey_completed BOOLEAN NOT NULL DEFAULT FALSE,
    portfolio_url_slug  VARCHAR(100) UNIQUE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at          TIMESTAMPTZ,
    last_login_at       TIMESTAMPTZ,
    CHECK (
        (auth_provider = 'email' AND password_hash IS NOT NULL)
        OR (auth_provider = 'google' AND google_sub IS NOT NULL)
    )
);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_users_email          ON users (email) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user  ON refresh_tokens (user_id, revoked_at);

-- ====================== 5. ONBOARDING SURVEY (FR1) ======================
CREATE TABLE IF NOT EXISTS onboarding_responses (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id            UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    academic_year      VARCHAR(20) NOT NULL
        CHECK (academic_year IN ('Năm 1','Năm 2','Năm 3','Năm 4')),
    major              VARCHAR(50) NOT NULL
        CHECK (major IN ('IT','Marketing','Business','Khác')),
    primary_goal       VARCHAR(100) NOT NULL
        CHECK (primary_goal IN (
            'Khám phá hướng đi','Đã có target, cần lộ trình',
            'Improve 1 kỹ năng cụ thể','Build portfolio để xin việc')),
    weekly_study_hours VARCHAR(20) NOT NULL
        CHECK (weekly_study_hours IN ('< 5h','5-10h','10-20h','> 20h')),
    proficiency_level  VARCHAR(50) NOT NULL
        CHECK (proficiency_level IN (
            'Beginner','Đã học cơ bản','Có kinh nghiệm dự án','Đã đi thực tập')),
    learning_priority  VARCHAR(50) NOT NULL
        CHECK (learning_priority IN ('Hard skill','Soft skill','Certificate','Portfolio project')),
    learning_budget    VARCHAR(30) NOT NULL
        CHECK (learning_budget IN ('free','< 500k','500k-2tr','> 2tr')),
    preferred_channel  VARCHAR(50) NOT NULL
        CHECK (preferred_channel IN (
            'Video','Đọc tài liệu','Làm project thực tế','Học có mentor')),
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_onboarding_major ON onboarding_responses (major);

-- ====================== 6. JD SUBMISSIONS (Edu-Bridge) ======================
CREATE TABLE IF NOT EXISTS jd_submissions (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    source_type      VARCHAR(20) NOT NULL CHECK (source_type IN ('url','text')),
    source_url       TEXT,
    raw_content      TEXT,
    job_title        VARCHAR(255),
    job_role_category VARCHAR(100),
    seniority_level  VARCHAR(50),
    salary_min       INTEGER,
    salary_max       INTEGER,
    currency         CHAR(3),
    parse_status     VARCHAR(20) NOT NULL DEFAULT 'pending'
                         CHECK (parse_status IN ('pending','processing','completed','failed')),
    parse_error      TEXT,
    parsed_at        TIMESTAMPTZ,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at       TIMESTAMPTZ,
    CONSTRAINT chk_jd_completed_has_content
        CHECK (parse_status <> 'completed' OR raw_content IS NOT NULL),
    CONSTRAINT chk_salary_range
        CHECK (salary_min IS NULL OR salary_max IS NULL OR salary_min <= salary_max)
);

CREATE TABLE IF NOT EXISTS jd_skills (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    jd_id          UUID NOT NULL REFERENCES jd_submissions(id) ON DELETE CASCADE,
    skill_id       UUID REFERENCES skills(id) ON DELETE SET NULL,
    skill_name_raw VARCHAR(150) NOT NULL,
    skill_type     VARCHAR(20) NOT NULL CHECK (skill_type IN ('hard_skill','soft_skill')),
    is_mandatory   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_jd_user         ON jd_submissions (user_id, created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_jd_parse_status ON jd_submissions (parse_status) WHERE parse_status IN ('pending','processing','failed');
CREATE INDEX IF NOT EXISTS idx_jd_skills_jd    ON jd_skills (jd_id);
CREATE INDEX IF NOT EXISTS idx_jd_skills_skill ON jd_skills (skill_id) WHERE skill_id IS NOT NULL;

-- ====================== 7. ASSESSMENT PATH (FR2.3) ======================
CREATE TABLE IF NOT EXISTS assessment_paths (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    jd_id      UUID NOT NULL UNIQUE REFERENCES jd_submissions(id) ON DELETE CASCADE,
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    path_type  VARCHAR(20) NOT NULL CHECK (path_type IN ('cv','assessment')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Path A: CV upload — hard-replace khi re-upload (UNIQUE assessment_path_id)
CREATE TABLE IF NOT EXISTS cv_submissions (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_path_id UUID NOT NULL UNIQUE REFERENCES assessment_paths(id) ON DELETE CASCADE,
    user_id            UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    file_url           TEXT NOT NULL,
    file_name          VARCHAR(255),
    file_size_bytes    INTEGER,
    mime_type          VARCHAR(50),
    parsed_text        TEXT,
    parsed_skills      JSONB,
    parse_status       VARCHAR(20) NOT NULL DEFAULT 'pending'
                           CHECK (parse_status IN ('pending','processing','completed','failed')),
    parse_error        TEXT,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    parsed_at          TIMESTAMPTZ
);

-- Path B: Assessment — multiple sessions (retake), 1 current per path
CREATE TABLE IF NOT EXISTS assessment_sessions (
    id                         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_path_id         UUID NOT NULL REFERENCES assessment_paths(id) ON DELETE CASCADE,
    user_id                    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    job_role_category_snapshot VARCHAR(100) NOT NULL,
    part1_count                SMALLINT NOT NULL,
    part2_count                SMALLINT NOT NULL,
    skill_scores               JSONB,
    status                     VARCHAR(20) NOT NULL DEFAULT 'in_progress'
                                   CHECK (status IN ('in_progress','submitted','expired')),
    is_current                 BOOLEAN NOT NULL DEFAULT TRUE,
    reused_from_session_id     UUID REFERENCES assessment_sessions(id) ON DELETE SET NULL,
    started_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    submitted_at               TIMESTAMPTZ,
    created_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS assessment_questions (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id     UUID NOT NULL REFERENCES assessment_sessions(id) ON DELETE CASCADE,
    sequence_order SMALLINT NOT NULL,
    part           SMALLINT NOT NULL CHECK (part IN (1,2)),
    question_text  TEXT NOT NULL,
    options        JSONB NOT NULL,
    correct_option CHAR(1) NOT NULL CHECK (correct_option IN ('A','B','C','D')),
    related_skill  VARCHAR(150),
    explanation    TEXT
);

CREATE TABLE IF NOT EXISTS assessment_answers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id      UUID NOT NULL REFERENCES assessment_sessions(id) ON DELETE CASCADE,
    question_id     UUID NOT NULL REFERENCES assessment_questions(id) ON DELETE CASCADE,
    selected_option CHAR(1) NOT NULL CHECK (selected_option IN ('A','B','C','D')),
    is_correct      BOOLEAN NOT NULL,
    answered_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (session_id, question_id)
);

CREATE INDEX IF NOT EXISTS idx_assessment_paths_user ON assessment_paths (user_id);
CREATE INDEX IF NOT EXISTS idx_cv_user               ON cv_submissions (user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_sessions_path         ON assessment_sessions (assessment_path_id);
CREATE INDEX IF NOT EXISTS idx_sessions_reuse        ON assessment_sessions (user_id, job_role_category_snapshot)
    WHERE status = 'submitted' AND is_current = TRUE;
CREATE UNIQUE INDEX IF NOT EXISTS idx_assessment_sessions_current
    ON assessment_sessions (assessment_path_id) WHERE is_current = TRUE;
CREATE INDEX IF NOT EXISTS idx_questions_session     ON assessment_questions (session_id, sequence_order);

-- ====================== 8. GAP ANALYSIS ======================
CREATE TABLE IF NOT EXISTS gap_analyses (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id            UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    jd_id              UUID NOT NULL REFERENCES jd_submissions(id) ON DELETE CASCADE,
    assessment_path_id UUID NOT NULL REFERENCES assessment_paths(id) ON DELETE CASCADE,
    input_source       VARCHAR(20) NOT NULL CHECK (input_source IN ('cv','assessment')),
    version            SMALLINT NOT NULL DEFAULT 1,
    is_latest          BOOLEAN NOT NULL DEFAULT TRUE,
    summary            JSONB,
    status             VARCHAR(20) NOT NULL DEFAULT 'pending'
                           CHECK (status IN ('pending','processing','completed','failed')),
    error              TEXT,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at       TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS gap_analysis_skills (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    gap_analysis_id    UUID NOT NULL REFERENCES gap_analyses(id) ON DELETE CASCADE,
    skill_id           UUID REFERENCES skills(id) ON DELETE SET NULL,
    skill_name         VARCHAR(150) NOT NULL,
    gap_status         VARCHAR(20) NOT NULL CHECK (gap_status IN ('missing','have','needs_upgrade')),
    current_level      VARCHAR(20) CHECK (current_level IN ('none','basic','intermediate','advanced')),
    target_level       VARCHAR(20) NOT NULL CHECK (target_level IN ('basic','intermediate','advanced')),
    urgency_score      SMALLINT CHECK (urgency_score BETWEEN 1 AND 10),
    reasoning          TEXT,
    is_mandatory_in_jd BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS idx_gap_user             ON gap_analyses (user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_gap_jd               ON gap_analyses (jd_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_gap_jd_latest ON gap_analyses (jd_id) WHERE is_latest = TRUE;
CREATE INDEX IF NOT EXISTS idx_gap_skills_ga        ON gap_analysis_skills (gap_analysis_id);

-- ====================== 9. CAREER TRACKS (FR4.5) ======================
CREATE TABLE IF NOT EXISTS career_tracks (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name        VARCHAR(255) NOT NULL,
    description TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS career_track_jds (
    career_track_id UUID NOT NULL REFERENCES career_tracks(id) ON DELETE CASCADE,
    jd_id           UUID NOT NULL REFERENCES jd_submissions(id) ON DELETE CASCADE,
    added_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (career_track_id, jd_id)
);

CREATE INDEX IF NOT EXISTS idx_career_tracks_user ON career_tracks (user_id);

-- ====================== 10. ROADMAPS ======================
CREATE TABLE IF NOT EXISTS roadmaps (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id               UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    jd_id                 UUID NOT NULL REFERENCES jd_submissions(id) ON DELETE CASCADE,
    gap_analysis_id       UUID REFERENCES gap_analyses(id) ON DELETE SET NULL,
    title                 VARCHAR(255) NOT NULL,
    estimated_total_hours INTEGER,
    status                VARCHAR(20) NOT NULL DEFAULT 'generating'
                              CHECK (status IN ('generating','active','archived','completed','failed')),
    is_outdated           BOOLEAN NOT NULL DEFAULT FALSE,
    progress_percent      SMALLINT NOT NULL DEFAULT 0 CHECK (progress_percent BETWEEN 0 AND 100),
    created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS roadmap_nodes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    roadmap_id      UUID NOT NULL REFERENCES roadmaps(id) ON DELETE CASCADE,
    skill_id        UUID REFERENCES skills(id) ON DELETE SET NULL,
    skill_name      VARCHAR(150) NOT NULL,
    description     TEXT,
    sequence_order  SMALLINT NOT NULL,
    estimated_hours INTEGER,
    is_prerequisite BOOLEAN NOT NULL DEFAULT FALSE,
    status          VARCHAR(20) NOT NULL DEFAULT 'not_started'
                        CHECK (status IN ('not_started','in_progress','completed')),
    completed_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS roadmap_node_prerequisites (
    node_id              UUID NOT NULL REFERENCES roadmap_nodes(id) ON DELETE CASCADE,
    prerequisite_node_id UUID NOT NULL REFERENCES roadmap_nodes(id) ON DELETE CASCADE,
    PRIMARY KEY (node_id, prerequisite_node_id),
    CHECK (node_id <> prerequisite_node_id)
);

CREATE INDEX IF NOT EXISTS idx_roadmaps_user_status      ON roadmaps (user_id, status);
CREATE UNIQUE INDEX IF NOT EXISTS idx_roadmaps_jd_active ON roadmaps (jd_id) WHERE status IN ('active','generating');
CREATE INDEX IF NOT EXISTS idx_roadmap_nodes_roadmap     ON roadmap_nodes (roadmap_id, sequence_order);
CREATE INDEX IF NOT EXISTS idx_roadmap_nodes_skill       ON roadmap_nodes (skill_id) WHERE skill_id IS NOT NULL;

-- ====================== 11. LEARNING RESOURCES ======================
CREATE TABLE IF NOT EXISTS learning_resources (
    id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title                    VARCHAR(255) NOT NULL,
    type                     VARCHAR(20) NOT NULL
                                 CHECK (type IN ('video','article','course','documentation','fptu_internal')),
    provider                 VARCHAR(100),
    url                      TEXT NOT NULL,
    description              TEXT,
    is_free                  BOOLEAN NOT NULL DEFAULT TRUE,
    access_type              VARCHAR(30) NOT NULL
                                 CHECK (access_type IN ('free','fptu_internal','affiliate',
                                                       'partnership_premium','partnership_subscription')),
    affiliate_label          VARCHAR(100),
    affiliate_commission_rate DECIMAL(5,2),
    partner_id               UUID,
    language                 VARCHAR(5) NOT NULL DEFAULT 'vi',
    duration_minutes         INTEGER,
    needs_admin_review       BOOLEAN NOT NULL DEFAULT FALSE,
    is_active                BOOLEAN NOT NULL DEFAULT TRUE,
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS skill_resources (
    skill_id       UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    resource_id    UUID NOT NULL REFERENCES learning_resources(id) ON DELETE CASCADE,
    is_primary     BOOLEAN NOT NULL DEFAULT FALSE,
    sequence_order SMALLINT,
    PRIMARY KEY (skill_id, resource_id)
);

CREATE TABLE IF NOT EXISTS affiliate_clicks (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           UUID REFERENCES users(id) ON DELETE SET NULL,
    resource_id       UUID NOT NULL REFERENCES learning_resources(id) ON DELETE CASCADE,
    roadmap_node_id   UUID REFERENCES roadmap_nodes(id) ON DELETE SET NULL,
    redirect_url      TEXT NOT NULL,
    ip_address        INET,
    user_agent        TEXT,
    converted_at      TIMESTAMPTZ,
    commission_amount DECIMAL(10,2),
    clicked_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_resources_access      ON learning_resources (access_type) WHERE is_active = TRUE;
CREATE INDEX IF NOT EXISTS idx_skill_resources_skill ON skill_resources (skill_id);

-- ====================== 12. PORTFOLIO (FR6) ======================
CREATE TABLE IF NOT EXISTS portfolios (
    user_id               UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    headline              VARCHAR(255),
    bio                   TEXT,
    cover_image_url       TEXT,
    show_completed_skills BOOLEAN NOT NULL DEFAULT TRUE,
    show_certificates     BOOLEAN NOT NULL DEFAULT TRUE,
    show_projects         BOOLEAN NOT NULL DEFAULT TRUE,
    is_public             BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS portfolio_certificates (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name           VARCHAR(255) NOT NULL,
    issuer         VARCHAR(255),
    issued_date    DATE,
    expires_date   DATE,
    credential_url TEXT,
    file_url       TEXT,
    is_visible     BOOLEAN NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS portfolio_projects (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title          VARCHAR(255) NOT NULL,
    description    TEXT,
    repo_url       TEXT,
    live_url       TEXT,
    image_url      TEXT,
    tech_stack     JSONB,
    role           VARCHAR(100),
    started_date   DATE,
    completed_date DATE,
    is_visible     BOOLEAN NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_certs_user    ON portfolio_certificates (user_id) WHERE is_visible = TRUE;
CREATE INDEX IF NOT EXISTS idx_projects_user ON portfolio_projects (user_id) WHERE is_visible = TRUE;

-- ====================== 13. SUBSCRIPTIONS & PAYMENTS (FR8) ======================
CREATE TABLE IF NOT EXISTS user_subscriptions (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tier_id     UUID NOT NULL REFERENCES subscription_tiers(id),
    status      VARCHAR(20) NOT NULL DEFAULT 'active'
                    CHECK (status IN ('active','expired','cancelled')),
    started_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at  TIMESTAMPTZ,
    auto_renew  BOOLEAN NOT NULL DEFAULT FALSE,
    cancelled_at TIMESTAMPTZ,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS payment_orders (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    subscription_id   UUID REFERENCES user_subscriptions(id) ON DELETE SET NULL,
    tier_id           UUID NOT NULL REFERENCES subscription_tiers(id),
    duration_months   SMALLINT NOT NULL CHECK (duration_months IN (1,3,6)),
    amount            DECIMAL(10,2) NOT NULL,
    currency          CHAR(3) NOT NULL DEFAULT 'VND',
    payment_provider  VARCHAR(20) NOT NULL
                          CHECK (payment_provider IN ('manual_transfer','vnpay','momo','sepay')),
    provider_order_id VARCHAR(255) UNIQUE,
    status            VARCHAR(20) NOT NULL DEFAULT 'pending'
                          CHECK (status IN ('pending','completed','failed','cancelled')),
    completed_at      TIMESTAMPTZ,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS subscription_renewal_notifications (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    subscription_id   UUID NOT NULL REFERENCES user_subscriptions(id) ON DELETE CASCADE,
    notification_type VARCHAR(20) NOT NULL CHECK (notification_type IN ('renewal_7d','renewal_3d','renewal_0d')),
    sent_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (subscription_id, notification_type)
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_user_subs_active ON user_subscriptions (user_id) WHERE status = 'active';
CREATE INDEX IF NOT EXISTS idx_payment_user            ON payment_orders (user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_payment_status          ON payment_orders (status, created_at DESC);

-- ====================== 14. RAG & ADMIN ======================
CREATE TABLE IF NOT EXISTS rag_documents (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    uploaded_by       UUID REFERENCES users(id) ON DELETE SET NULL,
    title             VARCHAR(255) NOT NULL,
    source_type       VARCHAR(50) NOT NULL
                          CHECK (source_type IN ('fptu_curriculum','fptu_syllabus','external_doc')),
    file_url          TEXT,
    related_skill_ids UUID[] NOT NULL DEFAULT '{}',
    metadata          JSONB,
    chunks_count      INTEGER NOT NULL DEFAULT 0,
    embedding_status  VARCHAR(20) NOT NULL DEFAULT 'pending'
                          CHECK (embedding_status IN ('pending','processing','completed','failed')),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS rag_chunks (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL REFERENCES rag_documents(id) ON DELETE CASCADE,
    chunk_index INTEGER NOT NULL,
    content     TEXT NOT NULL,
    embedding   VECTOR(1536),
    token_count INTEGER,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (document_id, chunk_index)
);

CREATE TABLE IF NOT EXISTS rag_query_logs (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           UUID REFERENCES users(id) ON DELETE SET NULL,
    query_type        VARCHAR(50) NOT NULL,
    entity_id         UUID,
    prompt_tokens     INTEGER NOT NULL,
    completion_tokens INTEGER NOT NULL,
    cost_usd          DECIMAL(10,6) NOT NULL,
    duration_ms       INTEGER NOT NULL,
    model_used        VARCHAR(50) NOT NULL,
    success           BOOLEAN NOT NULL DEFAULT TRUE,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS admin_actions (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    action_type   VARCHAR(50) NOT NULL,
    target_type   VARCHAR(50),
    target_id     UUID,
    metadata      JSONB,
    ip_address    INET,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_rag_chunks_embedding ON rag_chunks USING hnsw (embedding vector_cosine_ops);
CREATE INDEX IF NOT EXISTS idx_rag_chunks_doc       ON rag_chunks (document_id);
CREATE INDEX IF NOT EXISTS idx_rag_docs_skills_gin  ON rag_documents USING GIN (related_skill_ids);
CREATE INDEX IF NOT EXISTS idx_rag_logs_user        ON rag_query_logs (user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_admin_actions_admin  ON admin_actions (admin_user_id, created_at DESC);

-- ============================================================================
-- 15. TRIGGERS
-- ============================================================================

-- 15.1 Free subscription cho user mới — XỬ LÝ Ở TẦNG APP, KHÔNG dùng DB trigger.
-- FreeSubscriptionFactory được gọi trong RegisterCommandHandler + GoogleLoginCommandHandler
-- để INSERT user_subscriptions (free, active). Nếu để cả DB trigger lẫn app cùng tạo sẽ
-- double-insert và vi phạm unique idx_user_subs_active. Gỡ trigger nếu DB cũ còn:
DROP TRIGGER IF EXISTS trg_users_create_free_sub ON users;
DROP FUNCTION IF EXISTS trg_create_free_subscription();

-- 15.2 Auto-update timestamps
CREATE OR REPLACE FUNCTION trg_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = NOW(); RETURN NEW; END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_users_updated_at         ON users;
CREATE TRIGGER trg_users_updated_at         BEFORE UPDATE ON users         FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

DROP TRIGGER IF EXISTS trg_skills_updated_at        ON skills;
CREATE TRIGGER trg_skills_updated_at        BEFORE UPDATE ON skills        FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

DROP TRIGGER IF EXISTS trg_career_tracks_updated_at ON career_tracks;
CREATE TRIGGER trg_career_tracks_updated_at BEFORE UPDATE ON career_tracks FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

DROP TRIGGER IF EXISTS trg_roadmaps_updated_at      ON roadmaps;
CREATE TRIGGER trg_roadmaps_updated_at      BEFORE UPDATE ON roadmaps      FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

DROP TRIGGER IF EXISTS trg_onboarding_updated_at    ON onboarding_responses;
CREATE TRIGGER trg_onboarding_updated_at    BEFORE UPDATE ON onboarding_responses FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

DROP TRIGGER IF EXISTS trg_resources_updated_at     ON learning_resources;
CREATE TRIGGER trg_resources_updated_at     BEFORE UPDATE ON learning_resources FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

DROP TRIGGER IF EXISTS trg_subs_updated_at          ON user_subscriptions;
CREATE TRIGGER trg_subs_updated_at          BEFORE UPDATE ON user_subscriptions FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

DROP TRIGGER IF EXISTS trg_portfolios_updated_at    ON portfolios;
CREATE TRIGGER trg_portfolios_updated_at    BEFORE UPDATE ON portfolios    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

-- 15.3 Auto-recalc roadmap progress
CREATE OR REPLACE FUNCTION trg_update_roadmap_progress()
RETURNS TRIGGER AS $$
DECLARE v_total INT; v_completed INT; v_percent SMALLINT;
BEGIN
    SELECT COUNT(*), COUNT(*) FILTER (WHERE status = 'completed')
    INTO v_total, v_completed
    FROM roadmap_nodes WHERE roadmap_id = NEW.roadmap_id;
    v_percent = CASE WHEN v_total = 0 THEN 0
                     ELSE ROUND((v_completed::NUMERIC / v_total) * 100)::SMALLINT END;
    UPDATE roadmaps
    SET progress_percent = v_percent,
        status = CASE WHEN v_percent = 100 THEN 'completed' ELSE status END,
        updated_at = NOW()
    WHERE id = NEW.roadmap_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_node_status_change ON roadmap_nodes;
CREATE TRIGGER trg_node_status_change
    AFTER INSERT OR UPDATE OF status ON roadmap_nodes
    FOR EACH ROW EXECUTE FUNCTION trg_update_roadmap_progress();

-- 15.4 Cleanup related_skill_ids khi xoá skill
CREATE OR REPLACE FUNCTION trg_cleanup_skill_in_rag_docs()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE rag_documents
    SET related_skill_ids = array_remove(related_skill_ids, OLD.id)
    WHERE OLD.id = ANY(related_skill_ids);
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_skills_before_delete ON skills;
CREATE TRIGGER trg_skills_before_delete
    BEFORE DELETE ON skills
    FOR EACH ROW EXECUTE FUNCTION trg_cleanup_skill_in_rag_docs();

-- ============================================================================
-- 16. SEED DATA (idempotent qua ON CONFLICT DO NOTHING)
-- ============================================================================

-- Subscription tiers
INSERT INTO subscription_tiers (
    tier_code, display_name, price_monthly, currency,
    jd_quota, gap_analysis_quota, assessment_quota, roadmap_active_quota,
    career_track_quota, portfolio_certificate_quota, portfolio_project_quota,
    full_gap_history, is_active
) VALUES
    ('free',    'Miễn phí',  0,     'VND',  3,  3,  3,  3,  1,  3,  3, FALSE, TRUE),
    ('student', 'Sinh viên', 49000, 'VND', -1, -1, -1, -1, -1, -1, -1, TRUE,  TRUE)
ON CONFLICT (tier_code) DO NOTHING;

-- Skills phổ biến (IT + Marketing). Admin có thể thêm/sửa sau qua UI.
INSERT INTO skills (name, slug, category, major, description, difficulty_level) VALUES
    ('OOP Principles',  'oop-principles', 'fundamentals', 'IT', 'Object-Oriented Programming', 2),
    ('Java',            'java',           'language',     'IT', 'Java programming language', 2),
    ('Spring Boot',     'spring-boot',    'framework',    'IT', 'Java backend framework', 3),
    ('SQL',             'sql',            'database',     'IT', 'Structured Query Language', 2),
    ('Docker',          'docker',         'devops',       'IT', 'Container platform', 3),
    ('Git',             'git',            'tooling',      'IT', 'Version control system', 1),
    ('REST API Design', 'rest-api',       'architecture', 'IT', 'RESTful API design', 2),
    ('React',           'react',          'framework',    'IT', 'Frontend library', 3),
    ('SEO',             'seo',            'marketing',    'Marketing', 'Search Engine Optimization', 2),
    ('Content Writing', 'content-writing','content',      'Marketing', 'Copywriting skills', 1)
ON CONFLICT (slug) DO NOTHING;

-- ============================================================================
-- 17. (OPTIONAL) Admin user — chạy sau khi đã có BCrypt hash
-- ============================================================================
-- Lý do để comment: spec yêu cầu bcrypt cost=11 cho password. Phải sinh hash trước.
-- Có 3 cách lấy hash cho "Admin@123":
--
--   (1) C# REPL/script (trong project có BCrypt.Net-Next):
--       Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 11));
--
--   (2) htpasswd (Linux/Mac/WSL):
--       htpasswd -bnBC 11 "" "Admin@123" | tr -d ':\n' | sed 's/$2y/$2a/'
--
--   (3) Online generator (CHỈ DEV — không dùng password thật):
--       https://bcrypt-generator.com/  → chọn cost 11
--
-- Sau khi có hash, uncomment khối dưới và paste vào chỗ <BCRYPT_HASH>:
--
-- INSERT INTO users (email, password_hash, auth_provider, full_name, role, is_survey_completed)
-- VALUES (
--     'admin@edunexus.local',
--     '<BCRYPT_HASH>',
--     'email',
--     'System Admin',
--     'admin',
--     TRUE
-- )
-- ON CONFLICT (email) DO NOTHING;

-- ============================================================================
-- 18. VERIFY (chạy thủ công sau khi load schema)
-- ============================================================================
-- SELECT extname FROM pg_extension WHERE extname IN ('uuid-ossp', 'vector', 'pg_trgm');   -- expect 3 rows
-- SELECT tier_code, jd_quota FROM subscription_tiers;                                       -- expect 2 rows: free, student
-- SELECT COUNT(*) FROM skills;                                                              -- expect 10 (sau seed)
--
-- Test trigger Free subscription:
--   INSERT INTO users (email, password_hash, auth_provider, full_name)
--   VALUES ('test@test.com', '$2a$11$dummy', 'email', 'Test User');
--   SELECT u.email, st.tier_code FROM users u
--     JOIN user_subscriptions us ON us.user_id = u.id
--     JOIN subscription_tiers st ON st.id = us.tier_id
--     WHERE u.email = 'test@test.com';
--   -- expect: test@test.com | free
--   DELETE FROM users WHERE email = 'test@test.com';
