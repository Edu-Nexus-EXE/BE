CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS pg_trgm;
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    CREATE EXTENSION IF NOT EXISTS vector;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE learning_resources (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        title character varying(255) NOT NULL,
        type character varying(20) NOT NULL,
        provider character varying(100),
        url text NOT NULL,
        description text,
        is_free boolean NOT NULL DEFAULT TRUE,
        access_type character varying(30) NOT NULL,
        affiliate_label character varying(100),
        affiliate_commission_rate numeric(5,2),
        partner_id uuid,
        language character varying(5) NOT NULL DEFAULT ('vi'::character varying),
        duration_minutes integer,
        needs_admin_review boolean NOT NULL DEFAULT FALSE,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT learning_resources_pkey PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE skills (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        name character varying(150) NOT NULL,
        slug character varying(150) NOT NULL,
        category character varying(50) NOT NULL,
        major character varying(50) NOT NULL,
        description text,
        difficulty_level smallint NOT NULL DEFAULT 1,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT skills_pkey PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE subscription_tiers (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        tier_code character varying(20) NOT NULL,
        display_name character varying(50) NOT NULL,
        price_monthly numeric(10,2) NOT NULL,
        currency character(3) NOT NULL DEFAULT ('VND'::bpchar),
        jd_quota integer NOT NULL,
        gap_analysis_quota integer NOT NULL,
        assessment_quota integer NOT NULL,
        roadmap_active_quota integer NOT NULL,
        career_track_quota integer NOT NULL,
        portfolio_certificate_quota integer NOT NULL,
        portfolio_project_quota integer NOT NULL,
        full_gap_history boolean NOT NULL DEFAULT FALSE,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT subscription_tiers_pkey PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        email character varying(255) NOT NULL,
        password_hash character varying(255),
        auth_provider character varying(20) NOT NULL DEFAULT ('email'::character varying),
        google_sub character varying(255),
        full_name character varying(255) NOT NULL,
        avatar_url text,
        role character varying(20) NOT NULL DEFAULT ('user'::character varying),
        is_banned boolean NOT NULL DEFAULT FALSE,
        is_survey_completed boolean NOT NULL DEFAULT FALSE,
        portfolio_url_slug character varying(100),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        last_login_at timestamp with time zone,
        CONSTRAINT users_pkey PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE skill_prerequisites (
        skill_id uuid NOT NULL,
        prerequisite_id uuid NOT NULL,
        CONSTRAINT skill_prerequisites_pkey PRIMARY KEY (skill_id, prerequisite_id),
        CONSTRAINT skill_prerequisites_prerequisite_id_fkey FOREIGN KEY (prerequisite_id) REFERENCES skills (id) ON DELETE CASCADE,
        CONSTRAINT skill_prerequisites_skill_id_fkey FOREIGN KEY (skill_id) REFERENCES skills (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE skill_resources (
        skill_id uuid NOT NULL,
        resource_id uuid NOT NULL,
        is_primary boolean NOT NULL DEFAULT FALSE,
        sequence_order smallint,
        CONSTRAINT skill_resources_pkey PRIMARY KEY (skill_id, resource_id),
        CONSTRAINT skill_resources_resource_id_fkey FOREIGN KEY (resource_id) REFERENCES learning_resources (id) ON DELETE CASCADE,
        CONSTRAINT skill_resources_skill_id_fkey FOREIGN KEY (skill_id) REFERENCES skills (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE admin_actions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        admin_user_id uuid NOT NULL,
        action_type character varying(50) NOT NULL,
        target_type character varying(50),
        target_id uuid,
        metadata jsonb,
        ip_address inet,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT admin_actions_pkey PRIMARY KEY (id),
        CONSTRAINT admin_actions_admin_user_id_fkey FOREIGN KEY (admin_user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE career_tracks (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        name character varying(255) NOT NULL,
        description text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT career_tracks_pkey PRIMARY KEY (id),
        CONSTRAINT career_tracks_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE jd_submissions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        source_type character varying(20) NOT NULL,
        source_url text,
        raw_content text,
        job_title character varying(255),
        job_role_category character varying(100),
        seniority_level character varying(50),
        salary_min integer,
        salary_max integer,
        currency character(3),
        parse_status character varying(20) NOT NULL DEFAULT ('pending'::character varying),
        parse_error text,
        parsed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        CONSTRAINT jd_submissions_pkey PRIMARY KEY (id),
        CONSTRAINT jd_submissions_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE onboarding_responses (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        academic_year character varying(20) NOT NULL,
        major character varying(50) NOT NULL,
        primary_goal character varying(100) NOT NULL,
        weekly_study_hours character varying(20) NOT NULL,
        proficiency_level character varying(50) NOT NULL,
        learning_priority character varying(50) NOT NULL,
        learning_budget character varying(30) NOT NULL,
        preferred_channel character varying(50) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT onboarding_responses_pkey PRIMARY KEY (id),
        CONSTRAINT onboarding_responses_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE portfolio_certificates (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        name character varying(255) NOT NULL,
        issuer character varying(255),
        issued_date date,
        expires_date date,
        credential_url text,
        file_url text,
        is_visible boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT portfolio_certificates_pkey PRIMARY KEY (id),
        CONSTRAINT portfolio_certificates_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE portfolio_projects (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        title character varying(255) NOT NULL,
        description text,
        repo_url text,
        live_url text,
        image_url text,
        tech_stack jsonb,
        role character varying(100),
        started_date date,
        completed_date date,
        is_visible boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT portfolio_projects_pkey PRIMARY KEY (id),
        CONSTRAINT portfolio_projects_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE portfolios (
        user_id uuid NOT NULL,
        headline character varying(255),
        bio text,
        cover_image_url text,
        show_completed_skills boolean NOT NULL DEFAULT TRUE,
        show_certificates boolean NOT NULL DEFAULT TRUE,
        show_projects boolean NOT NULL DEFAULT TRUE,
        is_public boolean NOT NULL DEFAULT TRUE,
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT portfolios_pkey PRIMARY KEY (user_id),
        CONSTRAINT portfolios_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE rag_documents (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        uploaded_by uuid,
        title character varying(255) NOT NULL,
        source_type character varying(50) NOT NULL,
        file_url text,
        related_skill_ids uuid[] NOT NULL DEFAULT ('{}'::uuid[]),
        metadata jsonb,
        chunks_count integer NOT NULL DEFAULT 0,
        embedding_status character varying(20) NOT NULL DEFAULT ('pending'::character varying),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT rag_documents_pkey PRIMARY KEY (id),
        CONSTRAINT rag_documents_uploaded_by_fkey FOREIGN KEY (uploaded_by) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE rag_query_logs (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid,
        query_type character varying(50) NOT NULL,
        entity_id uuid,
        prompt_tokens integer NOT NULL,
        completion_tokens integer NOT NULL,
        cost_usd numeric(10,6) NOT NULL,
        duration_ms integer NOT NULL,
        model_used character varying(50) NOT NULL,
        success boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT rag_query_logs_pkey PRIMARY KEY (id),
        CONSTRAINT rag_query_logs_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        token_hash character varying(255) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT refresh_tokens_pkey PRIMARY KEY (id),
        CONSTRAINT refresh_tokens_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE user_subscriptions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        tier_id uuid NOT NULL,
        status character varying(20) NOT NULL DEFAULT ('active'::character varying),
        started_at timestamp with time zone NOT NULL DEFAULT (now()),
        expires_at timestamp with time zone,
        auto_renew boolean NOT NULL DEFAULT FALSE,
        cancelled_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT user_subscriptions_pkey PRIMARY KEY (id),
        CONSTRAINT user_subscriptions_tier_id_fkey FOREIGN KEY (tier_id) REFERENCES subscription_tiers (id),
        CONSTRAINT user_subscriptions_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE assessment_paths (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        jd_id uuid NOT NULL,
        user_id uuid NOT NULL,
        path_type character varying(20) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT assessment_paths_pkey PRIMARY KEY (id),
        CONSTRAINT assessment_paths_jd_id_fkey FOREIGN KEY (jd_id) REFERENCES jd_submissions (id) ON DELETE CASCADE,
        CONSTRAINT assessment_paths_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE career_track_jds (
        career_track_id uuid NOT NULL,
        jd_id uuid NOT NULL,
        added_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT career_track_jds_pkey PRIMARY KEY (career_track_id, jd_id),
        CONSTRAINT career_track_jds_career_track_id_fkey FOREIGN KEY (career_track_id) REFERENCES career_tracks (id) ON DELETE CASCADE,
        CONSTRAINT career_track_jds_jd_id_fkey FOREIGN KEY (jd_id) REFERENCES jd_submissions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE jd_skills (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        jd_id uuid NOT NULL,
        skill_id uuid,
        skill_name_raw character varying(150) NOT NULL,
        skill_type character varying(20) NOT NULL,
        is_mandatory boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT jd_skills_pkey PRIMARY KEY (id),
        CONSTRAINT jd_skills_jd_id_fkey FOREIGN KEY (jd_id) REFERENCES jd_submissions (id) ON DELETE CASCADE,
        CONSTRAINT jd_skills_skill_id_fkey FOREIGN KEY (skill_id) REFERENCES skills (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE rag_chunks (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        document_id uuid NOT NULL,
        chunk_index integer NOT NULL,
        content text NOT NULL,
        embedding vector(1536),
        token_count integer,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT rag_chunks_pkey PRIMARY KEY (id),
        CONSTRAINT rag_chunks_document_id_fkey FOREIGN KEY (document_id) REFERENCES rag_documents (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE payment_orders (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        subscription_id uuid,
        tier_id uuid NOT NULL,
        duration_months smallint NOT NULL,
        amount numeric(10,2) NOT NULL,
        currency character(3) NOT NULL DEFAULT ('VND'::bpchar),
        payment_provider character varying(20) NOT NULL,
        provider_order_id character varying(255),
        status character varying(20) NOT NULL DEFAULT ('pending'::character varying),
        completed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT payment_orders_pkey PRIMARY KEY (id),
        CONSTRAINT payment_orders_subscription_id_fkey FOREIGN KEY (subscription_id) REFERENCES user_subscriptions (id) ON DELETE SET NULL,
        CONSTRAINT payment_orders_tier_id_fkey FOREIGN KEY (tier_id) REFERENCES subscription_tiers (id),
        CONSTRAINT payment_orders_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE subscription_renewal_notifications (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        subscription_id uuid NOT NULL,
        notification_type character varying(20) NOT NULL,
        sent_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT subscription_renewal_notifications_pkey PRIMARY KEY (id),
        CONSTRAINT subscription_renewal_notifications_subscription_id_fkey FOREIGN KEY (subscription_id) REFERENCES user_subscriptions (id) ON DELETE CASCADE,
        CONSTRAINT subscription_renewal_notifications_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE assessment_sessions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        assessment_path_id uuid NOT NULL,
        user_id uuid NOT NULL,
        job_role_category_snapshot character varying(100) NOT NULL,
        part1_count smallint NOT NULL,
        part2_count smallint NOT NULL,
        skill_scores jsonb,
        status character varying(20) NOT NULL DEFAULT ('in_progress'::character varying),
        is_current boolean NOT NULL DEFAULT TRUE,
        reused_from_session_id uuid,
        started_at timestamp with time zone NOT NULL DEFAULT (now()),
        submitted_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT assessment_sessions_pkey PRIMARY KEY (id),
        CONSTRAINT assessment_sessions_assessment_path_id_fkey FOREIGN KEY (assessment_path_id) REFERENCES assessment_paths (id) ON DELETE CASCADE,
        CONSTRAINT assessment_sessions_reused_from_session_id_fkey FOREIGN KEY (reused_from_session_id) REFERENCES assessment_sessions (id) ON DELETE SET NULL,
        CONSTRAINT assessment_sessions_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE cv_submissions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        assessment_path_id uuid NOT NULL,
        user_id uuid NOT NULL,
        file_url text NOT NULL,
        file_name character varying(255),
        file_size_bytes integer,
        mime_type character varying(50),
        parsed_text text,
        parsed_skills jsonb,
        parse_status character varying(20) NOT NULL DEFAULT ('pending'::character varying),
        parse_error text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        parsed_at timestamp with time zone,
        CONSTRAINT cv_submissions_pkey PRIMARY KEY (id),
        CONSTRAINT cv_submissions_assessment_path_id_fkey FOREIGN KEY (assessment_path_id) REFERENCES assessment_paths (id) ON DELETE CASCADE,
        CONSTRAINT cv_submissions_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE gap_analyses (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        jd_id uuid NOT NULL,
        assessment_path_id uuid NOT NULL,
        input_source character varying(20) NOT NULL,
        version smallint NOT NULL DEFAULT 1,
        is_latest boolean NOT NULL DEFAULT TRUE,
        summary jsonb,
        status character varying(20) NOT NULL DEFAULT ('pending'::character varying),
        error text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        completed_at timestamp with time zone,
        CONSTRAINT gap_analyses_pkey PRIMARY KEY (id),
        CONSTRAINT gap_analyses_assessment_path_id_fkey FOREIGN KEY (assessment_path_id) REFERENCES assessment_paths (id) ON DELETE CASCADE,
        CONSTRAINT gap_analyses_jd_id_fkey FOREIGN KEY (jd_id) REFERENCES jd_submissions (id) ON DELETE CASCADE,
        CONSTRAINT gap_analyses_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE assessment_questions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        session_id uuid NOT NULL,
        sequence_order smallint NOT NULL,
        part smallint NOT NULL,
        question_text text NOT NULL,
        options jsonb NOT NULL,
        correct_option character varying(1) NOT NULL,
        related_skill character varying(150),
        explanation text,
        CONSTRAINT assessment_questions_pkey PRIMARY KEY (id),
        CONSTRAINT assessment_questions_session_id_fkey FOREIGN KEY (session_id) REFERENCES assessment_sessions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE gap_analysis_skills (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        gap_analysis_id uuid NOT NULL,
        skill_id uuid,
        skill_name character varying(150) NOT NULL,
        gap_status character varying(20) NOT NULL,
        current_level character varying(20),
        target_level character varying(20) NOT NULL,
        urgency_score smallint,
        reasoning text,
        is_mandatory_in_jd boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT gap_analysis_skills_pkey PRIMARY KEY (id),
        CONSTRAINT gap_analysis_skills_gap_analysis_id_fkey FOREIGN KEY (gap_analysis_id) REFERENCES gap_analyses (id) ON DELETE CASCADE,
        CONSTRAINT gap_analysis_skills_skill_id_fkey FOREIGN KEY (skill_id) REFERENCES skills (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE roadmaps (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        jd_id uuid NOT NULL,
        gap_analysis_id uuid,
        title character varying(255) NOT NULL,
        estimated_total_hours integer,
        status character varying(20) NOT NULL DEFAULT ('generating'::character varying),
        is_outdated boolean NOT NULL DEFAULT FALSE,
        progress_percent smallint NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT roadmaps_pkey PRIMARY KEY (id),
        CONSTRAINT roadmaps_gap_analysis_id_fkey FOREIGN KEY (gap_analysis_id) REFERENCES gap_analyses (id) ON DELETE SET NULL,
        CONSTRAINT roadmaps_jd_id_fkey FOREIGN KEY (jd_id) REFERENCES jd_submissions (id) ON DELETE CASCADE,
        CONSTRAINT roadmaps_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE assessment_answers (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        session_id uuid NOT NULL,
        question_id uuid NOT NULL,
        selected_option character varying(1) NOT NULL,
        is_correct boolean NOT NULL,
        answered_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT assessment_answers_pkey PRIMARY KEY (id),
        CONSTRAINT assessment_answers_question_id_fkey FOREIGN KEY (question_id) REFERENCES assessment_questions (id) ON DELETE CASCADE,
        CONSTRAINT assessment_answers_session_id_fkey FOREIGN KEY (session_id) REFERENCES assessment_sessions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE roadmap_nodes (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        roadmap_id uuid NOT NULL,
        skill_id uuid,
        skill_name character varying(150) NOT NULL,
        description text,
        sequence_order smallint NOT NULL,
        estimated_hours integer,
        is_prerequisite boolean NOT NULL DEFAULT FALSE,
        status character varying(20) NOT NULL DEFAULT ('not_started'::character varying),
        completed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT roadmap_nodes_pkey PRIMARY KEY (id),
        CONSTRAINT roadmap_nodes_roadmap_id_fkey FOREIGN KEY (roadmap_id) REFERENCES roadmaps (id) ON DELETE CASCADE,
        CONSTRAINT roadmap_nodes_skill_id_fkey FOREIGN KEY (skill_id) REFERENCES skills (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE affiliate_clicks (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid,
        resource_id uuid NOT NULL,
        roadmap_node_id uuid,
        redirect_url text NOT NULL,
        ip_address inet,
        user_agent text,
        converted_at timestamp with time zone,
        commission_amount numeric(10,2),
        clicked_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT affiliate_clicks_pkey PRIMARY KEY (id),
        CONSTRAINT affiliate_clicks_resource_id_fkey FOREIGN KEY (resource_id) REFERENCES learning_resources (id) ON DELETE CASCADE,
        CONSTRAINT affiliate_clicks_roadmap_node_id_fkey FOREIGN KEY (roadmap_node_id) REFERENCES roadmap_nodes (id) ON DELETE SET NULL,
        CONSTRAINT affiliate_clicks_user_id_fkey FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE TABLE roadmap_node_prerequisites (
        node_id uuid NOT NULL,
        prerequisite_node_id uuid NOT NULL,
        CONSTRAINT roadmap_node_prerequisites_pkey PRIMARY KEY (node_id, prerequisite_node_id),
        CONSTRAINT roadmap_node_prerequisites_node_id_fkey FOREIGN KEY (node_id) REFERENCES roadmap_nodes (id) ON DELETE CASCADE,
        CONSTRAINT roadmap_node_prerequisites_prerequisite_node_id_fkey FOREIGN KEY (prerequisite_node_id) REFERENCES roadmap_nodes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_admin_actions_admin ON admin_actions (admin_user_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_affiliate_clicks_resource_id" ON affiliate_clicks (resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_affiliate_clicks_roadmap_node_id" ON affiliate_clicks (roadmap_node_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_affiliate_clicks_user_id" ON affiliate_clicks (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX assessment_answers_session_id_question_id_key ON assessment_answers (session_id, question_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_assessment_answers_question_id" ON assessment_answers (question_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX assessment_paths_jd_id_key ON assessment_paths (jd_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_assessment_paths_user ON assessment_paths (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_questions_session ON assessment_questions (session_id, sequence_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX idx_assessment_sessions_current ON assessment_sessions (assessment_path_id) WHERE (is_current = true);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_sessions_path ON assessment_sessions (assessment_path_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_sessions_reuse ON assessment_sessions (user_id, job_role_category_snapshot) WHERE (((status)::text = 'submitted'::text) AND (is_current = true));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_assessment_sessions_reused_from_session_id" ON assessment_sessions (reused_from_session_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_career_track_jds_jd_id" ON career_track_jds (jd_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_career_tracks_user ON career_tracks (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX cv_submissions_assessment_path_id_key ON cv_submissions (assessment_path_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_cv_user ON cv_submissions (user_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_gap_jd ON gap_analyses (jd_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX idx_gap_jd_latest ON gap_analyses (jd_id) WHERE (is_latest = true);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_gap_user ON gap_analyses (user_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_gap_analyses_assessment_path_id" ON gap_analyses (assessment_path_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_gap_skills_ga ON gap_analysis_skills (gap_analysis_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_gap_analysis_skills_skill_id" ON gap_analysis_skills (skill_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_jd_skills_jd ON jd_skills (jd_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_jd_skills_skill ON jd_skills (skill_id) WHERE (skill_id IS NOT NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_jd_parse_status ON jd_submissions (parse_status) WHERE ((parse_status)::text = ANY ((ARRAY['pending'::character varying, 'processing'::character varying, 'failed'::character varying])::text[]));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_jd_user ON jd_submissions (user_id, created_at DESC) WHERE (deleted_at IS NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_resources_access ON learning_resources (access_type) WHERE (is_active = true);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_onboarding_major ON onboarding_responses (major);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX onboarding_responses_user_id_key ON onboarding_responses (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_payment_status ON payment_orders (status, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_payment_user ON payment_orders (user_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_payment_orders_subscription_id" ON payment_orders (subscription_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_payment_orders_tier_id" ON payment_orders (tier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX payment_orders_provider_order_id_key ON payment_orders (provider_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_certs_user ON portfolio_certificates (user_id) WHERE (is_visible = true);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_projects_user ON portfolio_projects (user_id) WHERE (is_visible = true);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_rag_chunks_doc ON rag_chunks (document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_rag_chunks_embedding ON rag_chunks USING hnsw (embedding vector_cosine_ops);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX rag_chunks_document_id_chunk_index_key ON rag_chunks (document_id, chunk_index);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_rag_docs_skills_gin ON rag_documents USING gin (related_skill_ids);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_rag_documents_uploaded_by" ON rag_documents (uploaded_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_rag_logs_user ON rag_query_logs (user_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_refresh_tokens_user ON refresh_tokens (user_id, revoked_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX refresh_tokens_token_hash_key ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_roadmap_node_prerequisites_prerequisite_node_id" ON roadmap_node_prerequisites (prerequisite_node_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_roadmap_nodes_roadmap ON roadmap_nodes (roadmap_id, sequence_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_roadmap_nodes_skill ON roadmap_nodes (skill_id) WHERE (skill_id IS NOT NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX idx_roadmaps_jd_active ON roadmaps (jd_id) WHERE ((status)::text = ANY ((ARRAY['active'::character varying, 'generating'::character varying])::text[]));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_roadmaps_user_status ON roadmaps (user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_roadmaps_gap_analysis_id" ON roadmaps (gap_analysis_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_skill_prerequisites_prerequisite_id" ON skill_prerequisites (prerequisite_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_skill_resources_skill ON skill_resources (skill_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_skill_resources_resource_id" ON skill_resources (resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_skills_name_trgm ON skills USING gin (name gin_trgm_ops);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX skills_slug_key ON skills (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_subscription_renewal_notifications_user_id" ON subscription_renewal_notifications (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX subscription_renewal_notifica_subscription_id_notification__key ON subscription_renewal_notifications (subscription_id, notification_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX subscription_tiers_tier_code_key ON subscription_tiers (tier_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX idx_user_subs_active ON user_subscriptions (user_id) WHERE ((status)::text = 'active'::text);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX "IX_user_subscriptions_tier_id" ON user_subscriptions (tier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE INDEX idx_users_email ON users (email) WHERE (deleted_at IS NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX users_email_key ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX users_google_sub_key ON users (google_sub);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    CREATE UNIQUE INDEX users_portfolio_url_slug_key ON users (portfolio_url_slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260527063853_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260527063853_InitialCreate', '9.0.4');
    END IF;
END $EF$;
COMMIT;

