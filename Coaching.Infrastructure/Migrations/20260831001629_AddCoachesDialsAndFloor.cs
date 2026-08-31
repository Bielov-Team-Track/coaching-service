using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachesDialsAndFloor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DrillDials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OnText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OffText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OnLabel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    OffLabel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillDials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillDials_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanCoaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanCoaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanCoaches_TrainingPlanTemplates_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanCourtBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourtId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOurs = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    TakenBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Split = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanCourtBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanCourtBookings_TrainingPlanTemplates_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanItemDialValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    StationItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    DialName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanItemDialValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanItemDialValues_TrainingPlanTemplates_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanItemPlacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourtId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    StationItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanItemPlacements", x => x.Id);
                    table.CheckConstraint("CK_PlanItemPlacements_OneAnchor", "(\"ItemId\" IS NULL) <> (\"StationItemId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_PlanItemPlacements_TrainingPlanTemplates_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TrainingPlanTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanStationCoaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanStationCoaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanStationCoaches_PlanStations_StationId",
                        column: x => x.StationId,
                        principalTable: "PlanStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrillDials_DrillId_Name",
                table: "DrillDials",
                columns: new[] { "DrillId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrillDials_DrillId_Order",
                table: "DrillDials",
                columns: new[] { "DrillId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanCoaches_PlanId_UserId",
                table: "PlanCoaches",
                columns: new[] { "PlanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanCoaches_UserId",
                table: "PlanCoaches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanCourtBookings_PlanId_CourtId",
                table: "PlanCourtBookings",
                columns: new[] { "PlanId", "CourtId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanCourtBookings_PlanId_VenueId",
                table: "PlanCourtBookings",
                columns: new[] { "PlanId", "VenueId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanItemDialValues_ItemId_DialName",
                table: "PlanItemDialValues",
                columns: new[] { "ItemId", "DialName" },
                unique: true,
                filter: "\"ItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanItemDialValues_PlanId",
                table: "PlanItemDialValues",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanItemDialValues_StationItemId_DialName",
                table: "PlanItemDialValues",
                columns: new[] { "StationItemId", "DialName" },
                unique: true,
                filter: "\"StationItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId",
                table: "PlanItemPlacements",
                columns: new[] { "PlanId", "VenueId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId_ItemId",
                table: "PlanItemPlacements",
                columns: new[] { "PlanId", "VenueId", "ItemId" },
                unique: true,
                filter: "\"ItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId_StationItemId",
                table: "PlanItemPlacements",
                columns: new[] { "PlanId", "VenueId", "StationItemId" },
                unique: true,
                filter: "\"StationItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanStationCoaches_StationId_UserId",
                table: "PlanStationCoaches",
                columns: new[] { "StationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanStationCoaches_UserId",
                table: "PlanStationCoaches",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrillDials");

            migrationBuilder.DropTable(
                name: "PlanCoaches");

            migrationBuilder.DropTable(
                name: "PlanCourtBookings");

            migrationBuilder.DropTable(
                name: "PlanItemDialValues");

            migrationBuilder.DropTable(
                name: "PlanItemPlacements");

            migrationBuilder.DropTable(
                name: "PlanStationCoaches");
        }
    }
}
