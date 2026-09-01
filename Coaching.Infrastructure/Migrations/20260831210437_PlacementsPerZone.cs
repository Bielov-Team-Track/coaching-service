using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlacementsPerZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId_ItemId",
                table: "PlanItemPlacements");

            migrationBuilder.DropIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId_StationItemId",
                table: "PlanItemPlacements");

            migrationBuilder.CreateIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId_ItemId_CourtId_ZoneId",
                table: "PlanItemPlacements",
                columns: new[] { "PlanId", "VenueId", "ItemId", "CourtId", "ZoneId" },
                unique: true,
                filter: "\"ItemId\" IS NOT NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId_StationItemId_CourtId_Zon~",
                table: "PlanItemPlacements",
                columns: new[] { "PlanId", "VenueId", "StationItemId", "CourtId", "ZoneId" },
                unique: true,
                filter: "\"StationItemId\" IS NOT NULL")
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId_ItemId_CourtId_ZoneId",
                table: "PlanItemPlacements");

            migrationBuilder.DropIndex(
                name: "IX_PlanItemPlacements_PlanId_VenueId_StationItemId_CourtId_Zon~",
                table: "PlanItemPlacements");

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
        }
    }
}
