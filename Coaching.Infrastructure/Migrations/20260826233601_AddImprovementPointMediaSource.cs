using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImprovementPointMediaSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "ImprovementPointMedia",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Uploads land under the "feedback/{coachId}/" S3 prefix; everything else was pasted.
            migrationBuilder.Sql(
                "UPDATE \"ImprovementPointMedia\" SET \"Source\" = 1 WHERE \"Url\" LIKE '%/feedback/%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "ImprovementPointMedia");
        }
    }
}
