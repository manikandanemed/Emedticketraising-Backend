using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignToToPersonalNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "assigned_to_user_id",
                table: "personal_notes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_personal_notes_assigned_to_user_id",
                table: "personal_notes",
                column: "assigned_to_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_personal_notes_users_assigned_to_user_id",
                table: "personal_notes",
                column: "assigned_to_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_personal_notes_users_assigned_to_user_id",
                table: "personal_notes");

            migrationBuilder.DropIndex(
                name: "IX_personal_notes_assigned_to_user_id",
                table: "personal_notes");

            migrationBuilder.DropColumn(
                name: "assigned_to_user_id",
                table: "personal_notes");
        }
    }
}
