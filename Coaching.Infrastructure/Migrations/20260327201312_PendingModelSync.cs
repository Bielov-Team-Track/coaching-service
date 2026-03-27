using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationGroup_EvaluationSessions_SessionId",
                table: "EvaluationGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationGroupPlayer_EvaluationGroup_GroupId",
                table: "EvaluationGroupPlayer");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerExerciseScore_EvaluationExercises_ExerciseId",
                table: "PlayerExerciseScore");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerExerciseScore_EvaluationSessions_SessionId",
                table: "PlayerExerciseScore");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerMetricScores_PlayerExerciseScore_PlayerExerciseScoreId",
                table: "PlayerMetricScores");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateComments_UserProfile_UserId",
                table: "TemplateComments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerExerciseScore",
                table: "PlayerExerciseScore");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EvaluationGroupPlayer",
                table: "EvaluationGroupPlayer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EvaluationGroup",
                table: "EvaluationGroup");

            migrationBuilder.RenameTable(
                name: "PlayerExerciseScore",
                newName: "PlayerExerciseScores");

            migrationBuilder.RenameTable(
                name: "EvaluationGroupPlayer",
                newName: "EvaluationGroupPlayers");

            migrationBuilder.RenameTable(
                name: "EvaluationGroup",
                newName: "EvaluationGroups");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerExerciseScore_SessionId_PlayerId_ExerciseId",
                table: "PlayerExerciseScores",
                newName: "IX_PlayerExerciseScores_SessionId_PlayerId_ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerExerciseScore_ExerciseId",
                table: "PlayerExerciseScores",
                newName: "IX_PlayerExerciseScores_ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_EvaluationGroupPlayer_GroupId_PlayerId",
                table: "EvaluationGroupPlayers",
                newName: "IX_EvaluationGroupPlayers_GroupId_PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_EvaluationGroup_SessionId",
                table: "EvaluationGroups",
                newName: "IX_EvaluationGroups_SessionId");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PlayerMetricScores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FeedbackId",
                table: "PlayerEvaluations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "Feedbacks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Feedbacks",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentPlainText",
                table: "Feedbacks",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EvaluationId",
                table: "Feedbacks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerExerciseScores",
                table: "PlayerExerciseScores",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EvaluationGroupPlayers",
                table: "EvaluationGroupPlayers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EvaluationGroups",
                table: "EvaluationGroups",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_FeedbackId",
                table: "PlayerEvaluations",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEvaluations_SessionId",
                table: "PlayerEvaluations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ClubId",
                table: "Feedbacks",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_EvaluationId",
                table: "Feedbacks",
                column: "EvaluationId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationGroupPlayers_EvaluationGroups_GroupId",
                table: "EvaluationGroupPlayers",
                column: "GroupId",
                principalTable: "EvaluationGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationGroups_EvaluationSessions_SessionId",
                table: "EvaluationGroups",
                column: "SessionId",
                principalTable: "EvaluationSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_PlayerEvaluations_EvaluationId",
                table: "Feedbacks",
                column: "EvaluationId",
                principalTable: "PlayerEvaluations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

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
                name: "FK_PlayerExerciseScores_EvaluationExercises_ExerciseId",
                table: "PlayerExerciseScores",
                column: "ExerciseId",
                principalTable: "EvaluationExercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerExerciseScores_EvaluationSessions_SessionId",
                table: "PlayerExerciseScores",
                column: "SessionId",
                principalTable: "EvaluationSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerMetricScores_PlayerExerciseScores_PlayerExerciseScore~",
                table: "PlayerMetricScores",
                column: "PlayerExerciseScoreId",
                principalTable: "PlayerExerciseScores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateComments_UserProfile_UserId",
                table: "TemplateComments",
                column: "UserId",
                principalTable: "UserProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationGroupPlayers_EvaluationGroups_GroupId",
                table: "EvaluationGroupPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationGroups_EvaluationSessions_SessionId",
                table: "EvaluationGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_PlayerEvaluations_EvaluationId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerEvaluations_EvaluationSessions_SessionId",
                table: "PlayerEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerEvaluations_Feedbacks_FeedbackId",
                table: "PlayerEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerExerciseScores_EvaluationExercises_ExerciseId",
                table: "PlayerExerciseScores");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerExerciseScores_EvaluationSessions_SessionId",
                table: "PlayerExerciseScores");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerMetricScores_PlayerExerciseScores_PlayerExerciseScore~",
                table: "PlayerMetricScores");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateComments_UserProfile_UserId",
                table: "TemplateComments");

            migrationBuilder.DropIndex(
                name: "IX_PlayerEvaluations_FeedbackId",
                table: "PlayerEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_PlayerEvaluations_SessionId",
                table: "PlayerEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_ClubId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_EvaluationId",
                table: "Feedbacks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerExerciseScores",
                table: "PlayerExerciseScores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EvaluationGroups",
                table: "EvaluationGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EvaluationGroupPlayers",
                table: "EvaluationGroupPlayers");

            migrationBuilder.DropColumn(
                name: "FeedbackId",
                table: "PlayerEvaluations");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "ContentPlainText",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "EvaluationId",
                table: "Feedbacks");

            migrationBuilder.RenameTable(
                name: "PlayerExerciseScores",
                newName: "PlayerExerciseScore");

            migrationBuilder.RenameTable(
                name: "EvaluationGroups",
                newName: "EvaluationGroup");

            migrationBuilder.RenameTable(
                name: "EvaluationGroupPlayers",
                newName: "EvaluationGroupPlayer");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerExerciseScores_SessionId_PlayerId_ExerciseId",
                table: "PlayerExerciseScore",
                newName: "IX_PlayerExerciseScore_SessionId_PlayerId_ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerExerciseScores_ExerciseId",
                table: "PlayerExerciseScore",
                newName: "IX_PlayerExerciseScore_ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_EvaluationGroups_SessionId",
                table: "EvaluationGroup",
                newName: "IX_EvaluationGroup_SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_EvaluationGroupPlayers_GroupId_PlayerId",
                table: "EvaluationGroupPlayer",
                newName: "IX_EvaluationGroupPlayer_GroupId_PlayerId");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "PlayerMetricScores",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerExerciseScore",
                table: "PlayerExerciseScore",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EvaluationGroup",
                table: "EvaluationGroup",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EvaluationGroupPlayer",
                table: "EvaluationGroupPlayer",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationGroup_EvaluationSessions_SessionId",
                table: "EvaluationGroup",
                column: "SessionId",
                principalTable: "EvaluationSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationGroupPlayer_EvaluationGroup_GroupId",
                table: "EvaluationGroupPlayer",
                column: "GroupId",
                principalTable: "EvaluationGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerExerciseScore_EvaluationExercises_ExerciseId",
                table: "PlayerExerciseScore",
                column: "ExerciseId",
                principalTable: "EvaluationExercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerExerciseScore_EvaluationSessions_SessionId",
                table: "PlayerExerciseScore",
                column: "SessionId",
                principalTable: "EvaluationSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerMetricScores_PlayerExerciseScore_PlayerExerciseScoreId",
                table: "PlayerMetricScores",
                column: "PlayerExerciseScoreId",
                principalTable: "PlayerExerciseScore",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateComments_UserProfile_UserId",
                table: "TemplateComments",
                column: "UserId",
                principalTable: "UserProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
