using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Edu_Nexus.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "learning_resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    url = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_free = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    access_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    affiliate_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    affiliate_commission_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    partner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValueSql: "'vi'::character varying"),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    needs_admin_review = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("learning_resources_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    major = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    difficulty_level = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("skills_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_tiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tier_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price_monthly = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValueSql: "'VND'::bpchar"),
                    jd_quota = table.Column<int>(type: "integer", nullable: false),
                    gap_analysis_quota = table.Column<int>(type: "integer", nullable: false),
                    assessment_quota = table.Column<int>(type: "integer", nullable: false),
                    roadmap_active_quota = table.Column<int>(type: "integer", nullable: false),
                    career_track_quota = table.Column<int>(type: "integer", nullable: false),
                    portfolio_certificate_quota = table.Column<int>(type: "integer", nullable: false),
                    portfolio_project_quota = table.Column<int>(type: "integer", nullable: false),
                    full_gap_history = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("subscription_tiers_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    auth_provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'email'::character varying"),
                    google_sub = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'user'::character varying"),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_survey_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    portfolio_url_slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_prerequisites",
                columns: table => new
                {
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prerequisite_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("skill_prerequisites_pkey", x => new { x.skill_id, x.prerequisite_id });
                    table.ForeignKey(
                        name: "skill_prerequisites_prerequisite_id_fkey",
                        column: x => x.prerequisite_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "skill_prerequisites_skill_id_fkey",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_resources",
                columns: table => new
                {
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sequence_order = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("skill_resources_pkey", x => new { x.skill_id, x.resource_id });
                    table.ForeignKey(
                        name: "skill_resources_resource_id_fkey",
                        column: x => x.resource_id,
                        principalTable: "learning_resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "skill_resources_skill_id_fkey",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("admin_actions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "admin_actions_admin_user_id_fkey",
                        column: x => x.admin_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "career_tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("career_tracks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "career_tracks_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jd_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: true),
                    raw_content = table.Column<string>(type: "text", nullable: true),
                    job_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    job_role_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    seniority_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    salary_min = table.Column<int>(type: "integer", nullable: true),
                    salary_max = table.Column<int>(type: "integer", nullable: true),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    parse_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying"),
                    parse_error = table.Column<string>(type: "text", nullable: true),
                    parsed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("jd_submissions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "jd_submissions_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    major = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    primary_goal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    weekly_study_hours = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    proficiency_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    learning_priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    learning_budget = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    preferred_channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("onboarding_responses_pkey", x => x.id);
                    table.ForeignKey(
                        name: "onboarding_responses_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    issuer = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_date = table.Column<DateOnly>(type: "date", nullable: true),
                    credential_url = table.Column<string>(type: "text", nullable: true),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("portfolio_certificates_pkey", x => x.id);
                    table.ForeignKey(
                        name: "portfolio_certificates_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    repo_url = table.Column<string>(type: "text", nullable: true),
                    live_url = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    tech_stack = table.Column<string>(type: "jsonb", nullable: true),
                    role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    started_date = table.Column<DateOnly>(type: "date", nullable: true),
                    completed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("portfolio_projects_pkey", x => x.id);
                    table.ForeignKey(
                        name: "portfolio_projects_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolios",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    headline = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    cover_image_url = table.Column<string>(type: "text", nullable: true),
                    show_completed_skills = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_certificates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_projects = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("portfolios_pkey", x => x.user_id);
                    table.ForeignKey(
                        name: "portfolios_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rag_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    related_skill_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false, defaultValueSql: "'{}'::uuid[]"),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    chunks_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    embedding_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("rag_documents_pkey", x => x.id);
                    table.ForeignKey(
                        name: "rag_documents_uploaded_by_fkey",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rag_query_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    query_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    model_used = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("rag_query_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "rag_query_logs_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("refresh_tokens_pkey", x => x.id);
                    table.ForeignKey(
                        name: "refresh_tokens_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'active'::character varying"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    auto_renew = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_subscriptions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_subscriptions_tier_id_fkey",
                        column: x => x.tier_id,
                        principalTable: "subscription_tiers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "user_subscriptions_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assessment_paths",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jd_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("assessment_paths_pkey", x => x.id);
                    table.ForeignKey(
                        name: "assessment_paths_jd_id_fkey",
                        column: x => x.jd_id,
                        principalTable: "jd_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "assessment_paths_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "career_track_jds",
                columns: table => new
                {
                    career_track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jd_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("career_track_jds_pkey", x => new { x.career_track_id, x.jd_id });
                    table.ForeignKey(
                        name: "career_track_jds_career_track_id_fkey",
                        column: x => x.career_track_id,
                        principalTable: "career_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "career_track_jds_jd_id_fkey",
                        column: x => x.jd_id,
                        principalTable: "jd_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jd_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jd_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    skill_name_raw = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    skill_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("jd_skills_pkey", x => x.id);
                    table.ForeignKey(
                        name: "jd_skills_jd_id_fkey",
                        column: x => x.jd_id,
                        principalTable: "jd_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "jd_skills_skill_id_fkey",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rag_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chunk_index = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(1536)", maxLength: 1536, nullable: true),
                    token_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("rag_chunks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "rag_chunks_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "rag_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration_months = table.Column<short>(type: "smallint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValueSql: "'VND'::bpchar"),
                    payment_provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_order_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_orders_pkey", x => x.id);
                    table.ForeignKey(
                        name: "payment_orders_subscription_id_fkey",
                        column: x => x.subscription_id,
                        principalTable: "user_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "payment_orders_tier_id_fkey",
                        column: x => x.tier_id,
                        principalTable: "subscription_tiers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "payment_orders_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_renewal_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("subscription_renewal_notifications_pkey", x => x.id);
                    table.ForeignKey(
                        name: "subscription_renewal_notifications_subscription_id_fkey",
                        column: x => x.subscription_id,
                        principalTable: "user_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "subscription_renewal_notifications_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assessment_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_path_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_role_category_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    part1_count = table.Column<short>(type: "smallint", nullable: false),
                    part2_count = table.Column<short>(type: "smallint", nullable: false),
                    skill_scores = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'in_progress'::character varying"),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    reused_from_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("assessment_sessions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "assessment_sessions_assessment_path_id_fkey",
                        column: x => x.assessment_path_id,
                        principalTable: "assessment_paths",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "assessment_sessions_reused_from_session_id_fkey",
                        column: x => x.reused_from_session_id,
                        principalTable: "assessment_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "assessment_sessions_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cv_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_path_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    file_size_bytes = table.Column<int>(type: "integer", nullable: true),
                    mime_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    parsed_text = table.Column<string>(type: "text", nullable: true),
                    parsed_skills = table.Column<string>(type: "jsonb", nullable: true),
                    parse_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying"),
                    parse_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    parsed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cv_submissions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "cv_submissions_assessment_path_id_fkey",
                        column: x => x.assessment_path_id,
                        principalTable: "assessment_paths",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "cv_submissions_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gap_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jd_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_path_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    is_latest = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    summary = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'pending'::character varying"),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("gap_analyses_pkey", x => x.id);
                    table.ForeignKey(
                        name: "gap_analyses_assessment_path_id_fkey",
                        column: x => x.assessment_path_id,
                        principalTable: "assessment_paths",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "gap_analyses_jd_id_fkey",
                        column: x => x.jd_id,
                        principalTable: "jd_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "gap_analyses_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assessment_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_order = table.Column<short>(type: "smallint", nullable: false),
                    part = table.Column<short>(type: "smallint", nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    options = table.Column<string>(type: "jsonb", nullable: false),
                    correct_option = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    related_skill = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    explanation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("assessment_questions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "assessment_questions_session_id_fkey",
                        column: x => x.session_id,
                        principalTable: "assessment_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gap_analysis_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    gap_analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    skill_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    gap_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    target_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    urgency_score = table.Column<short>(type: "smallint", nullable: true),
                    reasoning = table.Column<string>(type: "text", nullable: true),
                    is_mandatory_in_jd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("gap_analysis_skills_pkey", x => x.id);
                    table.ForeignKey(
                        name: "gap_analysis_skills_gap_analysis_id_fkey",
                        column: x => x.gap_analysis_id,
                        principalTable: "gap_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "gap_analysis_skills_skill_id_fkey",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "roadmaps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jd_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gap_analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    estimated_total_hours = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'generating'::character varying"),
                    is_outdated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    progress_percent = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("roadmaps_pkey", x => x.id);
                    table.ForeignKey(
                        name: "roadmaps_gap_analysis_id_fkey",
                        column: x => x.gap_analysis_id,
                        principalTable: "gap_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "roadmaps_jd_id_fkey",
                        column: x => x.jd_id,
                        principalTable: "jd_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "roadmaps_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assessment_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_option = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("assessment_answers_pkey", x => x.id);
                    table.ForeignKey(
                        name: "assessment_answers_question_id_fkey",
                        column: x => x.question_id,
                        principalTable: "assessment_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "assessment_answers_session_id_fkey",
                        column: x => x.session_id,
                        principalTable: "assessment_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roadmap_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    roadmap_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    skill_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sequence_order = table.Column<short>(type: "smallint", nullable: false),
                    estimated_hours = table.Column<int>(type: "integer", nullable: true),
                    is_prerequisite = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'not_started'::character varying"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("roadmap_nodes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "roadmap_nodes_roadmap_id_fkey",
                        column: x => x.roadmap_id,
                        principalTable: "roadmaps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "roadmap_nodes_skill_id_fkey",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "affiliate_clicks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roadmap_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    redirect_url = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    converted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    commission_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    clicked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("affiliate_clicks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "affiliate_clicks_resource_id_fkey",
                        column: x => x.resource_id,
                        principalTable: "learning_resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "affiliate_clicks_roadmap_node_id_fkey",
                        column: x => x.roadmap_node_id,
                        principalTable: "roadmap_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "affiliate_clicks_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "roadmap_node_prerequisites",
                columns: table => new
                {
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prerequisite_node_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("roadmap_node_prerequisites_pkey", x => new { x.node_id, x.prerequisite_node_id });
                    table.ForeignKey(
                        name: "roadmap_node_prerequisites_node_id_fkey",
                        column: x => x.node_id,
                        principalTable: "roadmap_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "roadmap_node_prerequisites_prerequisite_node_id_fkey",
                        column: x => x.prerequisite_node_id,
                        principalTable: "roadmap_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_admin_actions_admin",
                table: "admin_actions",
                columns: new[] { "admin_user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_affiliate_clicks_resource_id",
                table: "affiliate_clicks",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_affiliate_clicks_roadmap_node_id",
                table: "affiliate_clicks",
                column: "roadmap_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_affiliate_clicks_user_id",
                table: "affiliate_clicks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "assessment_answers_session_id_question_id_key",
                table: "assessment_answers",
                columns: new[] { "session_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assessment_answers_question_id",
                table: "assessment_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "assessment_paths_jd_id_key",
                table: "assessment_paths",
                column: "jd_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_assessment_paths_user",
                table: "assessment_paths",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_questions_session",
                table: "assessment_questions",
                columns: new[] { "session_id", "sequence_order" });

            migrationBuilder.CreateIndex(
                name: "idx_assessment_sessions_current",
                table: "assessment_sessions",
                column: "assessment_path_id",
                unique: true,
                filter: "(is_current = true)");

            migrationBuilder.CreateIndex(
                name: "idx_sessions_path",
                table: "assessment_sessions",
                column: "assessment_path_id");

            migrationBuilder.CreateIndex(
                name: "idx_sessions_reuse",
                table: "assessment_sessions",
                columns: new[] { "user_id", "job_role_category_snapshot" },
                filter: "(((status)::text = 'submitted'::text) AND (is_current = true))");

            migrationBuilder.CreateIndex(
                name: "IX_assessment_sessions_reused_from_session_id",
                table: "assessment_sessions",
                column: "reused_from_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_track_jds_jd_id",
                table: "career_track_jds",
                column: "jd_id");

            migrationBuilder.CreateIndex(
                name: "idx_career_tracks_user",
                table: "career_tracks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "cv_submissions_assessment_path_id_key",
                table: "cv_submissions",
                column: "assessment_path_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cv_user",
                table: "cv_submissions",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_gap_jd",
                table: "gap_analyses",
                column: "jd_id");

            migrationBuilder.CreateIndex(
                name: "idx_gap_jd_latest",
                table: "gap_analyses",
                column: "jd_id",
                unique: true,
                filter: "(is_latest = true)");

            migrationBuilder.CreateIndex(
                name: "idx_gap_user",
                table: "gap_analyses",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_gap_analyses_assessment_path_id",
                table: "gap_analyses",
                column: "assessment_path_id");

            migrationBuilder.CreateIndex(
                name: "idx_gap_skills_ga",
                table: "gap_analysis_skills",
                column: "gap_analysis_id");

            migrationBuilder.CreateIndex(
                name: "IX_gap_analysis_skills_skill_id",
                table: "gap_analysis_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "idx_jd_skills_jd",
                table: "jd_skills",
                column: "jd_id");

            migrationBuilder.CreateIndex(
                name: "idx_jd_skills_skill",
                table: "jd_skills",
                column: "skill_id",
                filter: "(skill_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_jd_parse_status",
                table: "jd_submissions",
                column: "parse_status",
                filter: "((parse_status)::text = ANY ((ARRAY['pending'::character varying, 'processing'::character varying, 'failed'::character varying])::text[]))");

            migrationBuilder.CreateIndex(
                name: "idx_jd_user",
                table: "jd_submissions",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true },
                filter: "(deleted_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_resources_access",
                table: "learning_resources",
                column: "access_type",
                filter: "(is_active = true)");

            migrationBuilder.CreateIndex(
                name: "idx_onboarding_major",
                table: "onboarding_responses",
                column: "major");

            migrationBuilder.CreateIndex(
                name: "onboarding_responses_user_id_key",
                table: "onboarding_responses",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_payment_status",
                table: "payment_orders",
                columns: new[] { "status", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_payment_user",
                table: "payment_orders",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_payment_orders_subscription_id",
                table: "payment_orders",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_orders_tier_id",
                table: "payment_orders",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "payment_orders_provider_order_id_key",
                table: "payment_orders",
                column: "provider_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_certs_user",
                table: "portfolio_certificates",
                column: "user_id",
                filter: "(is_visible = true)");

            migrationBuilder.CreateIndex(
                name: "idx_projects_user",
                table: "portfolio_projects",
                column: "user_id",
                filter: "(is_visible = true)");

            migrationBuilder.CreateIndex(
                name: "idx_rag_chunks_doc",
                table: "rag_chunks",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "idx_rag_chunks_embedding",
                table: "rag_chunks",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "rag_chunks_document_id_chunk_index_key",
                table: "rag_chunks",
                columns: new[] { "document_id", "chunk_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_rag_docs_skills_gin",
                table: "rag_documents",
                column: "related_skill_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_rag_documents_uploaded_by",
                table: "rag_documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "idx_rag_logs_user",
                table: "rag_query_logs",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user",
                table: "refresh_tokens",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "refresh_tokens_token_hash_key",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roadmap_node_prerequisites_prerequisite_node_id",
                table: "roadmap_node_prerequisites",
                column: "prerequisite_node_id");

            migrationBuilder.CreateIndex(
                name: "idx_roadmap_nodes_roadmap",
                table: "roadmap_nodes",
                columns: new[] { "roadmap_id", "sequence_order" });

            migrationBuilder.CreateIndex(
                name: "idx_roadmap_nodes_skill",
                table: "roadmap_nodes",
                column: "skill_id",
                filter: "(skill_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_roadmaps_jd_active",
                table: "roadmaps",
                column: "jd_id",
                unique: true,
                filter: "((status)::text = ANY ((ARRAY['active'::character varying, 'generating'::character varying])::text[]))");

            migrationBuilder.CreateIndex(
                name: "idx_roadmaps_user_status",
                table: "roadmaps",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_roadmaps_gap_analysis_id",
                table: "roadmaps",
                column: "gap_analysis_id");

            migrationBuilder.CreateIndex(
                name: "IX_skill_prerequisites_prerequisite_id",
                table: "skill_prerequisites",
                column: "prerequisite_id");

            migrationBuilder.CreateIndex(
                name: "idx_skill_resources_skill",
                table: "skill_resources",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_skill_resources_resource_id",
                table: "skill_resources",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "idx_skills_name_trgm",
                table: "skills",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "skills_slug_key",
                table: "skills",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_renewal_notifications_user_id",
                table: "subscription_renewal_notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "subscription_renewal_notifica_subscription_id_notification__key",
                table: "subscription_renewal_notifications",
                columns: new[] { "subscription_id", "notification_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "subscription_tiers_tier_code_key",
                table: "subscription_tiers",
                column: "tier_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_subs_active",
                table: "user_subscriptions",
                column: "user_id",
                unique: true,
                filter: "((status)::text = 'active'::text)");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_tier_id",
                table: "user_subscriptions",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "email",
                filter: "(deleted_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_google_sub_key",
                table: "users",
                column: "google_sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_portfolio_url_slug_key",
                table: "users",
                column: "portfolio_url_slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_actions");

            migrationBuilder.DropTable(
                name: "affiliate_clicks");

            migrationBuilder.DropTable(
                name: "assessment_answers");

            migrationBuilder.DropTable(
                name: "career_track_jds");

            migrationBuilder.DropTable(
                name: "cv_submissions");

            migrationBuilder.DropTable(
                name: "gap_analysis_skills");

            migrationBuilder.DropTable(
                name: "jd_skills");

            migrationBuilder.DropTable(
                name: "onboarding_responses");

            migrationBuilder.DropTable(
                name: "payment_orders");

            migrationBuilder.DropTable(
                name: "portfolio_certificates");

            migrationBuilder.DropTable(
                name: "portfolio_projects");

            migrationBuilder.DropTable(
                name: "portfolios");

            migrationBuilder.DropTable(
                name: "rag_chunks");

            migrationBuilder.DropTable(
                name: "rag_query_logs");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "roadmap_node_prerequisites");

            migrationBuilder.DropTable(
                name: "skill_prerequisites");

            migrationBuilder.DropTable(
                name: "skill_resources");

            migrationBuilder.DropTable(
                name: "subscription_renewal_notifications");

            migrationBuilder.DropTable(
                name: "assessment_questions");

            migrationBuilder.DropTable(
                name: "career_tracks");

            migrationBuilder.DropTable(
                name: "rag_documents");

            migrationBuilder.DropTable(
                name: "roadmap_nodes");

            migrationBuilder.DropTable(
                name: "learning_resources");

            migrationBuilder.DropTable(
                name: "user_subscriptions");

            migrationBuilder.DropTable(
                name: "assessment_sessions");

            migrationBuilder.DropTable(
                name: "roadmaps");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "subscription_tiers");

            migrationBuilder.DropTable(
                name: "gap_analyses");

            migrationBuilder.DropTable(
                name: "assessment_paths");

            migrationBuilder.DropTable(
                name: "jd_submissions");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
