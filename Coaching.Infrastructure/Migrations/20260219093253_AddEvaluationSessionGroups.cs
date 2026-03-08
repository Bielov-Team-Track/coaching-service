using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationSessionGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PlayerMetricScores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerExerciseScoreId",
                table: "PlayerMetricScores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FeedbackId",
                table: "PlayerEvaluations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "PlayerEvaluations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "EvaluationSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                table: "EvaluationSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShareFeedback",
                table: "EvaluationSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShareMetrics",
                table: "EvaluationSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "EvaluationSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EvaluationGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EvaluatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationGroups_EvaluationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "EvaluationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerExerciseScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ScoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerExerciseScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerExerciseScores_EvaluationExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "EvaluationExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerExerciseScores_EvaluationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "EvaluationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationGroupPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationGroupPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationGroupPlayers_EvaluationGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "EvaluationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMetricScores_PlayerExerciseScoreId",
                table: "PlayerMetricScores",
                column: "PlayerExerciseScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_FeedbackId",
                table: "PlayerEvaluations",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_SessionId",
                table: "PlayerEvaluations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationGroupPlayers_GroupId_PlayerId",
                table: "EvaluationGroupPlayers",
                columns: new[] { "GroupId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationGroups_SessionId",
                table: "EvaluationGroups",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerExerciseScores_ExerciseId",
                table: "PlayerExerciseScores",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerExerciseScores_SessionId_PlayerId_ExerciseId",
                table: "PlayerExerciseScores",
                columns: new[] { "SessionId", "PlayerId", "ExerciseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerEvaluations_EvaluationSessions_SessionId",
                table: "PlayerEvaluations",
                column: "SessionId",
                principalTable: "EvaluationSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerEvaluations_Feedbacks_FeedbackId",
                table: "PlayerEvaluations",
                column: "FeedbackId",
                principalTable: "Feedbacks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerMetricScores_PlayerExerciseScores_PlayerExerciseScore~",
                table: "PlayerMetricScores",
                column: "PlayerExerciseScoreId",
                principalTable: "PlayerExerciseScores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerEvaluations_EvaluationSessions_SessionId",
                table: "PlayerEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerEvaluations_Feedbacks_FeedbackId",
                table: "PlayerEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerMetricScores_PlayerExerciseScores_PlayerExerciseScore~",
                table: "PlayerMetricScores");

            migrationBuilder.DropTable(
                name: "EvaluationGroupPlayers");

            migrationBuilder.DropTable(
                name: "PlayerExerciseScores");

            migrationBuilder.DropTable(
                name: "EvaluationGroups");

            migrationBuilder.DropIndex(
                name: "IX_PlayerMetricScores_PlayerExerciseScoreId",
                table: "PlayerMetricScores");

            migrationBuilder.DropIndex(
                name: "IX_PlayerEvaluations_FeedbackId",
                table: "PlayerEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_PlayerEvaluations_SessionId",
                table: "PlayerEvaluations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PlayerMetricScores");

            migrationBuilder.DropColumn(
                name: "PlayerExerciseScoreId",
                table: "PlayerMetricScores");

            migrationBuilder.DropColumn(
                name: "FeedbackId",
                table: "PlayerEvaluations");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "PlayerEvaluations");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "EvaluationSessions");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "EvaluationSessions");

            migrationBuilder.DropColumn(
                name: "ShareFeedback",
                table: "EvaluationSessions");

            migrationBuilder.DropColumn(
                name: "ShareMetrics",
                table: "EvaluationSessions");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "EvaluationSessions");
        }
    }
}
