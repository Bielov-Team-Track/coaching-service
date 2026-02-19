using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationExercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Skill = table.Column<int>(type: "integer", nullable: true),
                    MinScore = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SharedWithPlayer = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxState",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Surname = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImageThumbHash = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    MaxPoints = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Config = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationMetrics_EvaluationExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "EvaluationExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationPlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationPlanItems_EvaluationExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "EvaluationExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationPlanItems_EvaluationPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "EvaluationPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoachUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationSessions_EvaluationPlans_EvaluationPlanId",
                        column: x => x.EvaluationPlanId,
                        principalTable: "EvaluationPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ImprovementPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedbackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImprovementPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImprovementPoints_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Praises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedbackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BadgeType = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Praises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Praises_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateTable(
                name: "Drills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Skills = table.Column<int[]>(type: "integer[]", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: true),
                    MinPlayers = table.Column<int>(type: "integer", nullable: true),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: true),
                    Instructions = table.Column<string[]>(type: "text[]", nullable: false),
                    CoachingPoints = table.Column<string[]>(type: "text[]", nullable: false),
                    VideoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    LikeCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Animations = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drills_UserProfile_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPlanTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    TotalDuration = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPlanTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingPlanTemplates_UserProfile_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetricSkillWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    Skill = table.Column<int>(type: "integer", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricSkillWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricSkillWeights_EvaluationMetrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "EvaluationMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationParticipants_EvaluationSessions_EvaluationSession~",
                        column: x => x.EvaluationSessionId,
                        principalTable: "EvaluationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImprovementPointMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImprovementPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImprovementPointMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImprovementPointMedia_ImprovementPoints_ImprovementPointId",
                        column: x => x.ImprovementPointId,
                        principalTable: "ImprovementPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PraiseId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    BadgeType = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AwardedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerBadges_Praises_PraiseId",
                        column: x => x.PraiseId,
                        principalTable: "Praises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DrillAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<int>(type: "integer", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillAttachments_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrillBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillBookmarks_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrillBookmarks_UserProfile_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrillComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillComments_DrillComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "DrillComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrillComments_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrillComments_UserProfile_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrillEquipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillEquipment_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrillLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillLikes_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrillLikes_UserProfile_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrillVariations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillVariations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillVariations_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrillVariations_Drills_SourceDrillId",
                        column: x => x.SourceDrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrillVariations_Drills_TargetDrillId",
                        column: x => x.TargetDrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImprovementPointDrills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImprovementPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImprovementPointDrills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImprovementPointDrills_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImprovementPointDrills_ImprovementPoints_ImprovementPointId",
                        column: x => x.ImprovementPointId,
                        principalTable: "ImprovementPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateBookmarks_TrainingPlanTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateComments_TemplateComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "TemplateComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateComments_TrainingPlanTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateComments_UserProfile_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateLikes_TrainingPlanTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateSections_TrainingPlanTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: true),
                    SharedWithPlayer = table.Column<bool>(type: "boolean", nullable: false),
                    CoachNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerEvaluations_EvaluationParticipants_EvaluationParticip~",
                        column: x => x.EvaluationParticipantId,
                        principalTable: "EvaluationParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateItems_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateItems_TemplateSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "TemplateSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TemplateItems_TrainingPlanTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMetricScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    NormalizedScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMetricScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMetricScores_EvaluationMetrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "EvaluationMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMetricScores_PlayerEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "PlayerEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSkillScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Skill = table.Column<int>(type: "integer", nullable: false),
                    EarnedPoints = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxPoints = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    Level = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSkillScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSkillScores_PlayerEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "PlayerEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrillAttachments_DrillId",
                table: "DrillAttachments",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillAttachments_DrillId_Order",
                table: "DrillAttachments",
                columns: new[] { "DrillId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_DrillBookmarks_DrillId",
                table: "DrillBookmarks",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillBookmarks_DrillId_UserId",
                table: "DrillBookmarks",
                columns: new[] { "DrillId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrillBookmarks_UserId",
                table: "DrillBookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillComments_CreatedAt",
                table: "DrillComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DrillComments_DrillId",
                table: "DrillComments",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillComments_ParentCommentId",
                table: "DrillComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillComments_UserId",
                table: "DrillComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillEquipment_DrillId",
                table: "DrillEquipment",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillEquipment_IsOptional",
                table: "DrillEquipment",
                column: "IsOptional");

            migrationBuilder.CreateIndex(
                name: "IX_DrillLikes_DrillId",
                table: "DrillLikes",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillLikes_DrillId_UserId",
                table: "DrillLikes",
                columns: new[] { "DrillId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrillLikes_UserId",
                table: "DrillLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_Category",
                table: "Drills",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_ClubId",
                table: "Drills",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_CreatedByUserId",
                table: "Drills",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_LikeCount",
                table: "Drills",
                column: "LikeCount");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_Visibility",
                table: "Drills",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_DrillVariations_DrillId",
                table: "DrillVariations",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillVariations_SourceDrillId",
                table: "DrillVariations",
                column: "SourceDrillId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillVariations_SourceDrillId_Order",
                table: "DrillVariations",
                columns: new[] { "SourceDrillId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_DrillVariations_TargetDrillId",
                table: "DrillVariations",
                column: "TargetDrillId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationExercises_ClubId",
                table: "EvaluationExercises",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationExercises_CreatedByUserId",
                table: "EvaluationExercises",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationMetrics_ExerciseId_Order",
                table: "EvaluationMetrics",
                columns: new[] { "ExerciseId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipants_EvaluationSessionId_PlayerId",
                table: "EvaluationParticipants",
                columns: new[] { "EvaluationSessionId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationParticipants_PlayerId",
                table: "EvaluationParticipants",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPlanItems_ExerciseId",
                table: "EvaluationPlanItems",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPlanItems_PlanId_Order",
                table: "EvaluationPlanItems",
                columns: new[] { "PlanId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPlans_ClubId",
                table: "EvaluationPlans",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPlans_CreatedByUserId",
                table: "EvaluationPlans",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationSessions_ClubId",
                table: "EvaluationSessions",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationSessions_CoachUserId",
                table: "EvaluationSessions",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationSessions_EvaluationPlanId",
                table: "EvaluationSessions",
                column: "EvaluationPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationSessions_EventId",
                table: "EvaluationSessions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationThresholds_ClubId",
                table: "EvaluationThresholds",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationThresholds_ClubId_Skill",
                table: "EvaluationThresholds",
                columns: new[] { "ClubId", "Skill" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CoachUserId",
                table: "Feedbacks",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_EventId",
                table: "Feedbacks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_RecipientUserId",
                table: "Feedbacks",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImprovementPointDrills_DrillId",
                table: "ImprovementPointDrills",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_ImprovementPointDrills_ImprovementPointId_DrillId",
                table: "ImprovementPointDrills",
                columns: new[] { "ImprovementPointId", "DrillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImprovementPointMedia_ImprovementPointId",
                table: "ImprovementPointMedia",
                column: "ImprovementPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ImprovementPoints_FeedbackId_Order",
                table: "ImprovementPoints",
                columns: new[] { "FeedbackId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_MetricSkillWeights_MetricId_Skill",
                table: "MetricSkillWeights",
                columns: new[] { "MetricId", "Skill" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBadges_EventId",
                table: "PlayerBadges",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBadges_PraiseId",
                table: "PlayerBadges",
                column: "PraiseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBadges_UserId",
                table: "PlayerBadges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_EvaluatedByUserId",
                table: "PlayerEvaluations",
                column: "EvaluatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_EvaluationParticipantId",
                table: "PlayerEvaluations",
                column: "EvaluationParticipantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_PlayerId",
                table: "PlayerEvaluations",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMetricScores_EvaluationId_MetricId",
                table: "PlayerMetricScores",
                columns: new[] { "EvaluationId", "MetricId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMetricScores_MetricId",
                table: "PlayerMetricScores",
                column: "MetricId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSkillScores_EvaluationId_Skill",
                table: "PlayerSkillScores",
                columns: new[] { "EvaluationId", "Skill" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Praises_FeedbackId",
                table: "Praises",
                column: "FeedbackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBookmarks_TemplateId_UserId",
                table: "TemplateBookmarks",
                columns: new[] { "TemplateId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBookmarks_UserId",
                table: "TemplateBookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateComments_ParentCommentId",
                table: "TemplateComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateComments_TemplateId",
                table: "TemplateComments",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateComments_UserId",
                table: "TemplateComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_DrillId",
                table: "TemplateItems",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_SectionId",
                table: "TemplateItems",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_TemplateId_Order",
                table: "TemplateItems",
                columns: new[] { "TemplateId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateLikes_TemplateId_UserId",
                table: "TemplateLikes",
                columns: new[] { "TemplateId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateLikes_UserId",
                table: "TemplateLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_TemplateId_Order",
                table: "TemplateSections",
                columns: new[] { "TemplateId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanTemplates_ClubId",
                table: "TrainingPlanTemplates",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanTemplates_CreatedByUserId",
                table: "TrainingPlanTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanTemplates_LikeCount",
                table: "TrainingPlanTemplates",
                column: "LikeCount");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanTemplates_UsageCount",
                table: "TrainingPlanTemplates",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanTemplates_Visibility",
                table: "TrainingPlanTemplates",
                column: "Visibility");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrillAttachments");

            migrationBuilder.DropTable(
                name: "DrillBookmarks");

            migrationBuilder.DropTable(
                name: "DrillComments");

            migrationBuilder.DropTable(
                name: "DrillEquipment");

            migrationBuilder.DropTable(
                name: "DrillLikes");

            migrationBuilder.DropTable(
                name: "DrillVariations");

            migrationBuilder.DropTable(
                name: "EvaluationPlanItems");

            migrationBuilder.DropTable(
                name: "EvaluationThresholds");

            migrationBuilder.DropTable(
                name: "ImprovementPointDrills");

            migrationBuilder.DropTable(
                name: "ImprovementPointMedia");

            migrationBuilder.DropTable(
                name: "MetricSkillWeights");

            migrationBuilder.DropTable(
                name: "OutboxMessage");

            migrationBuilder.DropTable(
                name: "PlayerBadges");

            migrationBuilder.DropTable(
                name: "PlayerMetricScores");

            migrationBuilder.DropTable(
                name: "PlayerSkillScores");

            migrationBuilder.DropTable(
                name: "TemplateBookmarks");

            migrationBuilder.DropTable(
                name: "TemplateComments");

            migrationBuilder.DropTable(
                name: "TemplateItems");

            migrationBuilder.DropTable(
                name: "TemplateLikes");

            migrationBuilder.DropTable(
                name: "ImprovementPoints");

            migrationBuilder.DropTable(
                name: "InboxState");

            migrationBuilder.DropTable(
                name: "OutboxState");

            migrationBuilder.DropTable(
                name: "Praises");

            migrationBuilder.DropTable(
                name: "EvaluationMetrics");

            migrationBuilder.DropTable(
                name: "PlayerEvaluations");

            migrationBuilder.DropTable(
                name: "Drills");

            migrationBuilder.DropTable(
                name: "TemplateSections");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "EvaluationExercises");

            migrationBuilder.DropTable(
                name: "EvaluationParticipants");

            migrationBuilder.DropTable(
                name: "TrainingPlanTemplates");

            migrationBuilder.DropTable(
                name: "EvaluationSessions");

            migrationBuilder.DropTable(
                name: "UserProfile");

            migrationBuilder.DropTable(
                name: "EvaluationPlans");
        }
    }
}
