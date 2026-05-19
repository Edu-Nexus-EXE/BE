using System;
using System.Collections.Generic;
using Edu_Nexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Edu_Nexus.Domain.Enums.Users;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using Edu_Nexus.Domain.Enums.JdSkills;
using Edu_Nexus.Domain.Enums.AssessmentPaths;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using Edu_Nexus.Domain.Enums.AssessmentQuestions;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using Edu_Nexus.Domain.Enums.GapAnalysisSkills;
using Edu_Nexus.Domain.Enums.Roadmaps;
using Edu_Nexus.Domain.Enums.RoadmapNodes;
using Edu_Nexus.Domain.Enums.LearningResources;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using Edu_Nexus.Domain.Enums.PaymentOrders;
using Edu_Nexus.Domain.Enums.SubscriptionRenewalNotifications;
using Edu_Nexus.Domain.Enums.RagDocuments;

namespace Edu_Nexus.Infrastructure.Data;

public partial class EduNexusDbContext : DbContext
{
    public EduNexusDbContext()
    {
    }

    public EduNexusDbContext(DbContextOptions<EduNexusDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminAction> AdminActions { get; set; }

    public virtual DbSet<AffiliateClick> AffiliateClicks { get; set; }

    public virtual DbSet<AssessmentAnswer> AssessmentAnswers { get; set; }

    public virtual DbSet<AssessmentPath> AssessmentPaths { get; set; }

    public virtual DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }

    public virtual DbSet<AssessmentSession> AssessmentSessions { get; set; }

    public virtual DbSet<CareerTrack> CareerTracks { get; set; }

    public virtual DbSet<CareerTrackJd> CareerTrackJds { get; set; }

    public virtual DbSet<CvSubmission> CvSubmissions { get; set; }

    public virtual DbSet<GapAnalysis> GapAnalyses { get; set; }

    public virtual DbSet<GapAnalysisSkill> GapAnalysisSkills { get; set; }

    public virtual DbSet<JdSkill> JdSkills { get; set; }

    public virtual DbSet<JdSubmission> JdSubmissions { get; set; }

    public virtual DbSet<LearningResource> LearningResources { get; set; }

    public virtual DbSet<OnboardingResponse> OnboardingResponses { get; set; }

    public virtual DbSet<PaymentOrder> PaymentOrders { get; set; }

    public virtual DbSet<Portfolio> Portfolios { get; set; }

    public virtual DbSet<PortfolioCertificate> PortfolioCertificates { get; set; }

    public virtual DbSet<PortfolioProject> PortfolioProjects { get; set; }

    public virtual DbSet<RagChunk> RagChunks { get; set; }

    public virtual DbSet<RagDocument> RagDocuments { get; set; }

