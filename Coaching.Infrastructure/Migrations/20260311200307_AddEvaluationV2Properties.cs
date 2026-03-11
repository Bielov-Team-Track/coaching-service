using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationV2Properties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PlayerMetricScores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerExerciseScoreId",
                table: "PlayerMetricScores",
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
                name: "EvaluationGroup",
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
                    table.PrimaryKey("PK_EvaluationGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationGroup_EvaluationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "EvaluationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerExerciseScore",
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
                    table.PrimaryKey("PK_PlayerExerciseScore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerExerciseScore_EvaluationExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "EvaluationExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerExerciseScore_EvaluationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "EvaluationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationGroupPlayer",
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
                    table.PrimaryKey("PK_EvaluationGroupPlayer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationGroupPlayer_EvaluationGroup_GroupId",
                        column: x => x.GroupId,
                        principalTable: "EvaluationGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMetricScores_PlayerExerciseScoreId",
                table: "PlayerMetricScores",
                column: "PlayerExerciseScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationGroup_SessionId",
                table: "EvaluationGroup",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationGroupPlayer_GroupId_PlayerId",
                table: "EvaluationGroupPlayer",
                columns: new[] { "GroupId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerExerciseScore_ExerciseId",
                table: "PlayerExerciseScore",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerExerciseScore_SessionId_PlayerId_ExerciseId",
                table: "PlayerExerciseScore",
                columns: new[] { "SessionId", "PlayerId", "ExerciseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerMetricScores_PlayerExerciseScore_PlayerExerciseScoreId",
                table: "PlayerMetricScores",
                column: "PlayerExerciseScoreId",
                principalTable: "PlayerExerciseScore",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerMetricScores_PlayerExerciseScore_PlayerExerciseScoreId",
                table: "PlayerMetricScores");

            migrationBuilder.DropTable(
                name: "EvaluationGroupPlayer");

            migrationBuilder.DropTable(
                name: "PlayerExerciseScore");

            migrationBuilder.DropTable(
                name: "EvaluationGroup");

            migrationBuilder.DropIndex(
                name: "IX_PlayerMetricScores_PlayerExerciseScoreId",
                table: "PlayerMetricScores");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PlayerMetricScores");

            migrationBuilder.DropColumn(
                name: "PlayerExerciseScoreId",
                table: "PlayerMetricScores");

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
