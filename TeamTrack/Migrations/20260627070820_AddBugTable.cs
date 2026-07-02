using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TeamTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddBugTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bugs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bug_number = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    screenshot_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    work_item_id = table.Column<int>(type: "integer", nullable: false),
                    raised_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_to_user_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fixed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bugs", x => x.id);
                    table.ForeignKey(
                        name: "FK_bugs_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bugs_users_raised_by_user_id",
                        column: x => x.raised_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bugs_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bugs_assigned_to_user_id",
                table: "bugs",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bugs_bug_number",
                table: "bugs",
                column: "bug_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bugs_raised_by_user_id",
                table: "bugs",
                column: "raised_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bugs_work_item_id",
                table: "bugs",
                column: "work_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bugs");
        }
    }
}
