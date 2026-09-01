using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "TrainingPlanRunItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "TrainingPlanRunItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RunStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunStations_TrainingPlanRunItems_RunItemId",
                        column: x => x.RunItemId,
                        principalTable: "TrainingPlanRunItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunStationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunStationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunStationItems_RunStations_RunStationId",
                        column: x => x.RunStationId,
                        principalTable: "RunStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunStationItems_RunStationId_Order",
                table: "RunStationItems",
                columns: new[] { "RunStationId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_RunStations_RunItemId_Order",
                table: "RunStations",
                columns: new[] { "RunItemId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunStationItems");

            migrationBuilder.DropTable(
                name: "RunStations");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "TrainingPlanRunItems");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "TrainingPlanRunItems");
        }
    }
}
