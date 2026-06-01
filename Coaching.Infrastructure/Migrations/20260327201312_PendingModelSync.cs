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
                name: "FK_TemplateComments_UserProfile_UserId",
                table: "TemplateComments");

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
                name: "FK_TemplateComments_UserProfile_UserId",
                table: "TemplateComments");

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
