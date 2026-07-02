using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TeamTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attachment_urls",
                table: "work_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "labels",
                table: "work_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "work_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date",
                table: "work_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "team",
                table: "work_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_type",
                table: "work_items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "profile_picture",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "personal_notes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    priority = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personal_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_personal_notes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_members", x => new { x.project_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_personal_notes_user_id",
                table: "personal_notes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_members_user_id",
                table: "project_members",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personal_notes");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropColumn(
                name: "attachment_urls",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "labels",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "team",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "work_type",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "profile_picture",
                table: "users");
        }
    }
}
