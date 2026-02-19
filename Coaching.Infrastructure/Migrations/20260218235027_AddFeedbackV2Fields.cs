using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackV2Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            // Data migration: copy existing Comment data into the new Content/ContentPlainText columns,
            // wrapping plain text in <p> tags and HTML-escaping special characters for consistency.
            migrationBuilder.Sql("""
                UPDATE "Feedbacks"
                SET "Content" = '<p>' || replace(replace(replace("Comment", '&', '&amp;'), '<', '&lt;'), '>', '&gt;') || '</p>',
                    "ContentPlainText" = LEFT("Comment", 4000)
                WHERE "Comment" IS NOT NULL AND "Content" IS NULL
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ClubId",
                table: "Feedbacks",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_EvaluationId",
                table: "Feedbacks",
                column: "EvaluationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_PlayerEvaluations_EvaluationId",
                table: "Feedbacks",
                column: "EvaluationId",
                principalTable: "PlayerEvaluations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_PlayerEvaluations_EvaluationId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_ClubId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_EvaluationId",
                table: "Feedbacks");

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
        }
    }
}
