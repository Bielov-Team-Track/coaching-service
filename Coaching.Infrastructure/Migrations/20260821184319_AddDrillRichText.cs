using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coaching.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDrillRichText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoachingPointsHtml",
                table: "Drills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstructionsHtml",
                table: "Drills",
                type: "text",
                nullable: true);

            // Existing rows predate the editor. Wrap their lines as list HTML so every
            // reader that prefers the HTML column finds content, not a blank drill.
            migrationBuilder.Sql(BackfillSql("Instructions", "InstructionsHtml", "ol"));
            migrationBuilder.Sql(BackfillSql("CoachingPoints", "CoachingPointsHtml", "ul"));
        }

        private static string BackfillSql(string arrayColumn, string htmlColumn, string listTag) => $"""
            UPDATE "Drills"
            SET "{htmlColumn}" = '<{listTag}>' || (
                SELECT string_agg(
                    '<li><p>' ||
                    replace(replace(replace(btrim(line), '&', '&amp;'), '<', '&lt;'), '>', '&gt;') ||
                    '</p></li>', '')
                FROM unnest("{arrayColumn}") AS line
                WHERE btrim(line) <> ''
            ) || '</{listTag}>'
            WHERE "{htmlColumn}" IS NULL
              AND EXISTS (SELECT 1 FROM unnest("{arrayColumn}") AS line WHERE btrim(line) <> '');
            """;


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoachingPointsHtml",
                table: "Drills");

            migrationBuilder.DropColumn(
                name: "InstructionsHtml",
                table: "Drills");
        }
    }
}
