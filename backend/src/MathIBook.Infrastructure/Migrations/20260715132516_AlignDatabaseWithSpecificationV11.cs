using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathIBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignDatabaseWithSpecificationV11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Chapters_ChapterId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_Progresses_Lessons_LessonId",
                table: "Progresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Lessons_LessonId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttemptAnswers_Questions_QuestionId",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Lessons_LessonId",
                table: "QuizAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_UserId",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttemptAnswers_AttemptId",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropIndex(
                name: "IX_Questions_LessonId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_ChapterId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_OrderIndex",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_CoinTransactions_UserId",
                table: "CoinTransactions");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CoinsUpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "BadgeRuleId",
                table: "UserBadges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "UserBadges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LessonId",
                table: "QuizAttempts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientAttemptId",
                table: "QuizAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoinsEarned",
                table: "QuizAttempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPassed",
                table: "QuizAttempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "QuizId",
                table: "QuizAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RewardProcessedAt",
                table: "QuizAttempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Score10",
                table: "QuizAttempts",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "QuizAttempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LessonId",
                table: "Questions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ChapterId",
                table: "Questions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Questions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "BestScore10",
                table: "Progresses",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ContentViewed",
                table: "Progresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastViewedAt",
                table: "Progresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Progresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Progresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedEntityId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ContentVersion",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Lessons",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumTopicId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Lessons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Lessons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Lessons",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "BalanceAfter",
                table: "CoinTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientAttemptId",
                table: "CoinTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "CoinTransactions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RewardPolicyId",
                table: "CoinTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Chapters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumTopicId",
                table: "Chapters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Chapters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Chapters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Chapters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Chapters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Badges",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RewardCoins",
                table: "Badges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RuleMatchMode",
                table: "Badges",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Badges",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ChapterProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BestScore10 = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    QuizUnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstPassedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClientUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterProgresses", x => x.Id);
                    table.CheckConstraint("CK_ChapterProgresses_BestScore10", "\"BestScore10\" BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "FK_ChapterProgresses_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChapterProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BeforeData = table.Column<string>(type: "jsonb", nullable: true),
                    AfterData = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentAuditLogs_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Strand = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumTopics", x => x.Id);
                    table.CheckConstraint("CK_CurriculumTopics_Grade", "\"Grade\" = 8");
                });

            migrationBuilder.CreateTable(
                name: "RewardPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    QuizType = table.Column<int>(type: "integer", nullable: false),
                    CoinsPerCorrectAnswer = table.Column<int>(type: "integer", nullable: false),
                    FirstPassBonusCoins = table.Column<int>(type: "integer", nullable: false),
                    PerfectScoreBonusCoins = table.Column<int>(type: "integer", nullable: false),
                    ChapterCompletionBonusCoins = table.Column<int>(type: "integer", nullable: false),
                    RetryRewardPercent = table.Column<int>(type: "integer", nullable: false),
                    DailyCoinLimit = table.Column<int>(type: "integer", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardPolicies", x => x.Id);
                    table.CheckConstraint("CK_RewardPolicies_EffectiveRange", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
                    table.CheckConstraint("CK_RewardPolicies_NonNegative", "\"CoinsPerCorrectAnswer\" >= 0 AND \"FirstPassBonusCoins\" >= 0 AND \"PerfectScoreBonusCoins\" >= 0 AND \"ChapterCompletionBonusCoins\" >= 0 AND (\"DailyCoinLimit\" IS NULL OR \"DailyCoinLimit\" >= 0)");
                    table.CheckConstraint("CK_RewardPolicies_RetryPercent", "\"RetryRewardPercent\" BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizType = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: true),
                    RewardPolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    PassScore = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    FirstPassCoins = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                    table.CheckConstraint("CK_Quizzes_Duration", "\"DurationSeconds\" > 0");
                    table.CheckConstraint("CK_Quizzes_FirstPassCoins", "\"FirstPassCoins\" >= 0");
                    table.CheckConstraint("CK_Quizzes_PassScore", "\"PassScore\" BETWEEN 0 AND 10");
                    table.CheckConstraint("CK_Quizzes_Target", "(\"QuizType\" = 1 AND \"LessonId\" IS NOT NULL AND \"ChapterId\" IS NULL) OR (\"QuizType\" = 2 AND \"ChapterId\" IS NOT NULL AND \"LessonId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_Quizzes_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quizzes_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quizzes_RewardPolicies_RewardPolicyId",
                        column: x => x.RewardPolicyId,
                        principalTable: "RewardPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BadgeRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetChapterId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetQuizId = table.Column<Guid>(type: "uuid", nullable: true),
                    ThresholdValue = table.Column<int>(type: "integer", nullable: true),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Parameters = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeRules", x => x.Id);
                    table.CheckConstraint("CK_BadgeRules_Threshold", "\"ThresholdValue\" IS NULL OR \"ThresholdValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_BadgeRules_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BadgeRules_Chapters_TargetChapterId",
                        column: x => x.TargetChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BadgeRules_Quizzes_TargetQuizId",
                        column: x => x.TargetQuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.Id);
                    table.CheckConstraint("CK_QuizQuestions_Weight", "\"Weight\" > 0");
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Backfill and normalize legacy data before enabling the new constraints.
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "Role" = CASE WHEN LOWER("Role") = 'admin' THEN 'Admin' ELSE 'Student' END,
                    "Coins" = GREATEST("Coins", 0),
                    "IsActive" = TRUE,
                    "UpdatedAt" = COALESCE("CreatedAt", CURRENT_TIMESTAMP);

                UPDATE "Lessons"
                SET "ContentVersion" = 1,
                    "CreatedAt" = CURRENT_TIMESTAMP,
                    "UpdatedAt" = CURRENT_TIMESTAMP,
                    "PublishedAt" = CASE WHEN "IsPublished" THEN CURRENT_TIMESTAMP ELSE NULL END;

                UPDATE "Chapters" c
                SET "CreatedAt" = CURRENT_TIMESTAMP,
                    "UpdatedAt" = CURRENT_TIMESTAMP,
                    "IsPublished" = EXISTS (
                        SELECT 1
                        FROM "Lessons" l
                        WHERE l."ChapterId" = c."Id" AND l."IsPublished" = TRUE
                    ),
                    "PublishedAt" = CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM "Lessons" l
                            WHERE l."ChapterId" = c."Id" AND l."IsPublished" = TRUE
                        ) THEN CURRENT_TIMESTAMP
                        ELSE NULL
                    END;

                UPDATE "Questions"
                SET "CreatedAt" = CURRENT_TIMESTAMP,
                    "UpdatedAt" = CURRENT_TIMESTAMP;

                UPDATE "Badges"
                SET "RuleMatchMode" = 'ALL',
                    "IsActive" = TRUE,
                    "CreatedAt" = CURRENT_TIMESTAMP,
                    "UpdatedAt" = CURRENT_TIMESTAMP;

                UPDATE "Notifications"
                SET "Type" = 'system';

                INSERT INTO "CurriculumTopics" (
                    "Id", "Code", "Name", "Strand", "Grade",
                    "OrderIndex", "IsActive", "CreatedAt")
                VALUES (
                    '8a7f1bd8-6bb0-4ee0-a6af-08a3bfb8aa08',
                    'M8-LEGACY',
                    'Nội dung Toán lớp 8 hiện có',
                    1, 8, 0, TRUE, CURRENT_TIMESTAMP)


                UPDATE "Chapters"
                SET "CurriculumTopicId" = (
                    SELECT "Id" FROM "CurriculumTopics"
                    WHERE "Code" = 'M8-LEGACY'
                    LIMIT 1)
                WHERE "CurriculumTopicId" IS NULL;

                UPDATE "Lessons"
                SET "CurriculumTopicId" = COALESCE(
                    (SELECT c."CurriculumTopicId"
                     FROM "Chapters" c
                     WHERE c."Id" = "Lessons"."ChapterId"),
                    (SELECT "Id" FROM "CurriculumTopics"
                     WHERE "Code" = 'M8-LEGACY'
                     LIMIT 1))
                WHERE "CurriculumTopicId" IS NULL;
                INSERT INTO "RewardPolicies" (
                    "Id", "Name", "QuizType", "CoinsPerCorrectAnswer",
                    "FirstPassBonusCoins", "PerfectScoreBonusCoins",
                    "ChapterCompletionBonusCoins", "RetryRewardPercent",
                    "DailyCoinLimit", "EffectiveFrom", "EffectiveTo",
                    "IsActive", "CreatedAt", "UpdatedAt")
                VALUES
                    (gen_random_uuid(), 'Default lesson quiz rewards', 1, 10, 0, 5, 0, 100, NULL, CURRENT_TIMESTAMP, NULL, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
                    (gen_random_uuid(), 'Default chapter quiz rewards', 2, 10, 25, 5, 50, 50, NULL, CURRENT_TIMESTAMP, NULL, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

                INSERT INTO "Quizzes" (
                    "Id", "QuizType", "LessonId", "ChapterId", "RewardPolicyId",
                    "Title", "PassScore", "DurationSeconds", "FirstPassCoins",
                    "IsPublished", "IsDeleted", "CreatedAt", "UpdatedAt", "PublishedAt")
                SELECT
                    gen_random_uuid(), 1, l."Id", NULL,
                    (SELECT rp."Id" FROM "RewardPolicies" rp WHERE rp."QuizType" = 1 ORDER BY rp."CreatedAt" LIMIT 1),
                    l."Title" || ' - Quiz', 5.0, 900, 0,
                    l."IsPublished" AND EXISTS (
                        SELECT 1 FROM "Questions" q WHERE q."LessonId" = l."Id"
                    ),
                    FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP,
                    CASE
                        WHEN l."IsPublished" AND EXISTS (
                            SELECT 1 FROM "Questions" q WHERE q."LessonId" = l."Id"
                        ) THEN CURRENT_TIMESTAMP
                        ELSE NULL
                    END
                FROM "Lessons" l;

                INSERT INTO "Quizzes" (
                    "Id", "QuizType", "LessonId", "ChapterId", "RewardPolicyId",
                    "Title", "PassScore", "DurationSeconds", "FirstPassCoins",
                    "IsPublished", "IsDeleted", "CreatedAt", "UpdatedAt", "PublishedAt")
                SELECT
                    gen_random_uuid(), 2, NULL, c."Id",
                    (SELECT rp."Id" FROM "RewardPolicies" rp WHERE rp."QuizType" = 2 ORDER BY rp."CreatedAt" LIMIT 1),
                    c."Title" || ' - Kiểm tra chương', 5.0, 1200, 50,
                    FALSE, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL
                FROM "Chapters" c;

                INSERT INTO "QuizQuestions" ("Id", "QuizId", "QuestionId", "OrderIndex", "Weight")
                SELECT
                    gen_random_uuid(),
                    ranked."QuizId",
                    ranked."QuestionId",
                    ranked."QuizOrder",
                    1
                FROM (
                    SELECT
                        quiz."Id" AS "QuizId",
                        question."Id" AS "QuestionId",
                        (ROW_NUMBER() OVER (
                            PARTITION BY quiz."Id"
                            ORDER BY question."OrderIndex", question."Id"
                        ) - 1)::integer AS "QuizOrder"
                    FROM "Questions" question
                    INNER JOIN "Quizzes" quiz
                        ON quiz."LessonId" = question."LessonId"
                       AND quiz."QuizType" = 1
                ) ranked;

                UPDATE "QuizAttempts" attempt
                SET "QuizId" = quiz."Id",
                    "ClientAttemptId" = attempt."Id",
                    "Score10" = CASE
                        WHEN attempt."TotalQuestions" > 0
                            THEN LEAST(10.0, GREATEST(0.0, ROUND(attempt."Score" * 10.0 / attempt."TotalQuestions", 2)))
                        ELSE 0.0
                    END,
                    "IsPassed" = CASE
                        WHEN attempt."TotalQuestions" > 0
                            THEN attempt."Score" * 10.0 / attempt."TotalQuestions" >= 5.0
                        ELSE FALSE
                    END,
                    "SyncedAt" = attempt."CreatedAt"
                FROM "Quizzes" quiz
                WHERE quiz."LessonId" = attempt."LessonId"
                  AND quiz."QuizType" = 1;

                WITH best_attempt AS (
                    SELECT
                        "UserId",
                        "LessonId",
                        MAX("Score10") AS "BestScore10"
                    FROM "QuizAttempts"
                    WHERE "LessonId" IS NOT NULL
                    GROUP BY "UserId", "LessonId"
                )
                UPDATE "Progresses" progress
                SET "BestScore10" = GREATEST(
                        COALESCE(best_attempt."BestScore10", 0),
                        CASE WHEN progress."IsCompleted" THEN 5.0 ELSE 0.0 END
                    ),
                    "IsCompleted" = GREATEST(
                        COALESCE(best_attempt."BestScore10", 0),
                        CASE WHEN progress."IsCompleted" THEN 5.0 ELSE 0.0 END
                    ) >= 5.0,
                    "Status" = CASE
                        WHEN GREATEST(
                            COALESCE(best_attempt."BestScore10", 0),
                            CASE WHEN progress."IsCompleted" THEN 5.0 ELSE 0.0 END
                        ) >= 5.0 THEN 2
                        ELSE 1
                    END,
                    "ContentViewed" = TRUE,
                    "StartedAt" = COALESCE(progress."CompletedAt", progress."UpdatedAt", CURRENT_TIMESTAMP),
                    "LastViewedAt" = COALESCE(progress."UpdatedAt", CURRENT_TIMESTAMP),
                    "CompletedAt" = CASE
                        WHEN GREATEST(
                            COALESCE(best_attempt."BestScore10", 0),
                            CASE WHEN progress."IsCompleted" THEN 5.0 ELSE 0.0 END
                        ) >= 5.0
                            THEN COALESCE(progress."CompletedAt", progress."UpdatedAt", CURRENT_TIMESTAMP)
                        ELSE NULL
                    END
                FROM best_attempt
                WHERE best_attempt."UserId" = progress."UserId"
                  AND best_attempt."LessonId" = progress."LessonId";

                UPDATE "Progresses" progress
                SET "BestScore10" = CASE WHEN progress."IsCompleted" THEN 5.0 ELSE 0.0 END,
                    "Status" = CASE WHEN progress."IsCompleted" THEN 2 ELSE 1 END,
                    "ContentViewed" = TRUE,
                    "StartedAt" = COALESCE(progress."CompletedAt", progress."UpdatedAt", CURRENT_TIMESTAMP),
                    "LastViewedAt" = COALESCE(progress."UpdatedAt", CURRENT_TIMESTAMP)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "QuizAttempts" attempt
                    WHERE attempt."UserId" = progress."UserId"
                      AND attempt."LessonId" = progress."LessonId"
                );

                UPDATE "CoinTransactions"
                SET "Amount" = GREATEST("Amount", 0),
                    "IdempotencyKey" = 'legacy:coin:' || "Id"::text;

                UPDATE "CoinTransactions" transaction
                SET "RewardPolicyId" = quiz."RewardPolicyId",
                    "ClientAttemptId" = attempt."ClientAttemptId"
                FROM "QuizAttempts" attempt
                INNER JOIN "Quizzes" quiz ON quiz."Id" = attempt."QuizId"
                WHERE transaction."SourceType" = 'quiz_reward'
                  AND transaction."SourceId" = attempt."Id";

                WITH running_balance AS (
                    SELECT
                        "Id",
                        SUM("Amount") OVER (
                            PARTITION BY "UserId"
                            ORDER BY "CreatedAt", "Id"
                            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                        )::integer AS "BalanceAfter"
                    FROM "CoinTransactions"
                )
                UPDATE "CoinTransactions" transaction
                SET "BalanceAfter" = running_balance."BalanceAfter"
                FROM running_balance
                WHERE running_balance."Id" = transaction."Id";

                UPDATE "QuizAttempts" attempt
                SET "CoinsEarned" = reward."CoinsEarned",
                    "RewardProcessedAt" = attempt."CreatedAt"
                FROM (
                    SELECT
                        "SourceId" AS "AttemptId",
                        SUM("Amount")::integer AS "CoinsEarned"
                    FROM "CoinTransactions"
                    WHERE "SourceType" = 'quiz_reward' AND "SourceId" IS NOT NULL
                    GROUP BY "SourceId"
                ) reward
                WHERE reward."AttemptId" = attempt."Id";

                UPDATE "Users" user_account
                SET "CoinsUpdatedAt" = history."LastTransactionAt"
                FROM (
                    SELECT "UserId", MAX("CreatedAt") AS "LastTransactionAt"
                    FROM "CoinTransactions"
                    GROUP BY "UserId"
                ) history
                WHERE history."UserId" = user_account."Id";

                INSERT INTO "BadgeRules" (
                    "Id", "BadgeId", "RuleType", "TargetChapterId",
                    "TargetQuizId", "ThresholdValue", "OrderIndex", "Parameters")
                SELECT
                    gen_random_uuid(), badge."Id", badge."ConditionType",
                    NULL, NULL, NULL, 0, badge."ConditionValue"
                FROM "Badges" badge;

                UPDATE "UserBadges" user_badge
                SET "BadgeRuleId" = rule."Id",
                    "SourceId" = user_badge."BadgeId"
                FROM "BadgeRules" rule
                WHERE rule."BadgeId" = user_badge."BadgeId"
                  AND rule."OrderIndex" = 0;

                WITH user_chapter AS (
                    SELECT DISTINCT progress."UserId", lesson."ChapterId"
                    FROM "Progresses" progress
                    INNER JOIN "Lessons" lesson ON lesson."Id" = progress."LessonId"
                ),
                chapter_summary AS (
                    SELECT
                        user_chapter."UserId",
                        user_chapter."ChapterId",
                        COUNT(*) FILTER (WHERE lesson."IsPublished") AS "PublishedLessons",
                        COUNT(progress."Id") FILTER (
                            WHERE lesson."IsPublished" AND progress."IsCompleted"
                        ) AS "PassedLessons"
                    FROM user_chapter
                    INNER JOIN "Lessons" lesson
                        ON lesson."ChapterId" = user_chapter."ChapterId"
                    LEFT JOIN "Progresses" progress
                        ON progress."UserId" = user_chapter."UserId"
                       AND progress."LessonId" = lesson."Id"
                    GROUP BY user_chapter."UserId", user_chapter."ChapterId"
                )
                INSERT INTO "ChapterProgresses" (
                    "Id", "UserId", "ChapterId", "Status", "BestScore10",
                    "QuizUnlockedAt", "FirstPassedAt", "UpdatedAt", "ClientUpdatedAt")
                SELECT
                    gen_random_uuid(),
                    chapter_summary."UserId",
                    chapter_summary."ChapterId",
                    1,
                    0.0,
                    CASE
                        WHEN chapter_summary."PublishedLessons" > 0
                         AND chapter_summary."PublishedLessons" = chapter_summary."PassedLessons"
                            THEN CURRENT_TIMESTAMP
                        ELSE NULL
                    END,
                    NULL,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM chapter_summary;
                """);
            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_IsActive_Coins",
                table: "Users",
                columns: new[] { "Role", "IsActive", "Coins" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Coins",
                table: "Users",
                sql: "\"Coins\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "\"Role\" IN ('Student', 'Admin')");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_BadgeRuleId",
                table: "UserBadges",
                column: "BadgeRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId_EarnedAt",
                table: "UserBadges",
                columns: new[] { "UserId", "EarnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_ClientAttemptId",
                table: "QuizAttempts",
                column: "ClientAttemptId",
                unique: true,
                filter: "\"ClientAttemptId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_QuizId_UserId_CreatedAt",
                table: "QuizAttempts",
                columns: new[] { "QuizId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_UserId_CreatedAt",
                table: "QuizAttempts",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizAttempts_CoinsEarned",
                table: "QuizAttempts",
                sql: "\"CoinsEarned\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizAttempts_Score10",
                table: "QuizAttempts",
                sql: "\"Score10\" BETWEEN 0 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizAttempts_Target",
                table: "QuizAttempts",
                sql: "\"QuizId\" IS NOT NULL OR \"LessonId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttemptAnswers_AttemptId_QuestionId",
                table: "QuizAttemptAnswers",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizAttemptAnswers_SelectedOption",
                table: "QuizAttemptAnswers",
                sql: "\"SelectedOption\" BETWEEN -1 AND 3");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ChapterId_OrderIndex",
                table: "Questions",
                columns: new[] { "ChapterId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LessonId_OrderIndex",
                table: "Questions",
                columns: new[] { "LessonId", "OrderIndex" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_CorrectOption",
                table: "Questions",
                sql: "\"CorrectOption\" BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_SingleScope",
                table: "Questions",
                sql: "(\"LessonId\" IS NOT NULL AND \"ChapterId\" IS NULL) OR (\"LessonId\" IS NULL AND \"ChapterId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Progresses_BestScore10",
                table: "Progresses",
                sql: "\"BestScore10\" BETWEEN 0 AND 10");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ChapterId_OrderIndex",
                table: "Lessons",
                columns: new[] { "ChapterId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CurriculumTopicId",
                table: "Lessons",
                column: "CurriculumTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_IsPublished_IsDeleted",
                table: "Lessons",
                columns: new[] { "IsPublished", "IsDeleted" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Lessons_ContentVersion",
                table: "Lessons",
                sql: "\"ContentVersion\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_IdempotencyKey",
                table: "CoinTransactions",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_RewardPolicyId",
                table: "CoinTransactions",
                column: "RewardPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_UserId_CreatedAt",
                table: "CoinTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CoinTransactions_Amount",
                table: "CoinTransactions",
                sql: "\"Amount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CoinTransactions_BalanceAfter",
                table: "CoinTransactions",
                sql: "\"BalanceAfter\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CurriculumTopicId",
                table: "Chapters",
                column: "CurriculumTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_IsPublished_IsDeleted_OrderIndex",
                table: "Chapters",
                columns: new[] { "IsPublished", "IsDeleted", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Badges_IsActive_IsDeleted",
                table: "Badges",
                columns: new[] { "IsActive", "IsDeleted" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Badges_RewardCoins",
                table: "Badges",
                sql: "\"RewardCoins\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Badges_RuleMatchMode",
                table: "Badges",
                sql: "\"RuleMatchMode\" IN ('ALL', 'ANY')");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeRules_BadgeId_OrderIndex",
                table: "BadgeRules",
                columns: new[] { "BadgeId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BadgeRules_TargetChapterId",
                table: "BadgeRules",
                column: "TargetChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeRules_TargetQuizId",
                table: "BadgeRules",
                column: "TargetQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterProgresses_ChapterId",
                table: "ChapterProgresses",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterProgresses_UserId_ChapterId",
                table: "ChapterProgresses",
                columns: new[] { "UserId", "ChapterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentAuditLogs_AdminUserId_CreatedAt",
                table: "ContentAuditLogs",
                columns: new[] { "AdminUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentAuditLogs_EntityType_EntityId_CreatedAt",
                table: "ContentAuditLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_Code",
                table: "CurriculumTopics",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_Strand_OrderIndex",
                table: "CurriculumTopics",
                columns: new[] { "Strand", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuestionId",
                table: "QuizQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuizId_OrderIndex",
                table: "QuizQuestions",
                columns: new[] { "QuizId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuizId_QuestionId",
                table: "QuizQuestions",
                columns: new[] { "QuizId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ChapterId",
                table: "Quizzes",
                column: "ChapterId",
                unique: true,
                filter: "\"ChapterId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_LessonId",
                table: "Quizzes",
                column: "LessonId",
                unique: true,
                filter: "\"LessonId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_QuizType_IsPublished_IsDeleted",
                table: "Quizzes",
                columns: new[] { "QuizType", "IsPublished", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_RewardPolicyId",
                table: "Quizzes",
                column: "RewardPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPolicies_QuizType_IsActive_EffectiveFrom",
                table: "RewardPolicies",
                columns: new[] { "QuizType", "IsActive", "EffectiveFrom" });

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_CurriculumTopics_CurriculumTopicId",
                table: "Chapters",
                column: "CurriculumTopicId",
                principalTable: "CurriculumTopics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CoinTransactions_RewardPolicies_RewardPolicyId",
                table: "CoinTransactions",
                column: "RewardPolicyId",
                principalTable: "RewardPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Chapters_ChapterId",
                table: "Lessons",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_CurriculumTopics_CurriculumTopicId",
                table: "Lessons",
                column: "CurriculumTopicId",
                principalTable: "CurriculumTopics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Progresses_Lessons_LessonId",
                table: "Progresses",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Chapters_ChapterId",
                table: "Questions",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Lessons_LessonId",
                table: "Questions",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttemptAnswers_Questions_QuestionId",
                table: "QuizAttemptAnswers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Lessons_LessonId",
                table: "QuizAttempts",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Quizzes_QuizId",
                table: "QuizAttempts",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_BadgeRules_BadgeRuleId",
                table: "UserBadges",
                column: "BadgeRuleId",
                principalTable: "BadgeRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_CurriculumTopics_CurriculumTopicId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_CoinTransactions_RewardPolicies_RewardPolicyId",
                table: "CoinTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Chapters_ChapterId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_CurriculumTopics_CurriculumTopicId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_Progresses_Lessons_LessonId",
                table: "Progresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Chapters_ChapterId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Lessons_LessonId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttemptAnswers_Questions_QuestionId",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Lessons_LessonId",
                table: "QuizAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Quizzes_QuizId",
                table: "QuizAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_BadgeRules_BadgeRuleId",
                table: "UserBadges");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges");

            migrationBuilder.DropTable(
                name: "BadgeRules");

            migrationBuilder.DropTable(
                name: "ChapterProgresses");

            migrationBuilder.DropTable(
                name: "ContentAuditLogs");

            migrationBuilder.DropTable(
                name: "CurriculumTopics");

            migrationBuilder.DropTable(
                name: "QuizQuestions");

            migrationBuilder.DropTable(
                name: "Quizzes");

            migrationBuilder.DropTable(
                name: "RewardPolicies");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role_IsActive_Coins",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Coins",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserBadges_BadgeRuleId",
                table: "UserBadges");

            migrationBuilder.DropIndex(
                name: "IX_UserBadges_UserId_EarnedAt",
                table: "UserBadges");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_ClientAttemptId",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_QuizId_UserId_CreatedAt",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_UserId_CreatedAt",
                table: "QuizAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizAttempts_CoinsEarned",
                table: "QuizAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizAttempts_Score10",
                table: "QuizAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizAttempts_Target",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttemptAnswers_AttemptId_QuestionId",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizAttemptAnswers_SelectedOption",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ChapterId_OrderIndex",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_LessonId_OrderIndex",
                table: "Questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_CorrectOption",
                table: "Questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_SingleScope",
                table: "Questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Progresses_BestScore10",
                table: "Progresses");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_ChapterId_OrderIndex",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CurriculumTopicId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_IsPublished_IsDeleted",
                table: "Lessons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Lessons_ContentVersion",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_CoinTransactions_IdempotencyKey",
                table: "CoinTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CoinTransactions_RewardPolicyId",
                table: "CoinTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CoinTransactions_UserId_CreatedAt",
                table: "CoinTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CoinTransactions_Amount",
                table: "CoinTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CoinTransactions_BalanceAfter",
                table: "CoinTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_CurriculumTopicId",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_IsPublished_IsDeleted_OrderIndex",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Badges_IsActive_IsDeleted",
                table: "Badges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Badges_RewardCoins",
                table: "Badges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Badges_RuleMatchMode",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CoinsUpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BadgeRuleId",
                table: "UserBadges");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "UserBadges");

            migrationBuilder.DropColumn(
                name: "ClientAttemptId",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "CoinsEarned",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "IsPassed",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "QuizId",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "RewardProcessedAt",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "Score10",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "BestScore10",
                table: "Progresses");

            migrationBuilder.DropColumn(
                name: "ContentViewed",
                table: "Progresses");

            migrationBuilder.DropColumn(
                name: "LastViewedAt",
                table: "Progresses");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Progresses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Progresses");

            migrationBuilder.DropColumn(
                name: "RelatedEntityId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ContentVersion",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CurriculumTopicId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                table: "CoinTransactions");

            migrationBuilder.DropColumn(
                name: "ClientAttemptId",
                table: "CoinTransactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "CoinTransactions");

            migrationBuilder.DropColumn(
                name: "RewardPolicyId",
                table: "CoinTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "CurriculumTopicId",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "RewardCoins",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "RuleMatchMode",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Badges");

            migrationBuilder.AlterColumn<Guid>(
                name: "LessonId",
                table: "QuizAttempts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LessonId",
                table: "Questions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_UserId",
                table: "QuizAttempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttemptAnswers_AttemptId",
                table: "QuizAttemptAnswers",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LessonId",
                table: "Questions",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ChapterId",
                table: "Lessons",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_OrderIndex",
                table: "Lessons",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_UserId",
                table: "CoinTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Chapters_ChapterId",
                table: "Lessons",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Progresses_Lessons_LessonId",
                table: "Progresses",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Lessons_LessonId",
                table: "Questions",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttemptAnswers_Questions_QuestionId",
                table: "QuizAttemptAnswers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Lessons_LessonId",
                table: "QuizAttempts",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