    public virtual DbSet<RagQueryLog> RagQueryLogs { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Roadmap> Roadmaps { get; set; }

    public virtual DbSet<RoadmapNode> RoadmapNodes { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<SkillResource> SkillResources { get; set; }

    public virtual DbSet<SubscriptionRenewalNotification> SubscriptionRenewalNotifications { get; set; }

    public virtual DbSet<SubscriptionTier> SubscriptionTiers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("uuid-ossp")
            .HasPostgresExtension("vector");

        modelBuilder.Entity<AdminAction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("admin_actions_pkey");

            entity.ToTable("admin_actions");

            entity.HasIndex(e => new { e.AdminUserId, e.CreatedAt }, "idx_admin_actions_admin").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .HasColumnName("action_type");
            entity.Property(e => e.AdminUserId).HasColumnName("admin_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(50)
                .HasColumnName("target_type");

            entity.HasOne(d => d.AdminUser).WithMany(p => p.AdminActions)
                .HasForeignKey(d => d.AdminUserId)
                .HasConstraintName("admin_actions_admin_user_id_fkey");
        });

        modelBuilder.Entity<AffiliateClick>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("affiliate_clicks_pkey");

            entity.ToTable("affiliate_clicks");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ClickedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("clicked_at");
            entity.Property(e => e.CommissionAmount)
                .HasPrecision(10, 2)
                .HasColumnName("commission_amount");
            entity.Property(e => e.ConvertedAt).HasColumnName("converted_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.RedirectUrl).HasColumnName("redirect_url");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.RoadmapNodeId).HasColumnName("roadmap_node_id");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Resource).WithMany(p => p.AffiliateClicks)
                .HasForeignKey(d => d.ResourceId)
                .HasConstraintName("affiliate_clicks_resource_id_fkey");

            entity.HasOne(d => d.RoadmapNode).WithMany(p => p.AffiliateClicks)
                .HasForeignKey(d => d.RoadmapNodeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("affiliate_clicks_roadmap_node_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.AffiliateClicks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("affiliate_clicks_user_id_fkey");
        });

        modelBuilder.Entity<AssessmentAnswer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("assessment_answers_pkey");

            entity.ToTable("assessment_answers");

            entity.HasIndex(e => new { e.SessionId, e.QuestionId }, "assessment_answers_session_id_question_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AnsweredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("answered_at");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.SelectedOption)
                .HasMaxLength(1)
                .HasColumnName("selected_option")
                .HasConversion(
                    v => v.ToString(),
                    v => (AssessmentOption)Enum.Parse(typeof(AssessmentOption), v));
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Question).WithMany(p => p.AssessmentAnswers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("assessment_answers_question_id_fkey");

            entity.HasOne(d => d.Session).WithMany(p => p.AssessmentAnswers)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("assessment_answers_session_id_fkey");
        });

        modelBuilder.Entity<AssessmentPath>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("assessment_paths_pkey");

            entity.ToTable("assessment_paths");

            entity.HasIndex(e => e.JdId, "assessment_paths_jd_id_key").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_assessment_paths_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.JdId).HasColumnName("jd_id");
            entity.Property(e => e.PathType)
                .HasMaxLength(20)
                .HasColumnName("path_type")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (PathType)Enum.Parse(typeof(PathType), v, true));
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Jd).WithOne(p => p.AssessmentPath)
                .HasForeignKey<AssessmentPath>(d => d.JdId)
                .HasConstraintName("assessment_paths_jd_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.AssessmentPaths)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("assessment_paths_user_id_fkey");
        });

        modelBuilder.Entity<AssessmentQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("assessment_questions_pkey");

            entity.ToTable("assessment_questions");

            entity.HasIndex(e => new { e.SessionId, e.SequenceOrder }, "idx_questions_session");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CorrectOption)
                .HasMaxLength(1)
                .HasColumnName("correct_option")
                .HasConversion(
                    v => v.ToString(),
                    v => (AssessmentOption)Enum.Parse(typeof(AssessmentOption), v));
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.Options)
                .HasColumnType("jsonb")
                .HasColumnName("options");
            entity.Property(e => e.Part)
                .HasColumnName("part")
                .HasConversion(
                    v => (short)v,
                    v => (AssessmentQuestionPart)v);
            entity.Property(e => e.QuestionText).HasColumnName("question_text");
            entity.Property(e => e.RelatedSkill)
                .HasMaxLength(150)
                .HasColumnName("related_skill");
            entity.Property(e => e.SequenceOrder).HasColumnName("sequence_order");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Session).WithMany(p => p.AssessmentQuestions)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("assessment_questions_session_id_fkey");
        });

        modelBuilder.Entity<AssessmentSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("assessment_sessions_pkey");

            entity.ToTable("assessment_sessions");

            entity.HasIndex(e => e.AssessmentPathId, "idx_assessment_sessions_current")
                .IsUnique()
                .HasFilter("(is_current = true)");

            entity.HasIndex(e => e.AssessmentPathId, "idx_sessions_path");

            entity.HasIndex(e => new { e.UserId, e.JobRoleCategorySnapshot }, "idx_sessions_reuse").HasFilter("(((status)::text = 'submitted'::text) AND (is_current = true))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssessmentPathId).HasColumnName("assessment_path_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsCurrent)
                .HasDefaultValue(true)
                .HasColumnName("is_current");
            entity.Property(e => e.JobRoleCategorySnapshot)
                .HasMaxLength(100)
                .HasColumnName("job_role_category_snapshot");
            entity.Property(e => e.Part1Count).HasColumnName("part1_count");
            entity.Property(e => e.Part2Count).HasColumnName("part2_count");
            entity.Property(e => e.ReusedFromSessionId).HasColumnName("reused_from_session_id");
            entity.Property(e => e.SkillScores)
                .HasColumnType("jsonb")
                .HasColumnName("skill_scores");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'in_progress'::character varying")
                .HasColumnName("status")
                .HasConversion(
                    v => v == AssessmentSessionStatus.InProgress ? "in_progress" : v == AssessmentSessionStatus.Submitted ? "submitted" : "expired",
                    v => v == "in_progress" ? AssessmentSessionStatus.InProgress : v == "submitted" ? AssessmentSessionStatus.Submitted : AssessmentSessionStatus.Expired);
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AssessmentPath).WithOne(p => p.AssessmentSession)
                .HasForeignKey<AssessmentSession>(d => d.AssessmentPathId)
                .HasConstraintName("assessment_sessions_assessment_path_id_fkey");

            entity.HasOne(d => d.ReusedFromSession).WithMany(p => p.InverseReusedFromSession)
                .HasForeignKey(d => d.ReusedFromSessionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("assessment_sessions_reused_from_session_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.AssessmentSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("assessment_sessions_user_id_fkey");
        });

        modelBuilder.Entity<CareerTrack>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("career_tracks_pkey");

            entity.ToTable("career_tracks");

            entity.HasIndex(e => e.UserId, "idx_career_tracks_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.CareerTracks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("career_tracks_user_id_fkey");
        });

        modelBuilder.Entity<CareerTrackJd>(entity =>
        {
            entity.HasKey(e => new { e.CareerTrackId, e.JdId }).HasName("career_track_jds_pkey");

            entity.ToTable("career_track_jds");

            entity.Property(e => e.CareerTrackId).HasColumnName("career_track_id");
            entity.Property(e => e.JdId).HasColumnName("jd_id");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("added_at");

            entity.HasOne(d => d.CareerTrack).WithMany(p => p.CareerTrackJds)
                .HasForeignKey(d => d.CareerTrackId)
                .HasConstraintName("career_track_jds_career_track_id_fkey");

            entity.HasOne(d => d.Jd).WithMany(p => p.CareerTrackJds)
                .HasForeignKey(d => d.JdId)
                .HasConstraintName("career_track_jds_jd_id_fkey");
        });

        modelBuilder.Entity<CvSubmission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cv_submissions_pkey");

            entity.ToTable("cv_submissions");

            entity.HasIndex(e => e.AssessmentPathId, "cv_submissions_assessment_path_id_key").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_cv_user").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssessmentPathId).HasColumnName("assessment_path_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(e => e.FileUrl).HasColumnName("file_url");
            entity.Property(e => e.MimeType)
                .HasMaxLength(50)
                .HasColumnName("mime_type");
            entity.Property(e => e.ParseError).HasColumnName("parse_error");
            entity.Property(e => e.ParseStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("parse_status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (ParseStatus)Enum.Parse(typeof(ParseStatus), v, true));
            entity.Property(e => e.ParsedAt).HasColumnName("parsed_at");
            entity.Property(e => e.ParsedSkills)
                .HasColumnType("jsonb")
                .HasColumnName("parsed_skills");
            entity.Property(e => e.ParsedText).HasColumnName("parsed_text");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AssessmentPath).WithOne(p => p.CvSubmission)
                .HasForeignKey<CvSubmission>(d => d.AssessmentPathId)
                .HasConstraintName("cv_submissions_assessment_path_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.CvSubmissions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("cv_submissions_user_id_fkey");
        });

        modelBuilder.Entity<GapAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("gap_analyses_pkey");

            entity.ToTable("gap_analyses");

            entity.HasIndex(e => e.JdId, "idx_gap_jd");

            entity.HasIndex(e => e.JdId, "idx_gap_jd_latest")
                .IsUnique()
                .HasFilter("(is_latest = true)");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_gap_user").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssessmentPathId).HasColumnName("assessment_path_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Error).HasColumnName("error");
            entity.Property(e => e.InputSource)
                .HasMaxLength(20)
                .HasColumnName("input_source")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (GapAnalysisInputSource)Enum.Parse(typeof(GapAnalysisInputSource), v, true));
            entity.Property(e => e.IsLatest)
                .HasDefaultValue(true)
                .HasColumnName("is_latest");
            entity.Property(e => e.JdId).HasColumnName("jd_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (GapAnalysisStatus)Enum.Parse(typeof(GapAnalysisStatus), v, true));
            entity.Property(e => e.Summary)
                .HasColumnType("jsonb")
                .HasColumnName("summary");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Version)
                .HasDefaultValue((short)1)
                .HasColumnName("version");

            entity.HasOne(d => d.AssessmentPath).WithMany(p => p.GapAnalyses)
                .HasForeignKey(d => d.AssessmentPathId)
                .HasConstraintName("gap_analyses_assessment_path_id_fkey");

            entity.HasOne(d => d.Jd).WithOne(p => p.GapAnalysis)
                .HasForeignKey<GapAnalysis>(d => d.JdId)
                .HasConstraintName("gap_analyses_jd_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.GapAnalyses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("gap_analyses_user_id_fkey");
        });

        modelBuilder.Entity<GapAnalysisSkill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("gap_analysis_skills_pkey");

            entity.ToTable("gap_analysis_skills");

            entity.HasIndex(e => e.GapAnalysisId, "idx_gap_skills_ga");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CurrentLevel)
                .HasMaxLength(20)
                .HasColumnName("current_level")
                .HasConversion(
                    v => v.HasValue ? v.Value.ToString().ToLower() : null,
                    v => !string.IsNullOrEmpty(v) ? (SkillLevel)Enum.Parse(typeof(SkillLevel), v, true) : null);
            entity.Property(e => e.GapAnalysisId).HasColumnName("gap_analysis_id");
            entity.Property(e => e.GapStatus)
                .HasMaxLength(20)
                .HasColumnName("gap_status")
                .HasConversion(
                    v => v == GapStatus.Missing ? "missing" : v == GapStatus.Have ? "have" : "needs_upgrade",
                    v => v == "missing" ? GapStatus.Missing : v == "have" ? GapStatus.Have : GapStatus.NeedsUpgrade);
            entity.Property(e => e.IsMandatoryInJd)
                .HasDefaultValue(true)
                .HasColumnName("is_mandatory_in_jd");
            entity.Property(e => e.Reasoning).HasColumnName("reasoning");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.SkillName)
                .HasMaxLength(150)
                .HasColumnName("skill_name");
            entity.Property(e => e.TargetLevel)
                .HasMaxLength(20)
                .HasColumnName("target_level")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (SkillLevel)Enum.Parse(typeof(SkillLevel), v, true));
            entity.Property(e => e.UrgencyScore).HasColumnName("urgency_score");

            entity.HasOne(d => d.GapAnalysis).WithMany(p => p.GapAnalysisSkills)
                .HasForeignKey(d => d.GapAnalysisId)
                .HasConstraintName("gap_analysis_skills_gap_analysis_id_fkey");

            entity.HasOne(d => d.Skill).WithMany(p => p.GapAnalysisSkills)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("gap_analysis_skills_skill_id_fkey");
        });

        modelBuilder.Entity<JdSkill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("jd_skills_pkey");

            entity.ToTable("jd_skills");

            entity.HasIndex(e => e.JdId, "idx_jd_skills_jd");

            entity.HasIndex(e => e.SkillId, "idx_jd_skills_skill").HasFilter("(skill_id IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsMandatory)
                .HasDefaultValue(true)
                .HasColumnName("is_mandatory");
            entity.Property(e => e.JdId).HasColumnName("jd_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.SkillNameRaw)
                .HasMaxLength(150)
                .HasColumnName("skill_name_raw");
            entity.Property(e => e.SkillType)
                .HasMaxLength(20)
                .HasColumnName("skill_type")
                .HasConversion(
                    v => v == SkillType.HardSkill ? "hard_skill" : "soft_skill",
                    v => v == "hard_skill" ? SkillType.HardSkill : SkillType.SoftSkill);

            entity.HasOne(d => d.Jd).WithMany(p => p.JdSkills)
                .HasForeignKey(d => d.JdId)
                .HasConstraintName("jd_skills_jd_id_fkey");

            entity.HasOne(d => d.Skill).WithMany(p => p.JdSkills)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("jd_skills_skill_id_fkey");
        });

        modelBuilder.Entity<JdSubmission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("jd_submissions_pkey");

            entity.ToTable("jd_submissions");

            entity.HasIndex(e => e.ParseStatus, "idx_jd_parse_status").HasFilter("((parse_status)::text = ANY ((ARRAY['pending'::character varying, 'processing'::character varying, 'failed'::character varying])::text[]))");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_jd_user")
                .IsDescending(false, true)
                .HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.JobRoleCategory)
                .HasMaxLength(100)
                .HasColumnName("job_role_category");
            entity.Property(e => e.JobTitle)
                .HasMaxLength(255)
                .HasColumnName("job_title");
            entity.Property(e => e.ParseError).HasColumnName("parse_error");
            entity.Property(e => e.ParseStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("parse_status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (ParseStatus)Enum.Parse(typeof(ParseStatus), v, true));
            entity.Property(e => e.ParsedAt).HasColumnName("parsed_at");
            entity.Property(e => e.RawContent).HasColumnName("raw_content");
            entity.Property(e => e.SalaryMax).HasColumnName("salary_max");
            entity.Property(e => e.SalaryMin).HasColumnName("salary_min");
            entity.Property(e => e.SeniorityLevel)
                .HasMaxLength(50)
                .HasColumnName("seniority_level");
            entity.Property(e => e.SourceType)
                .HasMaxLength(20)
                .HasColumnName("source_type")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (JdSourceType)Enum.Parse(typeof(JdSourceType), v, true));
            entity.Property(e => e.SourceUrl).HasColumnName("source_url");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.JdSubmissions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("jd_submissions_user_id_fkey");
        });

        modelBuilder.Entity<LearningResource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("learning_resources_pkey");

            entity.ToTable("learning_resources");

            entity.HasIndex(e => e.AccessType, "idx_resources_access").HasFilter("(is_active = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccessType)
                .HasMaxLength(30)
                .HasColumnName("access_type")
                .HasConversion(
                    v => v == LearningResourceAccessType.FptuInternal ? "fptu_internal" :
                         v == LearningResourceAccessType.PartnershipPremium ? "partnership_premium" :
                         v == LearningResourceAccessType.PartnershipSubscription ? "partnership_subscription" :
                         v.ToString().ToLower(),
                    v => v == "fptu_internal" ? LearningResourceAccessType.FptuInternal :
                         v == "partnership_premium" ? LearningResourceAccessType.PartnershipPremium :
                         v == "partnership_subscription" ? LearningResourceAccessType.PartnershipSubscription :
                         (LearningResourceAccessType)Enum.Parse(typeof(LearningResourceAccessType), v, true));
            entity.Property(e => e.AffiliateCommissionRate)
                .HasPrecision(5, 2)
                .HasColumnName("affiliate_commission_rate");
            entity.Property(e => e.AffiliateLabel)
                .HasMaxLength(100)
                .HasColumnName("affiliate_label");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsFree)
                .HasDefaultValue(true)
                .HasColumnName("is_free");
            entity.Property(e => e.Language)
                .HasMaxLength(5)
                .HasDefaultValueSql("'vi'::character varying")
                .HasColumnName("language");
            entity.Property(e => e.NeedsAdminReview)
                .HasDefaultValue(false)
                .HasColumnName("needs_admin_review");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.Provider)
                .HasMaxLength(100)
                .HasColumnName("provider");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type")
                .HasConversion(
                    v => v == LearningResourceType.FptuInternal ? "fptu_internal" : v.ToString().ToLower(),
                    v => v == "fptu_internal" ? LearningResourceType.FptuInternal : (LearningResourceType)Enum.Parse(typeof(LearningResourceType), v, true));
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Url).HasColumnName("url");
        });

        modelBuilder.Entity<OnboardingResponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("onboarding_responses_pkey");

            entity.ToTable("onboarding_responses");

            entity.HasIndex(e => e.Major, "idx_onboarding_major");

            entity.HasIndex(e => e.UserId, "onboarding_responses_user_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AcademicYear)
                .HasMaxLength(20)
                .HasColumnName("academic_year");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LearningBudget)
                .HasMaxLength(30)
                .HasColumnName("learning_budget");
            entity.Property(e => e.LearningPriority)
                .HasMaxLength(50)
                .HasColumnName("learning_priority");
            entity.Property(e => e.Major)
                .HasMaxLength(50)
                .HasColumnName("major");
            entity.Property(e => e.PreferredChannel)
                .HasMaxLength(50)
                .HasColumnName("preferred_channel");
            entity.Property(e => e.PrimaryGoal)
                .HasMaxLength(100)
                .HasColumnName("primary_goal");
            entity.Property(e => e.ProficiencyLevel)
                .HasMaxLength(50)
                .HasColumnName("proficiency_level");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WeeklyStudyHours)
                .HasMaxLength(20)
                .HasColumnName("weekly_study_hours");

            entity.HasOne(d => d.User).WithOne(p => p.OnboardingResponse)
                .HasForeignKey<OnboardingResponse>(d => d.UserId)
                .HasConstraintName("onboarding_responses_user_id_fkey");
        });

        modelBuilder.Entity<PaymentOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payment_orders_pkey");

            entity.ToTable("payment_orders");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "idx_payment_status").IsDescending(false, true);

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_payment_user").IsDescending(false, true);

            entity.HasIndex(e => e.ProviderOrderId, "payment_orders_provider_order_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'VND'::bpchar")
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.DurationMonths).HasColumnName("duration_months");
            entity.Property(e => e.PaymentProvider)
                .HasMaxLength(20)
                .HasColumnName("payment_provider")
                .HasConversion(
                    v => v == PaymentProvider.ManualTransfer ? "manual_transfer" : v.ToString().ToLower(),
                    v => v == "manual_transfer" ? PaymentProvider.ManualTransfer : (PaymentProvider)Enum.Parse(typeof(PaymentProvider), v, true));
            entity.Property(e => e.ProviderOrderId)
                .HasMaxLength(255)
                .HasColumnName("provider_order_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (PaymentOrderStatus)Enum.Parse(typeof(PaymentOrderStatus), v, true));
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.TierId).HasColumnName("tier_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Subscription).WithMany(p => p.PaymentOrders)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("payment_orders_subscription_id_fkey");

            entity.HasOne(d => d.Tier).WithMany(p => p.PaymentOrders)
                .HasForeignKey(d => d.TierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payment_orders_tier_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentOrders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("payment_orders_user_id_fkey");
        });

        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("portfolios_pkey");

            entity.ToTable("portfolios");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.CoverImageUrl).HasColumnName("cover_image_url");
            entity.Property(e => e.Headline)
                .HasMaxLength(255)
                .HasColumnName("headline");
            entity.Property(e => e.IsPublic)
                .HasDefaultValue(true)
                .HasColumnName("is_public");
            entity.Property(e => e.ShowCertificates)
                .HasDefaultValue(true)
                .HasColumnName("show_certificates");
            entity.Property(e => e.ShowCompletedSkills)
                .HasDefaultValue(true)
                .HasColumnName("show_completed_skills");
            entity.Property(e => e.ShowProjects)
                .HasDefaultValue(true)
                .HasColumnName("show_projects");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne(p => p.Portfolio)
                .HasForeignKey<Portfolio>(d => d.UserId)
                .HasConstraintName("portfolios_user_id_fkey");
        });

        modelBuilder.Entity<PortfolioCertificate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("portfolio_certificates_pkey");

            entity.ToTable("portfolio_certificates");

            entity.HasIndex(e => e.UserId, "idx_certs_user").HasFilter("(is_visible = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CredentialUrl).HasColumnName("credential_url");
            entity.Property(e => e.ExpiresDate).HasColumnName("expires_date");
            entity.Property(e => e.FileUrl).HasColumnName("file_url");
            entity.Property(e => e.IsVisible)
                .HasDefaultValue(true)
                .HasColumnName("is_visible");
            entity.Property(e => e.IssuedDate).HasColumnName("issued_date");
            entity.Property(e => e.Issuer)
                .HasMaxLength(255)
                .HasColumnName("issuer");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PortfolioCertificates)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("portfolio_certificates_user_id_fkey");
        });

        modelBuilder.Entity<PortfolioProject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("portfolio_projects_pkey");

            entity.ToTable("portfolio_projects");

            entity.HasIndex(e => e.UserId, "idx_projects_user").HasFilter("(is_visible = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompletedDate).HasColumnName("completed_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.IsVisible)
                .HasDefaultValue(true)
                .HasColumnName("is_visible");
            entity.Property(e => e.LiveUrl).HasColumnName("live_url");
            entity.Property(e => e.RepoUrl).HasColumnName("repo_url");
            entity.Property(e => e.Role)
                .HasMaxLength(100)
                .HasColumnName("role");
            entity.Property(e => e.StartedDate).HasColumnName("started_date");
            entity.Property(e => e.TechStack)
                .HasColumnType("jsonb")
                .HasColumnName("tech_stack");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PortfolioProjects)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("portfolio_projects_user_id_fkey");
        });

        modelBuilder.Entity<RagChunk>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rag_chunks_pkey");

            entity.ToTable("rag_chunks");

            entity.HasIndex(e => e.DocumentId, "idx_rag_chunks_doc");

            entity.HasIndex(e => e.Embedding, "idx_rag_chunks_embedding")
                .HasMethod("hnsw")
                .HasOperators(new[] { "vector_cosine_ops" });

            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex }, "rag_chunks_document_id_chunk_index_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.Embedding)
                .HasMaxLength(1536)
                .HasColumnName("embedding");
            entity.Property(e => e.TokenCount).HasColumnName("token_count");

            entity.HasOne(d => d.Document).WithMany(p => p.RagChunks)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("rag_chunks_document_id_fkey");
        });

        modelBuilder.Entity<RagDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rag_documents_pkey");

            entity.ToTable("rag_documents");

            entity.HasIndex(e => e.RelatedSkillIds, "idx_rag_docs_skills_gin").HasMethod("gin");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChunksCount)
                .HasDefaultValue(0)
                .HasColumnName("chunks_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EmbeddingStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("embedding_status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (EmbeddingStatus)Enum.Parse(typeof(EmbeddingStatus), v, true));
            entity.Property(e => e.FileUrl).HasColumnName("file_url");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.RelatedSkillIds)
                .HasDefaultValueSql("'{}'::uuid[]")
                .HasColumnName("related_skill_ids");
            entity.Property(e => e.SourceType)
                .HasMaxLength(50)
                .HasColumnName("source_type")
                .HasConversion(
                    v => v == RagDocumentSourceType.FptuCurriculum ? "fptu_curriculum" :
                         v == RagDocumentSourceType.FptuSyllabus ? "fptu_syllabus" : "external_doc",
                    v => v == "fptu_curriculum" ? RagDocumentSourceType.FptuCurriculum :
                         v == "fptu_syllabus" ? RagDocumentSourceType.FptuSyllabus : RagDocumentSourceType.ExternalDoc);
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.RagDocuments)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("rag_documents_uploaded_by_fkey");
        });

        modelBuilder.Entity<RagQueryLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rag_query_logs_pkey");

            entity.ToTable("rag_query_logs");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_rag_logs_user").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompletionTokens).HasColumnName("completion_tokens");
            entity.Property(e => e.CostUsd)
                .HasPrecision(10, 6)
                .HasColumnName("cost_usd");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.ModelUsed)
                .HasMaxLength(50)
                .HasColumnName("model_used");
            entity.Property(e => e.PromptTokens).HasColumnName("prompt_tokens");
            entity.Property(e => e.QueryType)
                .HasMaxLength(50)
                .HasColumnName("query_type");
            entity.Property(e => e.Success)
                .HasDefaultValue(true)
                .HasColumnName("success");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RagQueryLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("rag_query_logs_user_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => new { e.UserId, e.RevokedAt }, "idx_refresh_tokens_user");

            entity.HasIndex(e => e.TokenHash, "refresh_tokens_token_hash_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Roadmap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roadmaps_pkey");

            entity.ToTable("roadmaps");

            entity.HasIndex(e => e.JdId, "idx_roadmaps_jd_active")
                .IsUnique()
                .HasFilter("((status)::text = ANY ((ARRAY['active'::character varying, 'generating'::character varying])::text[]))");

            entity.HasIndex(e => new { e.UserId, e.Status }, "idx_roadmaps_user_status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EstimatedTotalHours).HasColumnName("estimated_total_hours");
            entity.Property(e => e.GapAnalysisId).HasColumnName("gap_analysis_id");
            entity.Property(e => e.IsOutdated)
                .HasDefaultValue(false)
                .HasColumnName("is_outdated");
            entity.Property(e => e.JdId).HasColumnName("jd_id");
            entity.Property(e => e.ProgressPercent)
                .HasDefaultValue((short)0)
                .HasColumnName("progress_percent");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'generating'::character varying")
                .HasColumnName("status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (RoadmapStatus)Enum.Parse(typeof(RoadmapStatus), v, true));
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.GapAnalysis).WithMany(p => p.Roadmaps)
                .HasForeignKey(d => d.GapAnalysisId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("roadmaps_gap_analysis_id_fkey");

            entity.HasOne(d => d.Jd).WithOne(p => p.Roadmap)
                .HasForeignKey<Roadmap>(d => d.JdId)
                .HasConstraintName("roadmaps_jd_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Roadmaps)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("roadmaps_user_id_fkey");
        });

        modelBuilder.Entity<RoadmapNode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roadmap_nodes_pkey");

            entity.ToTable("roadmap_nodes");

            entity.HasIndex(e => new { e.RoadmapId, e.SequenceOrder }, "idx_roadmap_nodes_roadmap");

            entity.HasIndex(e => e.SkillId, "idx_roadmap_nodes_skill").HasFilter("(skill_id IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EstimatedHours).HasColumnName("estimated_hours");
            entity.Property(e => e.IsPrerequisite)
                .HasDefaultValue(false)
                .HasColumnName("is_prerequisite");
            entity.Property(e => e.RoadmapId).HasColumnName("roadmap_id");
            entity.Property(e => e.SequenceOrder).HasColumnName("sequence_order");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.SkillName)
                .HasMaxLength(150)
                .HasColumnName("skill_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'not_started'::character varying")
                .HasColumnName("status")
                .HasConversion(
                    v => v == RoadmapNodeStatus.NotStarted ? "not_started" : v == RoadmapNodeStatus.InProgress ? "in_progress" : "completed",
                    v => v == "not_started" ? RoadmapNodeStatus.NotStarted : v == "in_progress" ? RoadmapNodeStatus.InProgress : RoadmapNodeStatus.Completed);

            entity.HasOne(d => d.Roadmap).WithMany(p => p.RoadmapNodes)
                .HasForeignKey(d => d.RoadmapId)
                .HasConstraintName("roadmap_nodes_roadmap_id_fkey");

            entity.HasOne(d => d.Skill).WithMany(p => p.RoadmapNodes)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("roadmap_nodes_skill_id_fkey");

            entity.HasMany(d => d.Nodes).WithMany(p => p.PrerequisiteNodes)
                .UsingEntity<Dictionary<string, object>>(
                    "RoadmapNodePrerequisite",
                    r => r.HasOne<RoadmapNode>().WithMany()
                        .HasForeignKey("NodeId")
                        .HasConstraintName("roadmap_node_prerequisites_node_id_fkey"),
                    l => l.HasOne<RoadmapNode>().WithMany()
                        .HasForeignKey("PrerequisiteNodeId")
                        .HasConstraintName("roadmap_node_prerequisites_prerequisite_node_id_fkey"),
                    j =>
                    {
                        j.HasKey("NodeId", "PrerequisiteNodeId").HasName("roadmap_node_prerequisites_pkey");
                        j.ToTable("roadmap_node_prerequisites");
                        j.IndexerProperty<Guid>("NodeId").HasColumnName("node_id");
                        j.IndexerProperty<Guid>("PrerequisiteNodeId").HasColumnName("prerequisite_node_id");
                    });

            entity.HasMany(d => d.PrerequisiteNodes).WithMany(p => p.Nodes)
                .UsingEntity<Dictionary<string, object>>(
                    "RoadmapNodePrerequisite",
                    r => r.HasOne<RoadmapNode>().WithMany()
                        .HasForeignKey("PrerequisiteNodeId")
                        .HasConstraintName("roadmap_node_prerequisites_prerequisite_node_id_fkey"),
                    l => l.HasOne<RoadmapNode>().WithMany()
                        .HasForeignKey("NodeId")
                        .HasConstraintName("roadmap_node_prerequisites_node_id_fkey"),
                    j =>
                    {
                        j.HasKey("NodeId", "PrerequisiteNodeId").HasName("roadmap_node_prerequisites_pkey");
                        j.ToTable("roadmap_node_prerequisites");
                        j.IndexerProperty<Guid>("NodeId").HasColumnName("node_id");
                        j.IndexerProperty<Guid>("PrerequisiteNodeId").HasColumnName("prerequisite_node_id");
                    });
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("skills_pkey");

            entity.ToTable("skills");

            entity.HasIndex(e => e.Name, "idx_skills_name_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" });

            entity.HasIndex(e => e.Slug, "skills_slug_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DifficultyLevel)
                .HasDefaultValue((short)1)
                .HasColumnName("difficulty_level");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Major)
                .HasMaxLength(50)
                .HasColumnName("major");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Slug)
                .HasMaxLength(150)
                .HasColumnName("slug");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasMany(d => d.Prerequisites).WithMany(p => p.Skills)
                .UsingEntity<Dictionary<string, object>>(
                    "SkillPrerequisite",
                    r => r.HasOne<Skill>().WithMany()
                        .HasForeignKey("PrerequisiteId")
                        .HasConstraintName("skill_prerequisites_prerequisite_id_fkey"),
                    l => l.HasOne<Skill>().WithMany()
                        .HasForeignKey("SkillId")
                        .HasConstraintName("skill_prerequisites_skill_id_fkey"),
                    j =>
                    {
                        j.HasKey("SkillId", "PrerequisiteId").HasName("skill_prerequisites_pkey");
                        j.ToTable("skill_prerequisites");
                        j.IndexerProperty<Guid>("SkillId").HasColumnName("skill_id");
                        j.IndexerProperty<Guid>("PrerequisiteId").HasColumnName("prerequisite_id");
                    });

            entity.HasMany(d => d.Skills).WithMany(p => p.Prerequisites)
                .UsingEntity<Dictionary<string, object>>(
                    "SkillPrerequisite",
                    r => r.HasOne<Skill>().WithMany()
                        .HasForeignKey("SkillId")
                        .HasConstraintName("skill_prerequisites_skill_id_fkey"),
                    l => l.HasOne<Skill>().WithMany()
                        .HasForeignKey("PrerequisiteId")
                        .HasConstraintName("skill_prerequisites_prerequisite_id_fkey"),
                    j =>
                    {
                        j.HasKey("SkillId", "PrerequisiteId").HasName("skill_prerequisites_pkey");
                        j.ToTable("skill_prerequisites");
                        j.IndexerProperty<Guid>("SkillId").HasColumnName("skill_id");
                        j.IndexerProperty<Guid>("PrerequisiteId").HasColumnName("prerequisite_id");
                    });
        });

        modelBuilder.Entity<SkillResource>(entity =>
        {
            entity.HasKey(e => new { e.SkillId, e.ResourceId }).HasName("skill_resources_pkey");

            entity.ToTable("skill_resources");

            entity.HasIndex(e => e.SkillId, "idx_skill_resources_skill");

            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");
            entity.Property(e => e.SequenceOrder).HasColumnName("sequence_order");

            entity.HasOne(d => d.Resource).WithMany(p => p.SkillResources)
                .HasForeignKey(d => d.ResourceId)
                .HasConstraintName("skill_resources_resource_id_fkey");

            entity.HasOne(d => d.Skill).WithMany(p => p.SkillResources)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("skill_resources_skill_id_fkey");
        });

        modelBuilder.Entity<SubscriptionRenewalNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscription_renewal_notifications_pkey");

            entity.ToTable("subscription_renewal_notifications");

            entity.HasIndex(e => new { e.SubscriptionId, e.NotificationType }, "subscription_renewal_notifica_subscription_id_notification__key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(20)
                .HasColumnName("notification_type")
                .HasConversion(
                    v => v == SubscriptionRenewalNotificationType.Renewal7d ? "renewal_7d" :
                         v == SubscriptionRenewalNotificationType.Renewal3d ? "renewal_3d" : "renewal_0d",
                    v => v == "renewal_7d" ? SubscriptionRenewalNotificationType.Renewal7d :
                         v == "renewal_3d" ? SubscriptionRenewalNotificationType.Renewal3d : SubscriptionRenewalNotificationType.Renewal0d);
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("sent_at");
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Subscription).WithMany(p => p.SubscriptionRenewalNotifications)
                .HasForeignKey(d => d.SubscriptionId)
                .HasConstraintName("subscription_renewal_notifications_subscription_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.SubscriptionRenewalNotifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("subscription_renewal_notifications_user_id_fkey");
        });

        modelBuilder.Entity<SubscriptionTier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscription_tiers_pkey");

            entity.ToTable("subscription_tiers");

            entity.HasIndex(e => e.TierCode, "subscription_tiers_tier_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssessmentQuota).HasColumnName("assessment_quota");
            entity.Property(e => e.CareerTrackQuota).HasColumnName("career_track_quota");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'VND'::bpchar")
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(50)
                .HasColumnName("display_name");
            entity.Property(e => e.FullGapHistory)
                .HasDefaultValue(false)
                .HasColumnName("full_gap_history");
            entity.Property(e => e.GapAnalysisQuota).HasColumnName("gap_analysis_quota");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.JdQuota).HasColumnName("jd_quota");
            entity.Property(e => e.PortfolioCertificateQuota).HasColumnName("portfolio_certificate_quota");
            entity.Property(e => e.PortfolioProjectQuota).HasColumnName("portfolio_project_quota");
            entity.Property(e => e.PriceMonthly)
                .HasPrecision(10, 2)
                .HasColumnName("price_monthly");
            entity.Property(e => e.RoadmapActiveQuota).HasColumnName("roadmap_active_quota");
            entity.Property(e => e.TierCode)
                .HasMaxLength(20)
                .HasColumnName("tier_code")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (SubscriptionTierCode)Enum.Parse(typeof(SubscriptionTierCode), v, true));
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "idx_users_email").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.GoogleSub, "users_google_sub_key").IsUnique();

            entity.HasIndex(e => e.PortfolioUrlSlug, "users_portfolio_url_slug_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AuthProvider)
                .HasMaxLength(20)
                .HasDefaultValueSql("'email'::character varying")
                .HasColumnName("auth_provider")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (AuthProvider)Enum.Parse(typeof(AuthProvider), v, true));
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.GoogleSub)
                .HasMaxLength(255)
                .HasColumnName("google_sub");
            entity.Property(e => e.IsBanned)
                .HasDefaultValue(false)
                .HasColumnName("is_banned");
            entity.Property(e => e.IsSurveyCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_survey_completed");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PortfolioUrlSlug)
                .HasMaxLength(100)
                .HasColumnName("portfolio_url_slug");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValueSql("'user'::character varying")
                .HasColumnName("role")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (UserRole)Enum.Parse(typeof(UserRole), v, true));
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_subscriptions_pkey");

            entity.ToTable("user_subscriptions");

            entity.HasIndex(e => e.UserId, "idx_user_subs_active")
                .IsUnique()
                .HasFilter("((status)::text = 'active'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AutoRenew)
                .HasDefaultValue(false)
                .HasColumnName("auto_renew");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => (UserSubscriptionStatus)Enum.Parse(typeof(UserSubscriptionStatus), v, true));
            entity.Property(e => e.TierId).HasColumnName("tier_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Tier).WithMany(p => p.UserSubscriptions)
                .HasForeignKey(d => d.TierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_subscriptions_tier_id_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.UserSubscription)
                .HasForeignKey<UserSubscription>(d => d.UserId)
                .HasConstraintName("user_subscriptions_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
