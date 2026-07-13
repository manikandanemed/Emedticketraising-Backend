using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamTrack.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateSoftwareBuildIntoTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "build_id",
                table: "tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "project_id",
                table: "tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_build_id",
                table: "tickets",
                column: "build_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_project_id",
                table: "tickets",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_projects_project_id",
                table: "tickets",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_software_builds_build_id",
                table: "tickets",
                column: "build_id",
                principalTable: "software_builds",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_projects_project_id",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_software_builds_build_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_build_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_project_id",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "build_id",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "tickets");
        }
    }
}
