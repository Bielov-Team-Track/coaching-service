using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanItemKinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoachedDuration",
                table: "TrainingPlanTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "DrillId",
                table: "TrainingPlanRunItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "DrillId",
                table: "TemplateItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "TemplateItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "TemplateItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // Every row that existed before this migration is a Drill (Kind defaults to 0),
            // so a plan's coached total starts equal to its wall-clock total rather than zero.
            migrationBuilder.Sql(@"UPDATE ""TrainingPlanTemplates"" SET ""CoachedDuration"" = ""TotalDuration"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoachedDuration",
                table: "TrainingPlanTemplates");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "TemplateItems");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "TemplateItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "DrillId",
                table: "TrainingPlanRunItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DrillId",
                table: "TemplateItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
