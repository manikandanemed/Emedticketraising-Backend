using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddEpicAndBugSeverityDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "epic_color",
                table: "work_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "epic_name",
                table: "work_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "severity",
                table: "bugs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "epic_color",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "epic_name",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "severity",
                table: "bugs");
        }
    }
}
