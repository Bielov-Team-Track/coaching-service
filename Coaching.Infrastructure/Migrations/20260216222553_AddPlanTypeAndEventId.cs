using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanTypeAndEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "TrainingPlanTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanType",
                table: "TrainingPlanTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTemplateId",
                table: "TrainingPlanTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanTemplates_EventId",
                table: "TrainingPlanTemplates",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanTemplates_PlanType",
                table: "TrainingPlanTemplates",
                column: "PlanType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingPlanTemplates_EventId",
                table: "TrainingPlanTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TrainingPlanTemplates_PlanType",
                table: "TrainingPlanTemplates");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "TrainingPlanTemplates");

            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "TrainingPlanTemplates");

            migrationBuilder.DropColumn(
                name: "SourceTemplateId",
                table: "TrainingPlanTemplates");
        }
    }
}
